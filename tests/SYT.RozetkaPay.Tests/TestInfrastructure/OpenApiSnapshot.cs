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
