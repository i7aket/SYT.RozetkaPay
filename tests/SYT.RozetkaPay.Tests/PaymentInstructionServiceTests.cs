using System.Net;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Wire-level coverage of the two official payment-instruction operations added by EXP-354.
///
/// <c>createPaymentInstructions</c> is an ordinary authenticated POST. <c>declinePaymentInstruction</c> is
/// not: the official document declares it <c>security: []</c> and answers with a bare <c>302</c>. That
/// makes it the one operation where a credential must not be sent and a redirect must not be followed, so
/// most of this class is about proving those two negatives.
/// </summary>
public class PaymentInstructionServiceTests
{
    private const string CreateEndpoint = "/api/payment-instructions/v1/new";

    private const string DeclineEndpoint = "/api/payment-instructions/v1/decline";

    /// <summary>
    /// Default request headers that must never appear on a decline request.
    /// </summary>
    private static readonly string[] CredentialHeaderNames =
        ["Authorization", "Proxy-Authorization", "X-ON-BEHALF-OF", "X-CUSTOMER-AUTH"];

    public static TheoryData<PaymentInstructionProcessingType, string> ProcessingTypeTokens =>
        new()
        {
            { PaymentInstructionProcessingType.CardPay, "cardpay" },
            { PaymentInstructionProcessingType.PPay, "ppay" }
        };

    public static TheoryData<PaymentInstructionMethod, string> MethodTokens =>
        new()
        {
            { PaymentInstructionMethod.Auth, "auth" },
            { PaymentInstructionMethod.Purchase, "purchase" }
        };

    public static TheoryData<HttpStatusCode, Type> DeclineErrorMappings =>
        new()
        {
            { HttpStatusCode.BadRequest, typeof(RozetkaPayValidationException) },
            { HttpStatusCode.Conflict, typeof(RozetkaPayException) },
            { HttpStatusCode.TooManyRequests, typeof(RozetkaPayRateLimitException) },
            { HttpStatusCode.InternalServerError, typeof(RozetkaPayException) }
        };

    // ===================== create =====================

    [Fact]
    public async Task Create_ShouldPostAuthenticatedToTheOfficialTarget()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        await Exp354TestContext.PaymentInstructions(handler, declineClient).CreateAsync(MinimalRequest());

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal(CreateEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.Equal(Exp354TestContext.JsonContentType, recorded.ContentType);
        Assert.True(recorded.Headers.ContainsKey("Authorization"));
    }

    /// <summary>
    /// The processing-type tokens are the reason these enums carry explicit member names: the SDK
    /// snake-case policy would emit <c>card_pay</c> and <c>p_pay</c>, which the provider rejects.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProcessingTypeTokens))]
    public async Task Create_ShouldSerializeProcessingTypeExactly(
        PaymentInstructionProcessingType processingType,
        string expectedToken)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        CreatePaymentInstructionsRequest request = MinimalRequest();
        request.ProcessingType = processingType;

        await Exp354TestContext.PaymentInstructions(handler, declineClient).CreateAsync(request);

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Contains($"\"processing_type\":\"{expectedToken}\"", recorded.Body!);

        // The snake-cased forms of the C# identifiers must never appear.
        Assert.DoesNotContain("card_pay", recorded.Body!);
        Assert.DoesNotContain("p_pay", recorded.Body!);
    }

    [Theory]
    [MemberData(nameof(MethodTokens))]
    public async Task Create_ShouldSerializeMethodExactly(PaymentInstructionMethod method, string expectedToken)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        CreatePaymentInstructionsRequest request = MinimalRequest();
        request.Method = method;

        await Exp354TestContext.PaymentInstructions(handler, declineClient).CreateAsync(request);

        Assert.Contains($"\"method\":\"{expectedToken}\"", Assert.Single(handler.Requests).Body!);
    }

    [Fact]
    public async Task Create_ShouldSerializeTheMinimalRequestExactly()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        await Exp354TestContext.PaymentInstructions(handler, declineClient).CreateAsync(MinimalRequest());

        // batch_external_id and payer are null, so both are omitted entirely.
        Assert.Equal(
            """{"processing_type":"cardpay","method":"purchase","currency":"UAH","orders":[{"api_key":"11111111-1111-1111-1111-111111111111","amount":100.50,"external_id":"order-1"}]}""",
            Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task Create_ShouldSerializePayerAndMultipleOrders()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        await Exp354TestContext.PaymentInstructions(handler, declineClient).CreateAsync(
            new CreatePaymentInstructionsRequest
            {
                ProcessingType = PaymentInstructionProcessingType.PPay,
                Method = PaymentInstructionMethod.Auth,
                Currency = "UAH",
                BatchExternalId = "batch-1",
                Payer = new PaymentInstructionPayer
                {
                    Tin = "1234567890",
                    FirstName = "Тарас",
                    LastName = "Шевченко",
                    Patronym = "Григорович"
                },
                Orders =
                [
                    new PaymentInstructionOrder
                    {
                        ApiKey = "11111111-1111-1111-1111-111111111111",
                        Amount = 100.50m,
                        ExternalId = "order-1",
                        Description = "First"
                    },
                    new PaymentInstructionOrder
                    {
                        ApiKey = "22222222-2222-2222-2222-222222222222",
                        Amount = 0.01m,
                        ExternalId = "order-2"
                    }
                ]
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.NotNull(recorded.Body);

        using System.Text.Json.JsonDocument body = System.Text.Json.JsonDocument.Parse(recorded.Body!);
        System.Text.Json.JsonElement root = body.RootElement;

        Assert.Equal("ppay", root.GetProperty("processing_type").GetString());
        Assert.Equal("auth", root.GetProperty("method").GetString());
        Assert.Equal("batch-1", root.GetProperty("batch_external_id").GetString());
        Assert.Equal("1234567890", root.GetProperty("payer").GetProperty("tin").GetString());
        Assert.Equal("Тарас", root.GetProperty("payer").GetProperty("first_name").GetString());
        Assert.Equal("Григорович", root.GetProperty("payer").GetProperty("patronym").GetString());

        System.Text.Json.JsonElement orders = root.GetProperty("orders");
        Assert.Equal(2, orders.GetArrayLength());
        Assert.Equal(100.50m, orders[0].GetProperty("amount").GetDecimal());
        Assert.Equal("First", orders[0].GetProperty("description").GetString());
        Assert.Equal(0.01m, orders[1].GetProperty("amount").GetDecimal());

        // The second order has no description, so the property is absent rather than null.
        Assert.False(orders[1].TryGetProperty("description", out _));
    }

    /// <summary>
    /// The order amount is the one decimal in this ticket: the official schema declares a number, so it is
    /// modelled as <see cref="decimal"/> and must reach the wire as a JSON number.
    /// </summary>
    [Fact]
    public async Task Create_ShouldSerializeTheOrderAmountAsAJsonNumber()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        await Exp354TestContext.PaymentInstructions(handler, declineClient).CreateAsync(MinimalRequest());

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"amount\":100.50", recorded.Body!);
        Assert.DoesNotContain("\"amount\":\"100.50\"", recorded.Body!);
    }

    [Fact]
    public async Task Create_ShouldMapTheTypedResponse()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "currency": "UAH",
              "batch_external_id": "batch-1",
              "batch_url": "https://provider.example/batch/1",
              "batch_download_url": "https://provider.example/batch/1/download",
              "instructions": [
                {
                  "id": "pi-1",
                  "external_id": "order-1",
                  "project_id": "project-1",
                  "number": 1001,
                  "url": "https://provider.example/pi/1",
                  "download_url": "https://provider.example/pi/1/download"
                }
              ]
            }
            """);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));

        PaymentInstructionsResult result = await Exp354TestContext
            .PaymentInstructions(handler, declineClient)
            .CreateAsync(MinimalRequest());

        Assert.Equal("UAH", result.Currency);
        Assert.Equal("batch-1", result.BatchExternalId);
        Assert.Equal("https://provider.example/batch/1", result.BatchUrl);
        Assert.Equal("https://provider.example/batch/1/download", result.BatchDownloadUrl);

        PaymentInstruction instruction = Assert.Single(result.Instructions!);
        Assert.Equal("pi-1", instruction.Id);
        Assert.Equal("order-1", instruction.ExternalId);
        Assert.Equal("project-1", instruction.ProjectId);
        Assert.Equal(1001m, instruction.Number);
        Assert.Equal("https://provider.example/pi/1", instruction.Url);
        Assert.Equal("https://provider.example/pi/1/download", instruction.DownloadUrl);
    }

    /// <summary>
    /// <c>createPaymentInstructions</c> is the tenth EXP-354 operation, and the only one whose service-level
    /// logging was not covered by a leak test. Its request carries payer identity and order identifiers, and
    /// its response carries provider URLs, so neither may reach a log sink.
    /// </summary>
    /// <remarks>
    /// The route is a constant with no caller input in it, so <see cref="BaseService"/> is expected to pass
    /// this on inspection. The test exists as a regression guard: if a later change gives this operation a
    /// query parameter, or starts logging the serialized body, it fails here instead of in production.
    /// </remarks>
    [Fact]
    public async Task Create_ShouldLogTheStaticRouteAndNothingFromTheRequestOrResponse()
    {
        // Unmistakably synthetic markers: long, unique, and obviously not real values, so a leak assertion
        // cannot pass by accident and a secret scanner has nothing to flag.
        const string batchMarker = "batch-external-id-must-never-be-logged-EXP354";
        const string tinMarker = "payer-tin-must-never-be-logged-EXP354";
        const string firstNameMarker = "payer-first-name-must-never-be-logged-EXP354";
        const string lastNameMarker = "payer-last-name-must-never-be-logged-EXP354";
        const string patronymMarker = "payer-patronym-must-never-be-logged-EXP354";
        const string apiKeyMarker = "order-api-key-must-never-be-logged-EXP354";
        const string orderExternalIdMarker = "order-external-id-must-never-be-logged-EXP354";
        const string descriptionMarker = "order-description-must-never-be-logged-EXP354";
        const string responseBatchUrlMarker = "response-batch-url-must-never-be-logged-EXP354";
        const string responseInstructionIdMarker = "response-instruction-id-must-never-be-logged-EXP354";
        const string responseProjectIdMarker = "response-project-id-must-never-be-logged-EXP354";

        RecordingHandler authenticated = RecordingHandler.Json($$"""
            {
              "currency": "UAH",
              "batch_external_id": "{{batchMarker}}",
              "batch_url": "https://provider.example/{{responseBatchUrlMarker}}",
              "batch_download_url": "https://provider.example/{{responseBatchUrlMarker}}/download",
              "instructions": [
                {
                  "id": "{{responseInstructionIdMarker}}",
                  "external_id": "{{orderExternalIdMarker}}",
                  "project_id": "{{responseProjectIdMarker}}",
                  "number": 1001,
                  "url": "https://provider.example/pi/{{responseInstructionIdMarker}}",
                  "download_url": "https://provider.example/pi/{{responseInstructionIdMarker}}/download"
                }
              ]
            }
            """);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));
        RecordingLogger logger = new();

        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(authenticated, declineClient, logger: logger);

        PaymentInstructionsResult result;
        using (service as IDisposable)
        {
            result = await service.CreateAsync(new CreatePaymentInstructionsRequest
            {
                ProcessingType = PaymentInstructionProcessingType.CardPay,
                Method = PaymentInstructionMethod.Purchase,
                Currency = "UAH",
                BatchExternalId = batchMarker,
                Payer = new PaymentInstructionPayer
                {
                    Tin = tinMarker,
                    FirstName = firstNameMarker,
                    LastName = lastNameMarker,
                    Patronym = patronymMarker
                },
                Orders =
                [
                    new PaymentInstructionOrder
                    {
                        ApiKey = apiKeyMarker,
                        Amount = 100.50m,
                        ExternalId = orderExternalIdMarker,
                        Description = descriptionMarker
                    }
                ]
            });
        }

        // The markers really did travel, in the request body and back in the response, so the assertions
        // below are about logging rather than about values that were never there.
        Exp354Request recorded = Assert.Single(authenticated.Requests);
        Assert.Contains(tinMarker, recorded.Body!, StringComparison.Ordinal);
        Assert.Contains(apiKeyMarker, recorded.Body!, StringComparison.Ordinal);
        Assert.Equal(responseInstructionIdMarker, Assert.Single(result.Instructions!).Id);

        // The static route is what the log carries.
        Assert.Contains(logger.StateValues, value => value.Contains(CreateEndpoint, StringComparison.Ordinal));

        string[] forbidden =
        [
            batchMarker,
            tinMarker,
            firstNameMarker,
            lastNameMarker,
            patronymMarker,
            apiKeyMarker,
            orderExternalIdMarker,
            descriptionMarker,
            responseBatchUrlMarker,
            responseInstructionIdMarker,
            responseProjectIdMarker
        ];

        foreach (string text in logger.AllText)
        {
            foreach (string marker in forbidden)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }

        // No body fragment of either direction reaches a sink either.
        Assert.All(
            logger.AllText,
            text =>
            {
                Assert.DoesNotContain("\"payer\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"orders\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"instructions\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("processing_type", text, StringComparison.Ordinal);
                Assert.DoesNotContain("100.50", text, StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task Create_ShouldRejectNullRequest()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));
        PaymentInstructionService service = Exp354TestContext.PaymentInstructions(handler, declineClient);

        Assert.Equal(
            "request",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!))).ParamName);
        Assert.Empty(handler.Requests);
    }

    // ===================== decline: request shape =====================

    [Fact]
    public async Task Decline_ShouldSendOneUnauthenticatedBodylessGet()
    {
        RecordingHandler authenticated = RecordingHandler.Json("{}");
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);

        PaymentInstructionDeclineResult result = await Exp354TestContext
            .PaymentInstructions(authenticated, declineClient)
            .DeclineAsync("project-1", "pi-1");

        // Exactly one request, and it went over the decline client - not the authenticated one.
        Exp354Request recorded = Assert.Single(decline.Requests);
        Assert.Empty(authenticated.Requests);

        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal(
            $"{DeclineEndpoint}?project_id=project-1&payment_instruction_id=pi-1",
            recorded.RequestUri.PathAndQuery);
        Assert.False(recorded.HasContent);
        Assert.Null(recorded.Body);
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
    }

    /// <summary>
    /// The whole point of the separate client: no RozetkaPay credential may be attached to a request whose
    /// redirect target is chosen by the provider.
    /// </summary>
    [Fact]
    public async Task Decline_ShouldSendNoCredentialHeader()
    {
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);

        await Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient, Exp354TestContext.WithCustomerAuth())
            .DeclineAsync("project-1", "pi-1");

        Exp354Request recorded = Assert.Single(decline.Requests);

        foreach (string headerName in CredentialHeaderNames)
        {
            Assert.False(
                recorded.Headers.ContainsKey(headerName),
                $"The decline request must not carry '{headerName}'.");
        }
    }

    [Theory]
    [InlineData(Exp354TestContext.HostileRawId, Exp354TestContext.HostileEncodedId)]
    [InlineData(Exp354TestContext.LooksEncodedRawId, Exp354TestContext.LooksEncodedExpectedId)]
    [InlineData("project-1", "project-1")]
    [InlineData("", "")]
    public async Task Decline_ShouldEscapeBothIdentifiersExactlyOnce(string rawId, string expectedValue)
    {
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);

        await Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient)
            .DeclineAsync(rawId, rawId);

        Exp354Request recorded = Assert.Single(decline.Requests);

        // Deterministic order: project_id first, payment_instruction_id second.
        Assert.Equal(
            $"{DeclineEndpoint}?project_id={expectedValue}&payment_instruction_id={expectedValue}",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(DeclineEndpoint, recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
    }

    [Fact]
    public async Task Decline_ShouldRejectNullIdentifiers()
    {
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        Assert.Equal(
            "projectId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeclineAsync(null!, "pi-1"))).ParamName);
        Assert.Equal(
            "paymentInstructionId",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.DeclineAsync("project-1", null!))).ParamName);

        Assert.Empty(decline.Requests);
    }

    // ===================== decline: redirect handling =====================

    [Theory]
    [InlineData("https://provider.example/declined")]
    [InlineData("https://provider.example/declined?status=ok&id=1")]
    [InlineData("/relative/target")]
    public async Task Decline_ShouldReturnTheLocationWithoutFetchingIt(string location)
    {
        RecordingHandler decline = RecordingHandler.Redirect(location);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);

        PaymentInstructionDeclineResult result = await Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient)
            .DeclineAsync("project-1", "pi-1");

        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal(location, result.Location.ToString());

        // One request only: the target was never fetched.
        Assert.Single(decline.Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Decline_ShouldThrowWhenLocationIsMissing(string? location)
    {
        RecordingHandler decline = RecordingHandler.Redirect(location);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.DeclineAsync(Exp354TestContext.SecretProjectId, Exp354TestContext.SecretInstructionId));

        // The message is static: it repeats neither identifier.
        Assert.DoesNotContain(Exp354TestContext.SecretProjectId, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Exp354TestContext.SecretInstructionId, exception.Message, StringComparison.Ordinal);
        Assert.Single(decline.Requests);
    }

    /// <summary>
    /// A <c>Location</c> the runtime cannot parse must fail loudly rather than yield a null target, and the
    /// failure must not echo the header value back into a message or a log.
    /// </summary>
    [Fact]
    public async Task Decline_ShouldThrowSafelyWhenLocationIsMalformed()
    {
        const string malformedLocation = "http://[not-a-valid-authority";
        RecordingHandler decline = RecordingHandler.Redirect(malformedLocation);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        RecordingLogger logger = new();
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient, logger: logger);

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.DeclineAsync("project-1", "pi-1"));

        Assert.DoesNotContain(malformedLocation, exception.Message, StringComparison.Ordinal);
        Assert.All(
            logger.AllText,
            text => Assert.DoesNotContain(malformedLocation, text, StringComparison.Ordinal));
        Assert.Single(decline.Requests);
    }

    /// <summary>
    /// The official document declares no successful status other than <c>302</c>, so a <c>200</c> is a
    /// protocol failure rather than a silent success.
    /// </summary>
    [Fact]
    public async Task Decline_ShouldThrowWhenTheProviderAnswersSuccessInsteadOfRedirect()
    {
        RecordingHandler decline = RecordingHandler.Status(HttpStatusCode.OK);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.DeclineAsync("project-1", "pi-1"));

        Assert.Contains("302", exception.Message, StringComparison.Ordinal);
        Assert.Single(decline.Requests);
    }

    [Theory]
    [MemberData(nameof(DeclineErrorMappings))]
    public async Task Decline_ShouldMapErrorsThroughTheExistingExceptions(
        HttpStatusCode status,
        Type expectedExceptionType)
    {
        RecordingHandler decline = RecordingHandler.Error(
            status,
            """{"code":"instruction_locked","message":"Instruction cannot be declined","error_id":"req-19"}""");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        RozetkaPayException failure = (RozetkaPayException)await Assert.ThrowsAnyAsync<Exception>(
            () => service.DeclineAsync("project-1", "pi-1"));

        Assert.IsType(expectedExceptionType, failure);
        Assert.NotNull(failure.ApiError);
        Assert.Equal(status, failure.ApiError!.StatusCode);
        Assert.Equal("instruction_locked", failure.ApiError.Code);
        Assert.Equal("req-19", failure.ApiError.RequestId);

        // A failed decline is not retried onto a different client, route, or authentication mode.
        Assert.Single(decline.Requests);
    }

    [Fact]
    public async Task Decline_ShouldLogNeitherIdentifierNorLocation()
    {
        const string location = "https://provider.example/declined?secret-location-marker-EXP354=1";
        RecordingHandler decline = RecordingHandler.Redirect(location);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        RecordingLogger logger = new();

        await Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient, logger: logger)
            .DeclineAsync(Exp354TestContext.SecretProjectId, Exp354TestContext.SecretInstructionId);

        string[] forbidden =
        [
            Exp354TestContext.SecretProjectId,
            Exp354TestContext.SecretInstructionId,
            "secret-location-marker-EXP354",
            location
        ];

        foreach (string text in logger.AllText)
        {
            foreach (string marker in forbidden)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }

        // The static route is what the log does carry, with no query at all.
        Assert.Contains(logger.StateValues, value => value.Contains(DeclineEndpoint, StringComparison.Ordinal));
        Assert.All(logger.AllText, text => Assert.DoesNotContain("project_id=", text, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Decline_ShouldPropagateCancellation()
    {
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using CancellationTokenSource cancellation = new();
        decline.OnRequest = (_, _) => cancellation.Cancel();
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DeclineAsync("project-1", "pi-1", cancellation.Token));

        Exp354Request recorded = Assert.Single(decline.Requests);
        Assert.True(recorded.CancellationRequestedOnArrival);
    }

    /// <summary>
    /// An already-cancelled token is rejected before the client is touched, so no request reaches a
    /// handler at all.
    /// </summary>
    [Fact]
    public async Task Decline_ShouldRejectAnAlreadyCancelledTokenBeforeSending()
    {
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DeclineAsync("project-1", "pi-1", cancellation.Token));

        Assert.Empty(decline.Requests);
    }

    // ===================== decline client construction =====================

    /// <summary>
    /// A caller-supplied decline client carrying a credential is rejected at construction. Silently
    /// stripping the header would change the behaviour of a client the caller may share elsewhere.
    /// </summary>
    [Theory]
    [InlineData("X-ON-BEHALF-OF")]
    [InlineData("X-CUSTOMER-AUTH")]
    public void Constructor_ShouldRejectADeclineClientCarryingACredentialHeader(string headerName)
    {
        using HttpClient authenticated = Exp354TestContext.CreateHttpClient(RecordingHandler.Json("{}"));
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));
        declineClient.DefaultRequestHeaders.Add(headerName, "not-a-real-value-EXP354");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PaymentInstructionService(
                Exp354TestContext.CreateConfiguration(),
                authenticated,
                declineClient));

        Assert.Equal("declineHttpClient", exception.ParamName);
        Assert.Contains(headerName, exception.Message, StringComparison.Ordinal);

        // The caller's client is left exactly as it was: nothing was stripped.
        Assert.True(declineClient.DefaultRequestHeaders.Contains(headerName));
    }

    [Fact]
    public void Constructor_ShouldRejectADeclineClientCarryingAnAuthorizationHeader()
    {
        using HttpClient authenticated = Exp354TestContext.CreateHttpClient(RecordingHandler.Json("{}"));
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(RecordingHandler.Json("{}"));
        declineClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "not-a-real-credential-EXP354");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PaymentInstructionService(
                Exp354TestContext.CreateConfiguration(),
                authenticated,
                declineClient));

        Assert.Equal("declineHttpClient", exception.ParamName);
        Assert.NotNull(declineClient.DefaultRequestHeaders.Authorization);
    }

    /// <summary>
    /// A null decline client is rejected. The cast is required rather than incidental: an untyped
    /// <see langword="null"/> as the third argument binds to the logger overload, which is the ordinary
    /// safe constructor and correctly does not throw.
    /// </summary>
    [Fact]
    public void Constructor_ShouldRejectANullDeclineClient()
    {
        using HttpClient authenticated = Exp354TestContext.CreateHttpClient(RecordingHandler.Json("{}"));

        Assert.Equal(
            "declineHttpClient",
            Assert.Throws<ArgumentNullException>(() => new PaymentInstructionService(
                Exp354TestContext.CreateConfiguration(),
                authenticated,
                (HttpClient)null!)).ParamName);
    }

    /// <summary>
    /// The ordinary constructor must be safe with no caller action: it builds its own credential-free,
    /// non-redirecting client, and it leaves the authenticated client the caller handed it alone.
    /// </summary>
    /// <remarks>
    /// EXP-341 moved the credential off <see cref="HttpClient.DefaultRequestHeaders"/> and onto each
    /// request, so the authenticated client's defaults must come out of construction exactly as the caller
    /// supplied them - here, empty - while <c>CreateAsync</c> still sends the configured credentials.
    /// </remarks>
    [Fact]
    public async Task DefaultConstructor_ShouldNotMutateTheAuthenticatedClientOrTheDeclineClient()
    {
        RozetkaPayConfiguration configuration = Exp354TestContext.WithCustomerAuth();
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient authenticated = Exp354TestContext.CreateHttpClient(handler);

        Assert.Empty(authenticated.DefaultRequestHeaders);

        PaymentInstructionService service = new(configuration, authenticated);
        using (service as IDisposable)
        {
            // Construction installed nothing on the caller's collection.
            Assert.Empty(authenticated.DefaultRequestHeaders);

            await service.CreateAsync(MinimalRequest());

            // The credentials still go on the wire - they are simply request-scoped now.
            Exp354Request recorded = Assert.Single(handler.Requests);
            Assert.Equal(
                [configuration.GetBasicAuthenticationHeader()],
                recorded.Headers["Authorization"]);
            Assert.Equal([Exp354TestContext.CustomerAuthPlaceholder], recorded.Headers["X-CUSTOMER-AUTH"]);
            Assert.Equal([configuration.UserAgent], recorded.Headers["User-Agent"]);

            // And still nothing was written to the client the caller owns.
            Assert.Empty(authenticated.DefaultRequestHeaders);
        }
    }

    // ===================== non-provider loopback redirect proof =====================

    /// <summary>
    /// End-to-end proof over real sockets, using the ordinary safe constructor: the SDK sends one
    /// unauthenticated request to a loopback server that answers <c>302</c> pointing at a second loopback
    /// server. The second server must receive nothing.
    /// </summary>
    /// <remarks>
    /// Both servers bind to <c>127.0.0.1</c>, so no traffic leaves the machine and no RozetkaPay host is
    /// contacted. This is a transport-level check only; it makes no claim about provider behaviour, which
    /// is EXP-337's subject.
    /// </remarks>
    [Fact]
    public async Task Decline_OverRealSockets_ShouldNotFollowTheRedirectAndSendNoCredential()
    {
        using LoopbackServer redirectTarget = LoopbackServer.Answering("THIS-BODY-MUST-NEVER-BE-FETCHED-EXP354");
        string targetUrl = $"{redirectTarget.BaseUrl}/followed";

        using LoopbackServer declineServer = LoopbackServer.Redirecting(targetUrl);

        RozetkaPayConfiguration configuration = Exp354TestContext.WithCustomerAuth();
        configuration.BaseUrl = declineServer.BaseUrl;
        configuration.OnBehalfOf = "on-behalf-placeholder-not-a-real-value-EXP354";

        using HttpClient authenticated = new() { BaseAddress = new Uri(declineServer.BaseUrl) };

        // The ordinary constructor: the service builds its own non-redirecting decline client.
        PaymentInstructionService service = new(configuration, authenticated);

        PaymentInstructionDeclineResult result;
        using (service as IDisposable)
        {
            result = await service.DeclineAsync("project-1", "pi-1");
        }

        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal(targetUrl, result.Location.ToString());

        // Exactly one request reached the decline server, and none reached the redirect target.
        LoopbackRequest received = Assert.Single(declineServer.Requests);
        Assert.Empty(redirectTarget.Requests);

        Assert.Equal("GET", received.Method);
        Assert.Equal(
            $"{DeclineEndpoint}?project_id=project-1&payment_instruction_id=pi-1",
            received.RawUrl);

        // No credential travelled on the wire, even though the configuration carries all of them.
        foreach (string headerName in CredentialHeaderNames)
        {
            Assert.False(
                received.Headers.ContainsKey(headerName),
                $"The decline request must not carry '{headerName}' on the wire.");
        }
    }

    // ===================== disposal =====================

    /// <summary>
    /// A decline client the service created is released on dispose; a caller-supplied one is not.
    /// </summary>
    [Fact]
    public async Task Dispose_ShouldReleaseOnlyAnOwnedDeclineClient()
    {
        // Owned: created by the ordinary constructor.
        using HttpClient authenticated = Exp354TestContext.CreateHttpClient(RecordingHandler.Json("{}"));
        PaymentInstructionService owning = new(Exp354TestContext.CreateConfiguration(), authenticated);
        ((IDisposable)owning).Dispose();

        // Disposing twice is a no-op rather than a failure.
        ((IDisposable)owning).Dispose();

        // The authenticated client the caller supplied is untouched and still usable.
        Assert.NotNull(authenticated.BaseAddress);

        // Injected: the service must not dispose what it does not own.
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient injectedDeclineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService borrowing = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), injectedDeclineClient);

        ((IDisposable)borrowing).Dispose();

        // Still usable: a disposed HttpClient would throw here instead.
        using HttpResponseMessage response = await injectedDeclineClient.GetAsync(
            new Uri($"{Exp354TestContext.BaseUrl}{DeclineEndpoint}"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    /// <summary>
    /// After the owned client is disposed, the operation that used it fails loudly rather than silently
    /// falling back to the authenticated client.
    /// </summary>
    [Fact]
    public async Task Dispose_ShouldNotLeaveDeclineFallingBackToTheAuthenticatedClient()
    {
        RecordingHandler authenticatedHandler = RecordingHandler.Json("{}");
        using HttpClient authenticated = Exp354TestContext.CreateHttpClient(authenticatedHandler);
        PaymentInstructionService service = new(Exp354TestContext.CreateConfiguration(), authenticated);

        ((IDisposable)service).Dispose();

        await Assert.ThrowsAnyAsync<Exception>(() => service.DeclineAsync("project-1", "pi-1"));

        // Nothing was ever sent over the authenticated client.
        Assert.Empty(authenticatedHandler.Requests);
    }

    private static CreatePaymentInstructionsRequest MinimalRequest()
    {
        return new CreatePaymentInstructionsRequest
        {
            ProcessingType = PaymentInstructionProcessingType.CardPay,
            Method = PaymentInstructionMethod.Purchase,
            Currency = "UAH",
            Orders =
            [
                new PaymentInstructionOrder
                {
                    ApiKey = "11111111-1111-1111-1111-111111111111",
                    Amount = 100.50m,
                    ExternalId = "order-1"
                }
            ]
        };
    }
}
