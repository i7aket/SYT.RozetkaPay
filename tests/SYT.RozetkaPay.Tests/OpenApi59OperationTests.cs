using System.Security.Cryptography;
using System.Text.Json;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Guards the pinned OpenAPI snapshot refreshed by EXP-354.
///
/// The snapshot is the SDK's single statement about what the provider publishes, so its identity is
/// asserted byte-for-byte: the SHA-256 below is the hash of the document fetched from
/// <c>https://docs.rozetkapay.com/openapi.json</c> on 2026-07-25. Path and operation counts, the ten
/// operations EXP-354 adds, and the two operationIds that stopped being duplicates are asserted from the
/// document itself rather than from a hand-maintained list.
///
/// This class proves what the pinned document declares. It does not prove that a live sandbox answers
/// all 67 operations - that coverage is EXP-337, and no claim about it is made here.
/// </summary>
public class OpenApi59OperationTests
{
    /// <summary>
    /// SHA-256 of the official document observed on 2026-07-25 and pinned by EXP-354.
    /// </summary>
    private const string PinnedSha256 =
        "d3114314e542adc8239579116f02a367496387636af0707c332c848ac27766cf";

    private const int PinnedPathCount = 59;

    private const int PinnedOperationCount = 67;

    private static readonly string[] HttpVerbs =
        ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

    /// <summary>
    /// The ten net-new verb/path/operationId triples EXP-354 covers.
    /// </summary>
    public static TheoryData<string, string, string> NewOperations =>
        new()
        {
            { "PATCH", "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method", "UpdateSubscriptionPaymentMethod" },
            { "POST", "/api/in-store-payments/v1/create", "createInStorePayment" },
            { "POST", "/api/in-store-payments/v1/confirm", "confirmInStorePayment" },
            { "POST", "/api/in-store-payments/v1/refund", "refundInStorePayment" },
            { "POST", "/api/in-store-payments/v1/info", "getInStorePaymentInfo" },
            { "GET", "/api/partners/v1/fee-details", "feeDetails" },
            { "GET", "/api/partners/v1/merchant-status", "merchantStatus" },
            { "GET", "/api/partners/v1/transaction-details", "transactionDetails" },
            { "POST", "/api/payment-instructions/v1/new", "createPaymentInstructions" },
            { "GET", "/api/payment-instructions/v1/decline", "declinePaymentInstruction" }
        };

    /// <summary>
    /// The snapshot on disk is the one this suite was written against.
    /// </summary>
    /// <remarks>
    /// Whether that snapshot still matches what RozetkaPay publishes is a different question, and
    /// <c>scripts/verify-openapi-drift.sh</c> answers it in CI. This hash catches the other failure: a
    /// local edit to the snapshot, which would quietly move every expectation the contract tests read
    /// from it.
    /// </remarks>
    [Fact]
    public void PinnedSnapshot_ShouldMatchTheDocumentedHash()
    {
        byte[] bytes = File.ReadAllBytes(SnapshotPath());
        string actual = Convert.ToHexStringLower(SHA256.HashData(bytes));

        Assert.Equal(PinnedSha256, actual);
    }

    [Fact]
    public void PinnedSnapshot_ShouldDeclareFiftyNinePathsAndSixtySevenOperations()
    {
        using JsonDocument document = LoadSnapshot();
        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.Equal("3.0.3", document.RootElement.GetProperty("openapi").GetString());
        Assert.Equal(PinnedPathCount, paths.EnumerateObject().Count());
        Assert.Equal(PinnedOperationCount, EnumerateOperations(paths).Count);
    }

    [Theory]
    [MemberData(nameof(NewOperations))]
    public void PinnedSnapshot_ShouldDeclareTheNewOperation(string verb, string path, string operationId)
    {
        using JsonDocument document = LoadSnapshot();
        Dictionary<(string Verb, string Path), JsonElement> operations =
            EnumerateOperations(document.RootElement.GetProperty("paths"));

        Assert.True(
            operations.TryGetValue((verb, path), out JsonElement operation),
            $"{verb} {path} must be declared by the pinned snapshot.");
        Assert.Equal(operationId, operation.GetProperty("operationId").GetString());
    }

    [Fact]
    public void PinnedSnapshot_ShouldDeclareTenNewOperationsAndRemoveNone()
    {
        using JsonDocument document = LoadSnapshot();
        HashSet<(string, string)> declared =
            EnumerateOperations(document.RootElement.GetProperty("paths")).Keys.ToHashSet();

        HashSet<(string, string)> expectedNew = NewOperations
            .Select(row => ((string)row[0], (string)row[1]))
            .ToHashSet();

        Assert.Equal(10, expectedNew.Count);
        Assert.Subset(declared, expectedNew);
    }

    /// <summary>
    /// The two callback resend operations used to share the operationId <c>resendCallback</c>. They now
    /// carry distinct IDs, and no non-empty operationId in the document is duplicated any more.
    /// </summary>
    [Fact]
    public void PinnedSnapshot_ShouldGiveEveryOperationAUniqueOperationId()
    {
        using JsonDocument document = LoadSnapshot();
        Dictionary<(string Verb, string Path), JsonElement> operations =
            EnumerateOperations(document.RootElement.GetProperty("paths"));

        Assert.Equal(
            "resendAlternativePaymentCallback",
            operations[("POST", "/api/alternative-payments/v1/callback/resend")].GetProperty("operationId").GetString());
        Assert.Equal(
            "resendPayPartsCallback",
            operations[("POST", "/api/payparts/v1/callback/resend")].GetProperty("operationId").GetString());

        List<string> operationIds = operations.Values
            .Select(static operation => operation.TryGetProperty("operationId", out JsonElement id)
                ? id.GetString()
                : null)
            .Where(static id => !string.IsNullOrEmpty(id))
            .Select(static id => id!)
            .ToList();

        Assert.Equal(PinnedOperationCount, operationIds.Count);
        Assert.Equal(operationIds.Count, operationIds.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// The decline operation is the only unauthenticated one, and the only one whose documented success
    /// is a redirect. Both facts are what the SDK's separate credential-free non-redirecting client is
    /// built for, so a change to either must fail here.
    /// </summary>
    [Fact]
    public void PinnedSnapshot_DeclineOperation_ShouldBeUnauthenticatedAndReturn302WithLocation()
    {
        using JsonDocument document = LoadSnapshot();
        JsonElement decline = Operation(document, "get", "/api/payment-instructions/v1/decline");

        JsonElement security = decline.GetProperty("security");
        Assert.Equal(JsonValueKind.Array, security.ValueKind);
        Assert.Empty(security.EnumerateArray());

        JsonElement responses = decline.GetProperty("responses");
        JsonElement redirect = responses.GetProperty("302");
        Assert.True(redirect.GetProperty("headers").TryGetProperty("Location", out _));

        // A redirect body would be something to read; the operation declares none.
        Assert.False(redirect.TryGetProperty("content", out _));

        // The verb is a GET, and nothing is sent with it.
        Assert.False(decline.TryGetProperty("requestBody", out _));

        // Both query parameters are required.
        Dictionary<string, JsonElement> parameters = QueryParameters(decline);
        Assert.True(IsRequired(parameters["project_id"]));
        Assert.True(IsRequired(parameters["payment_instruction_id"]));
        Assert.Equal(2, parameters.Count);

        // 200 is not a documented outcome, so the SDK is right to treat it as a protocol failure.
        Assert.False(responses.TryGetProperty("200", out _));
    }

    /// <summary>
    /// Every other new operation inherits the document-level security requirement, which is what makes
    /// the decline exception meaningful.
    /// </summary>
    [Theory]
    [MemberData(nameof(NewOperations))]
    public void PinnedSnapshot_EveryNewOperationExceptDecline_ShouldInheritGlobalSecurity(
        string verb,
        string path,
        string operationId)
    {
        using JsonDocument document = LoadSnapshot();
        JsonElement operation = Operation(document, verb.ToLowerInvariant(), path);

        Assert.NotEmpty(document.RootElement.GetProperty("security").EnumerateArray());

        if (operationId == "declinePaymentInstruction")
        {
            Assert.Empty(operation.GetProperty("security").EnumerateArray());
            return;
        }

        Assert.False(
            operation.TryGetProperty("security", out _),
            $"{operationId} must inherit the document-level security requirement.");
    }

    /// <summary>
    /// The info operation is a POST that declares no request body. The SDK must therefore send a POST
    /// with no content at all — not a GET, and not an invented empty JSON object.
    /// </summary>
    [Fact]
    public void PinnedSnapshot_InStoreInfoOperation_ShouldBeABodylessPostWithRequiredExternalId()
    {
        using JsonDocument document = LoadSnapshot();
        JsonElement info = Operation(document, "post", "/api/in-store-payments/v1/info");

        Assert.False(info.TryGetProperty("requestBody", out _));

        Dictionary<string, JsonElement> parameters = QueryParameters(info);
        Assert.True(IsRequired(parameters["external_id"]));
        Assert.Single(parameters);

        // The same path is not also published as a GET.
        JsonElement pathItem = document.RootElement.GetProperty("paths").GetProperty("/api/in-store-payments/v1/info");
        Assert.False(pathItem.TryGetProperty("get", out _));

        Assert.True(HasJsonResponse(document, info, "200"));
    }

    [Fact]
    public void PinnedSnapshot_SubscriptionPaymentMethodOperation_ShouldBeAPatchWithBodyAndTypedResponse()
    {
        using JsonDocument document = LoadSnapshot();
        const string path = "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method";
        JsonElement update = Operation(document, "patch", path);

        JsonElement pathItem = document.RootElement.GetProperty("paths").GetProperty(path);
        Assert.False(pathItem.TryGetProperty("put", out _));
        Assert.False(pathItem.TryGetProperty("post", out _));

        Assert.True(update.TryGetProperty("requestBody", out JsonElement requestBody));
        Assert.Contains("UpdateSubscriptionPaymentMethodRequest", requestBody.GetProperty("$ref").GetString());

        Assert.Contains(
            "UpdateSubscriptionPaymentMethodResponse",
            update.GetProperty("responses").GetProperty("200").GetProperty("$ref").GetString());

        // The required path parameter, plus the optional customer-auth header the SDK already supports.
        List<JsonElement> parameters = update.GetProperty("parameters").EnumerateArray().ToList();
        Assert.Contains(parameters, parameter =>
            parameter.GetProperty("in").GetString() == "path"
            && parameter.GetProperty("name").GetString() == "subscription_id"
            && IsRequired(parameter));
        Assert.Contains(parameters, parameter =>
            parameter.GetProperty("in").GetString() == "header"
            && parameter.GetProperty("name").GetString() == "X-CUSTOMER-AUTH");
    }

    /// <summary>
    /// The eight remaining new operations declare a JSON <c>200</c>; the four in-store and the
    /// payment-instruction create operations also declare a JSON request body.
    /// </summary>
    [Fact]
    public void PinnedSnapshot_NewOperations_ShouldDeclareTheExpectedBodiesAndResponses()
    {
        using JsonDocument document = LoadSnapshot();

        string[] jsonBodyOperations =
        [
            "/api/in-store-payments/v1/create",
            "/api/in-store-payments/v1/confirm",
            "/api/in-store-payments/v1/refund",
            "/api/payment-instructions/v1/new"
        ];

        foreach (string path in jsonBodyOperations)
        {
            JsonElement operation = Operation(document, "post", path);
            Assert.True(
                HasJsonRequestBody(document, operation),
                $"POST {path} must declare an application/json request body.");
            Assert.True(HasJsonResponse(document, operation, "200"), $"POST {path} must declare a JSON 200.");
        }

        string[] partnerOperations =
        [
            "/api/partners/v1/fee-details",
            "/api/partners/v1/merchant-status",
            "/api/partners/v1/transaction-details"
        ];

        foreach (string path in partnerOperations)
        {
            JsonElement operation = Operation(document, "get", path);
            Assert.False(operation.TryGetProperty("requestBody", out _));
            Assert.True(HasJsonResponse(document, operation, "200"), $"GET {path} must declare a JSON 200.");
        }
    }

    /// <summary>
    /// Partner query parameter names and requiredness, as the SDK renders them.
    /// </summary>
    [Fact]
    public void PinnedSnapshot_PartnerOperations_ShouldDeclareTheExpectedQueryParameters()
    {
        using JsonDocument document = LoadSnapshot();

        Dictionary<string, JsonElement> fee = QueryParameters(Operation(document, "get", "/api/partners/v1/fee-details"));
        Assert.False(IsRequired(fee["merchant_project_id"]));
        Assert.Single(fee);

        Dictionary<string, JsonElement> status =
            QueryParameters(Operation(document, "get", "/api/partners/v1/merchant-status"));
        Assert.False(IsRequired(status["merchant_project_id"]));
        Assert.False(IsRequired(status["merchant_entity_id"]));
        Assert.Equal(2, status.Count);

        Dictionary<string, JsonElement> transactions =
            QueryParameters(Operation(document, "get", "/api/partners/v1/transaction-details"));
        Assert.True(IsRequired(transactions["merchant_entity_id"]));
        Assert.False(IsRequired(transactions["merchant_order_id"]));
        Assert.False(IsRequired(transactions["unified_external_id"]));
        Assert.Equal(3, transactions.Count);
    }

    /// <summary>
    /// The wire tokens the SDK enums must produce, read from the document instead of trusted.
    /// </summary>
    [Fact]
    public void PinnedSnapshot_ShouldDeclareTheExpectedEnumTokens()
    {
        using JsonDocument document = LoadSnapshot();

        JsonElement create = RequestBodySchema(document, Operation(document, "post", "/api/in-store-payments/v1/create"));
        Assert.Equal(
            ["980"],
            create.GetProperty("properties").GetProperty("currency").GetProperty("enum")
                .EnumerateArray().Select(static value => value.GetString()));

        JsonElement instructions = RequestBodySchema(
            document,
            Operation(document, "post", "/api/payment-instructions/v1/new"));
        JsonElement instructionProperties = instructions.GetProperty("properties");

        Assert.Equal(
            ["cardpay", "ppay"],
            instructionProperties.GetProperty("processing_type").GetProperty("enum")
                .EnumerateArray().Select(static value => value.GetString()));
        Assert.Equal(
            ["auth", "purchase"],
            instructionProperties.GetProperty("method").GetProperty("enum")
                .EnumerateArray().Select(static value => value.GetString()));

        JsonElement subscriptionUpdate = RequestBodySchema(
            document,
            Operation(document, "patch", "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method"));

        // payment_method is a $ref to components/schemas/SubscriptionPaymentMethod, whose own "type"
        // property is a further $ref to components/schemas/SubscriptionPaymentMethodType. The token list
        // is therefore two indirections deep.
        JsonElement paymentMethod = ResolveIn(
            document,
            subscriptionUpdate.GetProperty("properties").GetProperty("payment_method"));
        JsonElement paymentMethodType = ResolveIn(
            document,
            paymentMethod.GetProperty("properties").GetProperty("type"));

        Assert.Equal(
            ["cc_token", "wallet", "google_pay", "apple_pay", "recurrent_id"],
            paymentMethodType.GetProperty("enum").EnumerateArray().Select(static value => value.GetString()));

        // The nested objects the SDK models are the ones the provider declares, and no others.
        Assert.Equal(
            ["type", "cc_token", "wallet", "apple_pay", "google_pay", "recurrent_id"],
            paymentMethod.GetProperty("properties").EnumerateObject().Select(static property => property.Name));
    }

    private static string SnapshotPath()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "openapi.json");

        Assert.True(File.Exists(path), $"The pinned OpenAPI snapshot must be copied to {path}.");
        return path;
    }

    private static JsonDocument LoadSnapshot()
    {
        return JsonDocument.Parse(File.ReadAllBytes(SnapshotPath()));
    }

    private static Dictionary<(string Verb, string Path), JsonElement> EnumerateOperations(JsonElement paths)
    {
        Dictionary<(string, string), JsonElement> operations = [];

        foreach (JsonProperty path in paths.EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (HttpVerbs.Contains(operation.Name, StringComparer.Ordinal))
                {
                    operations[(operation.Name.ToUpperInvariant(), path.Name)] = operation.Value;
                }
            }
        }

        return operations;
    }

    private static JsonElement Operation(JsonDocument document, string verb, string path)
    {
        return document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(verb);
    }

    private static Dictionary<string, JsonElement> QueryParameters(JsonElement operation)
    {
        if (!operation.TryGetProperty("parameters", out JsonElement parameters))
        {
            return [];
        }

        return parameters.EnumerateArray()
            .Where(static parameter => parameter.GetProperty("in").GetString() == "query")
            .ToDictionary(
                static parameter => parameter.GetProperty("name").GetString()!,
                static parameter => parameter,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Requiredness of an OpenAPI parameter. An absent <c>required</c> key means "not required" per the
    /// specification, and the official document relies on that default for every optional parameter, so
    /// reading the key unconditionally would fail rather than report <see langword="false"/>.
    /// </summary>
    private static bool IsRequired(JsonElement parameter)
    {
        return parameter.TryGetProperty("required", out JsonElement required) && required.GetBoolean();
    }

    private static bool HasJsonRequestBody(JsonDocument document, JsonElement operation)
    {
        return operation.TryGetProperty("requestBody", out JsonElement requestBody)
            && ResolveIn(document, requestBody).TryGetProperty("content", out JsonElement content)
            && content.TryGetProperty("application/json", out _);
    }

    private static bool HasJsonResponse(JsonDocument document, JsonElement operation, string statusCode)
    {
        return operation.GetProperty("responses").TryGetProperty(statusCode, out JsonElement response)
            && ResolveIn(document, response).TryGetProperty("content", out JsonElement content)
            && content.TryGetProperty("application/json", out _);
    }

    private static JsonElement RequestBodySchema(JsonDocument document, JsonElement operation)
    {
        JsonElement requestBody = ResolveIn(document, operation.GetProperty("requestBody"));
        return ResolveIn(
            document,
            requestBody.GetProperty("content").GetProperty("application/json").GetProperty("schema"));
    }

    /// <summary>
    /// Follow <c>$ref</c> indirections until an inline node is reached.
    /// </summary>
    private static JsonElement ResolveIn(JsonDocument document, JsonElement node)
    {
        while (node.TryGetProperty("$ref", out JsonElement reference))
        {
            JsonElement current = document.RootElement;
            foreach (string segment in reference.GetString()!.TrimStart('#', '/').Split('/'))
            {
                current = current.GetProperty(segment);
            }

            node = current;
        }

        return node;
    }
}
