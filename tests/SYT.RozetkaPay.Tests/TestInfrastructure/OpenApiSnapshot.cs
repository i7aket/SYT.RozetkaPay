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

    /// <summary>
    /// Every path the document declares.
    /// </summary>
    internal static IEnumerable<string> DeclaredPaths()
    {
        return Document.Value.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .Select(static path => path.Name);
    }

    /// <summary>
    /// Every operation the document declares, as an uppercase verb and its path template.
    /// </summary>
    internal static IEnumerable<(string Method, string Path)> DeclaredOperations()
    {
        foreach (JsonProperty path in Document.Value.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (operation.Value.ValueKind == JsonValueKind.Object)
                {
                    yield return (operation.Name.ToUpperInvariant(), path.Name);
                }
            }
        }
    }

    /// <summary>
    /// The field names the document declares in an operation's <c>200</c> response body.
    /// </summary>
    /// <remarks>
    /// Empty when the operation declares no <c>200</c>, no JSON content, or a bare array of scalars —
    /// there is nothing to compare a model against in those cases, and returning an empty set lets
    /// the caller skip rather than guess.
    /// </remarks>
    internal static IReadOnlyCollection<string> ResponseFieldsOf(string method, string pathTemplate)
    {
        if (!TryGetOperation(method, pathTemplate, out JsonElement operation) ||
            !operation.TryGetProperty("responses", out JsonElement responses) ||
            !responses.TryGetProperty("200", out JsonElement ok))
        {
            return [];
        }

        return FieldsOfBody(ok);
    }

    /// <summary>
    /// The field names the document declares in an operation's request body.
    /// </summary>
    internal static IReadOnlyCollection<string> RequestFieldsOf(string method, string pathTemplate)
    {
        if (!TryGetOperation(method, pathTemplate, out JsonElement operation) ||
            !operation.TryGetProperty("requestBody", out JsonElement body))
        {
            return [];
        }

        return FieldsOfBody(body);
    }

    /// <summary>
    /// Resolves a request-body or response wrapper down to its JSON schema's property names.
    /// </summary>
    /// <remarks>
    /// The wrapper is usually a <c>$ref</c> into <c>components.requestBodies</c> or
    /// <c>components.responses</c>, so it is resolved first and the schema read from
    /// <c>content."application/json".schema</c>. An array schema is followed to its items, because a
    /// bare array of objects is a shape several operations really return.
    /// </remarks>
    private static IReadOnlyCollection<string> FieldsOfBody(JsonElement wrapper)
    {
        JsonElement resolved = Resolve(wrapper);

        if (!resolved.TryGetProperty("content", out JsonElement content) ||
            !content.TryGetProperty("application/json", out JsonElement json) ||
            !json.TryGetProperty("schema", out JsonElement schema))
        {
            return [];
        }

        schema = Resolve(schema);

        if (schema.TryGetProperty("type", out JsonElement kind) &&
            kind.ValueKind == JsonValueKind.String &&
            kind.GetString() == "array" &&
            schema.TryGetProperty("items", out JsonElement items))
        {
            schema = Resolve(items);
        }

        return [.. CollectPropertyNames(schema).Distinct(StringComparer.Ordinal)];
    }

    private static bool TryGetOperation(string method, string pathTemplate, out JsonElement operation)
    {
        operation = default;

        return Document.Value.RootElement.GetProperty("paths")
                   .TryGetProperty(pathTemplate, out JsonElement path)
               && path.TryGetProperty(method.ToLowerInvariant(), out operation);
    }

    /// <summary>
    /// Every schema reachable from the request body of any declared operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reachability, not direct reference. An operation's <c>requestBody</c> is usually a
    /// <c>$ref</c> into <c>components.requestBodies</c>, which refs a schema, which refs more — so
    /// the walk has to cross every component section, not just <c>components.schemas</c>. Stopping
    /// at the first hop reports nothing as request-side, which is wrong in the dangerous direction:
    /// it would let a request model be treated as a response one.
    /// </para>
    /// <para>
    /// A schema can be both. Anything reachable from a request body is included here whether or not
    /// a response uses it too, because a property on a shared type is still sent.
    /// </para>
    /// </remarks>
    internal static IEnumerable<string> RequestReachableSchemas()
    {
        HashSet<string> visited = [];
        HashSet<string> schemas = [];
        Queue<string> pending = new();

        foreach (JsonProperty path in Document.Value.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (operation.Value.ValueKind == JsonValueKind.Object &&
                    operation.Value.TryGetProperty("requestBody", out JsonElement body))
                {
                    foreach (string reference in References(body))
                    {
                        pending.Enqueue(reference);
                    }
                }
            }
        }

        while (pending.Count > 0)
        {
            string reference = pending.Dequeue();
            if (!visited.Add(reference))
            {
                continue;
            }

            const string schemaPrefix = "#/components/schemas/";
            if (reference.StartsWith(schemaPrefix, StringComparison.Ordinal))
            {
                schemas.Add(reference[schemaPrefix.Length..]);
            }

            if (!TryResolvePointer(reference, out JsonElement target))
            {
                continue;
            }

            foreach (string next in References(target))
            {
                pending.Enqueue(next);
            }
        }

        return schemas;
    }

    /// <summary>
    /// Every <c>$ref</c> string anywhere beneath a node.
    /// </summary>
    private static IEnumerable<string> References(JsonElement node)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in node.EnumerateObject())
                {
                    if (property.NameEquals("$ref") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        yield return property.Value.GetString()!;
                        continue;
                    }

                    foreach (string nested in References(property.Value))
                    {
                        yield return nested;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in node.EnumerateArray())
                {
                    foreach (string nested in References(item))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Walks a local JSON pointer such as <c>#/components/requestBodies/CreatePaymentRequestDev</c>.
    /// </summary>
    private static bool TryResolvePointer(string reference, out JsonElement target)
    {
        target = Document.Value.RootElement;

        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string segment in reference[2..].Split('/'))
        {
            // Per RFC 6901, in that order.
            string name = segment.Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);

            if (target.ValueKind != JsonValueKind.Object || !target.TryGetProperty(name, out target))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Every named schema with the JSON kind it declares for each property.
    /// </summary>
    /// <remarks>
    /// Kinds only — <c>string</c>, <c>integer</c>, <c>number</c>, <c>boolean</c>, <c>array</c>,
    /// <c>object</c>, <c>enum</c>. Formats are not reported: whether a date-time string is read as
    /// <c>DateTime</c> or left as text is a modelling choice, while reading a number into a string is
    /// a defect, and only the second is worth failing a build over.
    /// </remarks>
    internal static IEnumerable<(string Name, IReadOnlyDictionary<string, string> Properties)> SchemaPropertyKinds()
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

                Dictionary<string, string> kinds = [];
                CollectPropertyKinds(schema, kinds);

                if (kinds.Count > 0)
                {
                    yield return (entry.Name, kinds);
                }
            }
        }
    }

    private static void CollectPropertyKinds(JsonElement schema, Dictionary<string, string> into, int depth = 0)
    {
        if (depth > 10)
        {
            return;
        }

        schema = Resolve(schema);

        if (schema.TryGetProperty("allOf", out JsonElement composed))
        {
            foreach (JsonElement part in composed.EnumerateArray())
            {
                CollectPropertyKinds(part, into, depth + 1);
            }
        }

        if (!schema.TryGetProperty("properties", out JsonElement properties))
        {
            return;
        }

        foreach (JsonProperty property in properties.EnumerateObject())
        {
            string? kind = KindOf(property.Value);
            if (kind is not null)
            {
                into[property.Name] = kind;
            }
        }
    }

    private static string? KindOf(JsonElement node)
    {
        node = Resolve(node);

        if (node.TryGetProperty("enum", out _))
        {
            return "enum";
        }

        if (node.TryGetProperty("type", out JsonElement type) && type.ValueKind == JsonValueKind.String)
        {
            return type.GetString();
        }

        bool composed = node.TryGetProperty("properties", out _) || node.TryGetProperty("allOf", out _);

        return composed ? "object" : null;
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
