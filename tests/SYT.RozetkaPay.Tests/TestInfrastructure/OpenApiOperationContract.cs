namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Whether an operation sends a request body, as the official document declares it.
/// </summary>
internal enum ContractBodyPolicy
{
    /// <summary>The operation declares no request body, so none may be sent.</summary>
    None,

    /// <summary>The operation declares an <c>application/json</c> request body.</summary>
    Json
}

/// <summary>
/// Whether an operation is authenticated, as the official document declares it.
/// </summary>
internal enum ContractAuthPolicy
{
    /// <summary>The operation inherits the document-level Basic requirement.</summary>
    Authenticated,

    /// <summary>The operation overrides security with an empty list and must carry no credential.</summary>
    Anonymous
}

/// <summary>
/// One canonical row of the pinned OpenAPI document: the official operation identity, the canonical SDK
/// entry point, and the exact request that entry point must produce.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExpectedPathAndQuery"/> is written as a literal (or as a concatenation of literals) and is
/// never produced by a production URL helper. The official snapshot and this literal are the two
/// independent oracles; if the SDK escapes the wrong value, escapes at the wrong insertion point,
/// escapes twice, or emits the wrong verb, the row fails.
/// </para>
/// <para>
/// <see cref="InvokeAsync"/> calls the current canonical service method. Where the SDK also keeps a
/// legacy member for the same area, the legacy member is deliberately not used: it is not one of the 67
/// published operations, and covering it here would let a row pass while the official operation stayed
/// unreachable.
/// </para>
/// </remarks>
internal sealed record OpenApiOperationContract
{
    /// <summary>Official <c>operationId</c>, matched against the pinned document verbatim.</summary>
    public required string OperationId { get; init; }

    /// <summary>Uppercase HTTP method the document declares.</summary>
    public required string Method { get; init; }

    /// <summary>Official path template, including any <c>{placeholder}</c> segments.</summary>
    public required string PathTemplate { get; init; }

    /// <summary>Coverage group of section 6 of the plan; group sizes are asserted.</summary>
    public required string Group { get; init; }

    /// <summary>Canonical SDK interface a consumer injects to reach this operation.</summary>
    public required Type ServiceInterface { get; init; }

    /// <summary>Canonical SDK method name on <see cref="ServiceInterface"/>.</summary>
    public required string ServiceMethod { get; init; }

    /// <summary>Exact request target the SDK must produce, as an independent literal.</summary>
    public required string ExpectedPathAndQuery { get; init; }

    /// <summary>Request-body policy the official operation declares.</summary>
    public required ContractBodyPolicy Body { get; init; }

    /// <summary>Authentication policy the official document declares.</summary>
    public required ContractAuthPolicy Auth { get; init; }

    /// <summary>Response the controlled transport answers this operation with.</summary>
    public ContractResponseKind Response { get; init; } = ContractResponseKind.StructuredError;

    /// <summary>
    /// Sentinel fragments that must appear in the serialized request body. Unique per operation, so a
    /// row that accidentally reuses another row's payload is visible. Deliberately not a whole-body
    /// equality assertion: exact JSON shapes are already owned by the serializer-focused suites.
    /// </summary>
    public string[] ExpectedBodyFragments { get; init; } = [];

    /// <summary>Invokes the canonical SDK method with valid sample input.</summary>
    public required Func<ContractServiceHost, CancellationToken, Task> InvokeAsync { get; init; }

    /// <summary>
    /// Absolute path of <see cref="ExpectedPathAndQuery"/>. Split from the same literal so a failure
    /// reports the path and the query separately; the literal comparison remains the primary assertion.
    /// </summary>
    public string ExpectedAbsolutePath =>
        ExpectedPathAndQuery.Split('?', 2)[0];

    /// <summary>
    /// Query keys of <see cref="ExpectedPathAndQuery"/>, in order, split from the same literal.
    /// </summary>
    public string[] ExpectedQueryKeys
    {
        get
        {
            string[] parts = ExpectedPathAndQuery.Split('?', 2);
            if (parts.Length == 1 || parts[1].Length == 0)
            {
                return [];
            }

            return parts[1]
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(static pair => pair.Split('=', 2)[0])
                .ToArray();
        }
    }

    /// <summary>
    /// Credential-bearing header names this operation must carry. An authenticated operation carries
    /// Basic plus both optional headers because the contract configuration sets both; the one anonymous
    /// operation must carry none of them.
    /// </summary>
    public string[] ExpectedCredentialHeaders => Auth == ContractAuthPolicy.Authenticated
        ? ["Authorization", "X-ON-BEHALF-OF", "X-CUSTOMER-AUTH"]
        : [];

    /// <summary>Document identity of this row: the tuple the manifest and the snapshot are compared on.</summary>
    public (string Method, string PathTemplate, string OperationId) Identity =>
        (Method, PathTemplate, OperationId);

    /// <summary>Readable row label for a failure message. Carries no caller value and no credential.</summary>
    public override string ToString()
    {
        return $"{OperationId} ({Method} {PathTemplate})";
    }
}
