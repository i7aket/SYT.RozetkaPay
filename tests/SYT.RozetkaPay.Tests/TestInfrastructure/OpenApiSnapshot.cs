using System.Text.Json;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Reads the pinned OpenAPI snapshot that ships next to the test assembly.
/// </summary>
/// <remarks>
/// <para>
/// Expectations are read from the document rather than retyped into a test, so a typo in a test cannot
/// agree with the same typo in production. The snapshot itself is checked against the live document by
/// the drift job, which is what keeps these expectations current.
/// </para>
/// <para>
/// Enum values are collected through <c>allOf</c> as well as from a direct <c>enum</c> key. Two
/// published schemas — <c>AlternativePaymentResponseCode</c> and <c>PayPartsResponseCode</c> — carry no
/// values of their own and inherit all 184 from <c>ResponseCode</c>. A reader that looked only for a
/// direct key reported them as "not an enum" and skipped them, which is exactly how both shipped with
/// three values each.
/// </para>
/// </remarks>
internal static class OpenApiSnapshot
{
    private static readonly Lazy<JsonDocument> Document = new(static () =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "openapi.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    });

    /// <summary>
    /// Names of every component schema that declares enum values, directly or by inheritance.
    /// </summary>
    internal static IEnumerable<string> EnumSchemaNames()
    {
        foreach (JsonProperty schema in Schemas().EnumerateObject())
        {
            if (CollectEnumValues(schema.Value).Count > 0)
            {
                yield return schema.Name;
            }
        }
    }

    /// <summary>
    /// The distinct values a named enum schema declares, in declaration order.
    /// </summary>
    /// <remarks>
    /// Distinct because the document is allowed to repeat one: <c>ResponseCode</c> lists 185 entries of
    /// which <c>payment_method_not_found</c> appears twice.
    /// </remarks>
    internal static IReadOnlyList<string> EnumValues(string schemaName)
    {
        List<string> values = CollectEnumValues(Schemas().GetProperty(schemaName));

        return values.Count == 0
            ? throw new InvalidOperationException($"Schema '{schemaName}' declares no enum values.")
            : [.. values.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// The property names a named request body declares.
    /// </summary>
    internal static IEnumerable<string> RequestBodyPropertyNames(string requestBodyName)
    {
        return RequestBodySchema(requestBodyName)
            .GetProperty("properties")
            .EnumerateObject()
            .Select(static property => property.Name);
    }

    /// <summary>
    /// The JSON names a model type puts on the wire, ignoring anything marked <c>[JsonIgnore]</c>.
    /// </summary>
    internal static IEnumerable<string> JsonPropertyNamesOf(Type modelType)
    {
        return modelType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(static property => property
                .GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), inherit: true)
                .Length == 0)
            .Select(static property =>
                (property.GetCustomAttributes(
                        typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute),
                        inherit: true)
                    .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute)?.Name
                ?? property.Name);
    }

    /// <summary>
    /// The property names a named request body declares as required.
    /// </summary>
    internal static IEnumerable<string> RequiredRequestBodyPropertyNames(string requestBodyName)
    {
        JsonElement schema = RequestBodySchema(requestBodyName);

        return schema.TryGetProperty("required", out JsonElement required)
            ? required.EnumerateArray().Select(static value => value.GetString()!)
            : [];
    }

    /// <summary>
    /// The JSON names a model marks with <c>[Required]</c>.
    /// </summary>
    internal static IEnumerable<string> RequiredJsonPropertyNamesOf(Type modelType)
    {
        return modelType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(static property => property
                .GetCustomAttributes(
                    typeof(System.ComponentModel.DataAnnotations.RequiredAttribute),
                    inherit: true)
                .Length > 0)
            .Select(static property =>
                (property.GetCustomAttributes(
                        typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute),
                        inherit: true)
                    .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute)?.Name
                ?? property.Name);
    }

    private static JsonElement RequestBodySchema(string requestBodyName)
    {
        return Document.Value.RootElement
            .GetProperty("components")
            .GetProperty("requestBodies")
            .GetProperty(requestBodyName)
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");
    }

    /// <summary>
    /// Every named schema, request body and response, with the property names it declares.
    /// </summary>
    /// <remarks>
    /// Properties are collected through <c>allOf</c> as well as directly, so a schema that composes its
    /// shape from another reports the full set rather than only what it adds.
    /// </remarks>
    internal static IEnumerable<(string Name, IReadOnlyCollection<string> Properties)> SchemaPropertyNames()
    {
        JsonElement components = Document.Value.RootElement.GetProperty("components");

        foreach (string section in new[] { "schemas", "requestBodies", "responses" })
        {
            if (!components.TryGetProperty(section, out JsonElement group))
            {
                continue;
            }

            foreach (JsonProperty entry in group.EnumerateObject())
            {
                JsonElement schema = section == "schemas" ? entry.Value : BodySchemaOrDefault(entry.Value);
                if (schema.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string[] properties = [.. CollectPropertyNames(schema)];
                if (properties.Length > 0)
                {
                    yield return (entry.Name, properties);
                }
            }
        }
    }

    private static JsonElement BodySchemaOrDefault(JsonElement wrapper)
    {
        return wrapper.TryGetProperty("content", out JsonElement content) &&
               content.TryGetProperty("application/json", out JsonElement json) &&
               json.TryGetProperty("schema", out JsonElement schema)
            ? schema
            : default;
    }

    private static IEnumerable<string> CollectPropertyNames(JsonElement schema, int depth = 0)
    {
        if (depth > 10)
        {
            yield break;
        }

        schema = Resolve(schema);

        if (schema.TryGetProperty("allOf", out JsonElement composed))
        {
            foreach (JsonElement part in composed.EnumerateArray())
            {
                foreach (string name in CollectPropertyNames(part, depth + 1))
                {
                    yield return name;
                }
            }
        }

        if (schema.TryGetProperty("properties", out JsonElement properties))
        {
            foreach (JsonProperty property in properties.EnumerateObject())
            {
                yield return property.Name;
            }
        }
    }

    private static JsonElement Schemas()
    {
        return Document.Value.RootElement.GetProperty("components").GetProperty("schemas");
    }

    private static List<string> CollectEnumValues(JsonElement schema, int depth = 0)
    {
        List<string> values = [];
        if (depth > 10)
        {
            return values;
        }

        schema = Resolve(schema);

        if (schema.TryGetProperty("enum", out JsonElement declared))
        {
            values.AddRange(declared.EnumerateArray().Select(static value => value.GetString()!));
            return values;
        }

        if (schema.TryGetProperty("allOf", out JsonElement composed))
        {
            foreach (JsonElement part in composed.EnumerateArray())
            {
                values.AddRange(CollectEnumValues(part, depth + 1));
            }
        }

        return values;
    }

    private static JsonElement Resolve(JsonElement node, int depth = 0)
    {
        while (depth < 20 && node.ValueKind == JsonValueKind.Object &&
               node.TryGetProperty("$ref", out JsonElement reference))
        {
            JsonElement current = Document.Value.RootElement;
            foreach (string segment in reference.GetString()!.TrimStart('#', '/').Split('/'))
            {
                current = current.GetProperty(segment);
            }

            node = current;
            depth++;
        }

        return node;
    }
}
