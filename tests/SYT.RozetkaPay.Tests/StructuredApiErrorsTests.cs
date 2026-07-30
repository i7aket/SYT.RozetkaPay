using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// EXP-334: the public shape and constructor contract of the structured API error detail.
/// </summary>
public class RozetkaPayApiErrorModelTests
{
    [Fact]
    public void RozetkaPayApiError_ShouldBePublicAndSealed()
    {
        Type type = typeof(RozetkaPayApiError);

        Assert.True(type.IsPublic, "RozetkaPayApiError must be public.");
        Assert.True(type.IsSealed, "RozetkaPayApiError must be sealed.");
        Assert.False(type.IsAbstract, "RozetkaPayApiError must not be abstract.");
        Assert.Equal("SYT.RozetkaPay.Exceptions", type.Namespace);
    }

    [Fact]
    public void RozetkaPayApiError_ShouldExposeExactlyTheFourRequiredGetOnlyProperties()
    {
        Type type = typeof(RozetkaPayApiError);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[] { "Code", "RawBody", "RequestId", "StatusCode" },
            properties.Select(property => property.Name).Order(StringComparer.Ordinal));

        foreach (PropertyInfo property in properties)
        {
            Assert.True(property.CanRead, $"{property.Name} must be readable.");
            Assert.False(property.CanWrite, $"{property.Name} must be get-only.");
            Assert.Null(property.SetMethod);
        }

        Assert.Equal(typeof(HttpStatusCode), type.GetProperty(nameof(RozetkaPayApiError.StatusCode))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(RozetkaPayApiError.Code))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(RozetkaPayApiError.RequestId))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(RozetkaPayApiError.RawBody))!.PropertyType);
    }

    [Fact]
    public void RozetkaPayApiError_ShouldExposeASinglePublicConstructorWithTheAgreedSignature()
    {
        ConstructorInfo[] constructors = typeof(RozetkaPayApiError)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        ConstructorInfo constructor = Assert.Single(constructors);
        Assert.Equal(
            new[] { typeof(HttpStatusCode), typeof(string), typeof(string), typeof(string) },
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            new[] { "statusCode", "code", "requestId", "rawBody" },
            constructor.GetParameters().Select(parameter => parameter.Name));
    }

    [Fact]
    public void RozetkaPayApiError_ShouldNotAddBooleanMarkersOrPublicFields()
    {
        Type type = typeof(RozetkaPayApiError);

        Assert.DoesNotContain(
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void RozetkaPayApiError_ShouldNotOverrideToString()
    {
        // A custom ToString would leak the raw body into every log that formats the exception.
        MethodInfo toString = typeof(RozetkaPayApiError).GetMethod(nameof(ToString), Type.EmptyTypes)!;

        Assert.Equal(typeof(object), toString.DeclaringType);
    }

    [Fact]
    public void RozetkaPayApiError_ShouldRetainStatusCodeAndRawBodyVerbatim()
    {
        const string rawBody = """{ "code": "payment_declined",  "error_id": "abc" }""";

        RozetkaPayApiError error = new(HttpStatusCode.BadRequest, "payment_declined", "abc", rawBody);

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal("payment_declined", error.Code);
        Assert.Equal("abc", error.RequestId);
        Assert.Equal(rawBody, error.RawBody);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n  ")]
    public void RozetkaPayApiError_ShouldNormalizeBlankIdentifiersToNull(string? blank)
    {
        RozetkaPayApiError error = new(HttpStatusCode.Conflict, blank, blank, "{}");

        Assert.Null(error.Code);
        Assert.Null(error.RequestId);
    }

    [Fact]
    public void RozetkaPayApiError_ShouldTrimOuterWhitespaceFromIdentifiersOnly()
    {
        const string rawBody = "  {\"code\":\"x\"}  ";

        RozetkaPayApiError error = new(HttpStatusCode.BadRequest, "  spaced_code  ", "\treq-1\n", rawBody);

        Assert.Equal("spaced_code", error.Code);
        Assert.Equal("req-1", error.RequestId);
        Assert.Equal(rawBody, error.RawBody);
    }

    [Fact]
    public void RozetkaPayApiError_ShouldRejectNullRawBody()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new RozetkaPayApiError(HttpStatusCode.BadRequest, "code", "request", null!));

        Assert.Equal("rawBody", exception.ParamName);
    }

    [Fact]
    public void RozetkaPayApiError_ShouldAcceptEmptyRawBody()
    {
        RozetkaPayApiError error = new(HttpStatusCode.NoContent, null, null, string.Empty);

        Assert.Equal(string.Empty, error.RawBody);
        Assert.Null(error.Code);
        Assert.Null(error.RequestId);
    }

    [Fact]
    public void RozetkaPayApiError_Instances_ShouldNotShareState()
    {
        RozetkaPayApiError first = new(HttpStatusCode.BadRequest, "first_code", "first-id", "{\"n\":1}");
        RozetkaPayApiError second = new(HttpStatusCode.NotFound, "second_code", "second-id", "{\"n\":2}");

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal("first_code", first.Code);
        Assert.Equal("first-id", first.RequestId);
        Assert.Equal("{\"n\":1}", first.RawBody);

        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal("second_code", second.Code);
        Assert.Equal("second-id", second.RequestId);
        Assert.Equal("{\"n\":2}", second.RawBody);
    }
}

/// <summary>
/// EXP-334: the pre-existing public exception constructors must stay source- and behaviour-compatible.
/// </summary>
public class RozetkaPayExceptionCompatibilityTests
{
    [Fact]
    public void BaseException_ShouldExposeNullableApiErrorProperty()
    {
        PropertyInfo property = typeof(RozetkaPayException).GetProperty(nameof(RozetkaPayException.ApiError))!;

        Assert.Equal(typeof(RozetkaPayApiError), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
        Assert.Null(property.SetMethod);
        Assert.Equal(typeof(RozetkaPayException), property.DeclaringType);
    }

    [Fact]
    public void DerivedExceptions_ShouldInheritApiErrorInsteadOfRedeclaringIt()
    {
        Type[] derived =
        [
            typeof(RozetkaPayAuthorizationException),
            typeof(RozetkaPayValidationException),
            typeof(RozetkaPayRateLimitException),
            typeof(RozetkaPayNotFoundException)
        ];

        foreach (Type type in derived)
        {
            Assert.True(typeof(RozetkaPayException).IsAssignableFrom(type));
            Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        }
    }

    [Fact]
    public void PublicExceptionConstructors_ShouldKeepTheirOriginalArities()
    {
        Type[] hierarchy =
        [
            typeof(RozetkaPayException),
            typeof(RozetkaPayAuthorizationException),
            typeof(RozetkaPayValidationException),
            typeof(RozetkaPayRateLimitException),
            typeof(RozetkaPayNotFoundException)
        ];

        foreach (Type type in hierarchy)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            Assert.Equal(3, constructors.Length);
            Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
            Assert.NotNull(type.GetConstructor([typeof(string)]));
            Assert.NotNull(type.GetConstructor([typeof(string), typeof(Exception)]));
        }
    }

    [Fact]
    public void PublicExceptionConstructors_ShouldNotBeObsolete()
    {
        Type[] hierarchy =
        [
            typeof(RozetkaPayException),
            typeof(RozetkaPayAuthorizationException),
            typeof(RozetkaPayValidationException),
            typeof(RozetkaPayRateLimitException),
            typeof(RozetkaPayNotFoundException)
        ];

        foreach (Type type in hierarchy)
        {
            Assert.Null(type.GetCustomAttribute<ObsoleteAttribute>());

            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(constructor.GetCustomAttribute<ObsoleteAttribute>());
            }

            Assert.Null(typeof(RozetkaPayException)
                .GetProperty(nameof(RozetkaPayException.ApiError))!
                .GetCustomAttribute<ObsoleteAttribute>());
        }
    }

    [Fact]
    public void ManuallyConstructedExceptions_ShouldKeepMessageAndInnerExceptionAndCarryNoApiError()
    {
        InvalidOperationException inner = new("inner");

        RozetkaPayException baseParameterless = new();
        RozetkaPayException baseWithMessage = new("base");
        RozetkaPayException baseWithInner = new("base", inner);

        Assert.Null(baseParameterless.ApiError);
        Assert.Equal("base", baseWithMessage.Message);
        Assert.Null(baseWithMessage.ApiError);
        Assert.Same(inner, baseWithInner.InnerException);
        Assert.Null(baseWithInner.ApiError);

        RozetkaPayAuthorizationException authorization = new("auth", inner);
        Assert.Equal("auth", authorization.Message);
        Assert.Same(inner, authorization.InnerException);
        Assert.Null(authorization.ApiError);
        Assert.Null(new RozetkaPayAuthorizationException().ApiError);
        Assert.Null(new RozetkaPayAuthorizationException("auth").ApiError);

        RozetkaPayValidationException validation = new("validation", inner);
        Assert.Equal("validation", validation.Message);
        Assert.Same(inner, validation.InnerException);
        Assert.Null(validation.ApiError);
        Assert.Null(new RozetkaPayValidationException().ApiError);
        Assert.Null(new RozetkaPayValidationException("validation").ApiError);

        RozetkaPayRateLimitException rateLimit = new("rate", inner);
        Assert.Equal("rate", rateLimit.Message);
        Assert.Same(inner, rateLimit.InnerException);
        Assert.Null(rateLimit.ApiError);
        Assert.Null(new RozetkaPayRateLimitException().ApiError);
        Assert.Null(new RozetkaPayRateLimitException("rate").ApiError);

        RozetkaPayNotFoundException notFound = new("not-found", inner);
        Assert.Equal("not-found", notFound.Message);
        Assert.Same(inner, notFound.InnerException);
        Assert.Null(notFound.ApiError);
        Assert.Null(new RozetkaPayNotFoundException().ApiError);
        Assert.Null(new RozetkaPayNotFoundException("not-found").ApiError);
    }

    [Fact]
    public void TwoArgumentConstructors_ShouldStillAcceptANullLiteralWithoutAmbiguity()
    {
        // This test exists to compile: a public (string, RozetkaPayApiError) overload would make every
        // pre-existing `new RozetkaPayException("message", null)` call ambiguous.
        RozetkaPayException baseException = new("message", null!);
        RozetkaPayAuthorizationException authorization = new("message", null!);
        RozetkaPayValidationException validation = new("message", null!);
        RozetkaPayRateLimitException rateLimit = new("message", null!);
        RozetkaPayNotFoundException notFound = new("message", null!);

        Assert.Null(baseException.InnerException);
        Assert.Null(baseException.ApiError);
        Assert.Null(authorization.InnerException);
        Assert.Null(authorization.ApiError);
        Assert.Null(validation.InnerException);
        Assert.Null(validation.ApiError);
        Assert.Null(rateLimit.InnerException);
        Assert.Null(rateLimit.ApiError);
        Assert.Null(notFound.InnerException);
        Assert.Null(notFound.ApiError);
    }
}

/// <summary>
/// EXP-334: non-success HTTP responses must surface status, API code, request id and the raw body.
/// </summary>
public class StructuredApiErrorHttpTests
{
    private const string StandardErrorBody = """
        {"code":"test_code","message":"test message","error_id":"body-error-id"}
        """;

    public static TheoryData<HttpStatusCode, Type> StatusMappingMatrix => new()
    {
        { HttpStatusCode.BadRequest, typeof(RozetkaPayValidationException) },
        { HttpStatusCode.Unauthorized, typeof(RozetkaPayAuthorizationException) },
        { HttpStatusCode.Forbidden, typeof(RozetkaPayAuthorizationException) },
        { HttpStatusCode.NotFound, typeof(RozetkaPayNotFoundException) },
        { HttpStatusCode.TooManyRequests, typeof(RozetkaPayRateLimitException) },
        { HttpStatusCode.InternalServerError, typeof(RozetkaPayException) },
        { HttpStatusCode.Conflict, typeof(RozetkaPayException) },
        { HttpStatusCode.ServiceUnavailable, typeof(RozetkaPayException) }
    };

    [Theory]
    [MemberData(nameof(StatusMappingMatrix))]
    public async Task ErrorResponse_ShouldMapToTheExpectedExceptionWithStructuredDetails(
        HttpStatusCode statusCode,
        Type expectedExceptionType)
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(statusCode, StandardErrorBody));

        RozetkaPayException exception = await Assert.ThrowsAnyAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-1"));

        Assert.Equal(expectedExceptionType, exception.GetType());
        Assert.NotNull(exception.ApiError);
        Assert.Equal(statusCode, exception.ApiError!.StatusCode);
        Assert.Equal("test_code", exception.ApiError.Code);
        Assert.Equal("body-error-id", exception.ApiError.RequestId);
        Assert.Equal(StandardErrorBody, exception.ApiError.RawBody);
    }

    [Theory]
    [InlineData("""{"code":"payment_declined"}""", "payment_declined")]
    [InlineData("""{"code":"future_provider_code"}""", "future_provider_code")]
    [InlineData("""{"code":4200}""", "4200")]
    [InlineData("""{"code":null}""", null)]
    [InlineData("""{"code":"   "}""", null)]
    [InlineData("""{"code":{"nested":"value"}}""", null)]
    [InlineData("""{"code":["a","b"]}""", null)]
    [InlineData("""{"error":{"code":"nested_code"}}""", "nested_code")]
    [InlineData("""{"code":"top_level_code","error":{"code":"nested_code"}}""", "top_level_code")]
    [InlineData("{}", null)]
    public async Task ApiCode_ShouldBeReadAsForwardCompatibleText(string body, string? expectedCode)
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, body));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-2"));

        Assert.NotNull(exception.ApiError);
        Assert.Equal(expectedCode, exception.ApiError!.Code);
    }

    [Fact]
    public async Task ApiCode_ShouldNotBeConstrainedToTheResponseCodeEnum()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                """{"code":"code_added_by_the_provider_after_this_release"}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-3"));

        Assert.Equal("code_added_by_the_provider_after_this_release", exception.ApiError!.Code);
    }

    [Fact]
    public async Task RequestId_ShouldPreferTheXRequestIdHeader()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                StandardErrorBody,
                ("X-Request-Id", "header-request-id")));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-4"));

        Assert.Equal("header-request-id", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldMatchHeaderNamesCaseInsensitively()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                StandardErrorBody,
                ("x-rEqUeSt-iD", "odd-case-request-id")));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-5"));

        Assert.Equal("odd-case-request-id", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldFallBackToRequestIdHeader()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                StandardErrorBody,
                ("Request-Id", "plain-request-id")));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-6"));

        Assert.Equal("plain-request-id", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldPreferXRequestIdOverRequestId()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                StandardErrorBody,
                ("Request-Id", "plain-request-id"),
                ("X-Request-Id", "preferred-request-id")));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-7"));

        Assert.Equal("preferred-request-id", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldUseTheFirstNonBlankHeaderValue()
    {
        HttpResponseMessage response = ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, StandardErrorBody);
        response.Headers.TryAddWithoutValidation("X-Request-Id", "   ");
        response.Headers.TryAddWithoutValidation("X-Request-Id", "second-value");

        // Guard the premise: the blank value must really reach the SDK, otherwise this test would be vacuous.
        Assert.Equal(
            new[] { "   ", "second-value" },
            response.Headers.GetValues("X-Request-Id"));

        PaymentService service = ErrorTestContext.CreatePaymentService(response);

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-8"));

        Assert.Equal("second-value", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldFallBackToTopLevelErrorIdWhenNoHeaderIsPresent()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, """{"error_id":"body-only-id"}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-9"));

        Assert.Equal("body-only-id", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldFallBackToNestedErrorId()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                """{"error":{"error_id":"nested-body-id"}}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-10"));

        Assert.Equal("nested-body-id", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_HeaderShouldWinOverBodyErrorId()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                StandardErrorBody,
                ("X-Request-Id", "header-wins")));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-11"));

        Assert.Equal("header-wins", exception.ApiError!.RequestId);
    }

    [Theory]
    [InlineData("""{"error_id":null,"error":{"error_id":"nested-fallback-id"}}""", "nested-fallback-id")]
    [InlineData("""{"error_id":"  ","error":{"error_id":"nested-fallback-id"}}""", "nested-fallback-id")]
    [InlineData("""{"error_id":{"unexpected":"object"},"error":{"error_id":"nested-fallback-id"}}""", "nested-fallback-id")]
    [InlineData("""{"error_id":"top-level-id","error":{"error_id":"nested-fallback-id"}}""", "top-level-id")]
    public async Task RequestId_ShouldTreatTheBodyIdentifiersAsAPrecedenceChain(string body, string expectedRequestId)
    {
        // The request identifier is a precedence chain, so an unusable top-level error_id still yields to the
        // nested one. The API code is deliberately not a chain — see ApiCode_ShouldNotFallBackForAnExplicitNull.
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, body));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-chain"));

        Assert.Equal(expectedRequestId, exception.ApiError!.RequestId);
    }

    [Theory]
    [InlineData("""{"code":null,"error":{"code":"nested_code"}}""")]
    [InlineData("""{"code":{"unexpected":"object"},"error":{"code":"nested_code"}}""")]
    public async Task ApiCode_ShouldNotFallBackForAnExplicitNull(string body)
    {
        // A top-level "code" that the provider declared but left unusable stays null rather than silently
        // reporting the nested code as the top-level one.
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, body));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-declared"));

        Assert.Null(exception.ApiError!.Code);
    }

    [Fact]
    public async Task RequestId_ShouldBeNullWhenTheBodyIdentifierIsBlank()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, """{"error_id":"  "}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-12"));

        Assert.Null(exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task RequestId_ShouldBeNullWhenAbsentEverywhere()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, """{"code":"only_code"}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-13"));

        Assert.Null(exception.ApiError!.RequestId);
        Assert.Equal("only_code", exception.ApiError.Code);
    }

    [Fact]
    public async Task RequestId_ShouldIgnoreUnrelatedCorrelationHeaders()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                """{"code":"only_code"}""",
                ("traceparent", "00-11111111111111111111111111111111-2222222222222222-01"),
                ("X-Correlation-Id", "correlation-only")));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-14"));

        Assert.Null(exception.ApiError!.RequestId);
    }

    [Theory]
    [InlineData("""{"code":"compact","error_id":"id"}""")]
    [InlineData("{\n  \"code\": \"formatted\",\r\n  \"error_id\": \"id\"\n}\n")]
    [InlineData("plain text failure")]
    [InlineData("""{"code":"broken",""")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("""["array","payload"]""")]
    [InlineData("12345")]
    [InlineData("null")]
    [InlineData("\"just a json string\"")]
    public async Task RawBody_ShouldBePreservedVerbatimForEveryPayloadShape(string body)
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, body));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-15"));

        Assert.NotNull(exception.ApiError);
        Assert.Equal(body, exception.ApiError!.RawBody);
        Assert.Equal(HttpStatusCode.BadRequest, exception.ApiError.StatusCode);
    }

    [Fact]
    public async Task RawBody_ShouldBeEmptyStringWhenTheResponseHasNoContent()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-16"));

        Assert.NotNull(exception.ApiError);
        Assert.Equal(string.Empty, exception.ApiError!.RawBody);
        Assert.Null(exception.ApiError.Code);
        Assert.Null(exception.ApiError.RequestId);
    }

    [Theory]
    [InlineData("plain text failure")]
    [InlineData("""{"code":"broken",""")]
    [InlineData("""["array","payload"]""")]
    [InlineData("12345")]
    [InlineData("""{"code":{"unexpected":"object"},"message":{"unexpected":"object"}}""")]
    public async Task MalformedPayloads_ShouldNotReplaceTheSdkExceptionWithAParserError(string body)
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.NotFound, body));

        RozetkaPayNotFoundException exception = await Assert.ThrowsAsync<RozetkaPayNotFoundException>(async () =>
            await service.GetInfoAsync("id-17"));

        Assert.Equal("Resource not found", exception.Message);
        Assert.Equal(body, exception.ApiError!.RawBody);
    }

    [Fact]
    public async Task BadRequest_ShouldKeepTheProviderMessage()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, """{"message":"Invalid card number"}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-18"));

        Assert.Equal("Invalid card number", exception.Message);
    }

    [Fact]
    public async Task BadRequest_ShouldFallBackToGenericMessageForMalformedJson()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, "not-a-json"));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-19"));

        Assert.Equal("Bad request", exception.Message);
    }

    [Fact]
    public async Task DefaultStatus_ShouldUseTopLevelErrorString()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.Conflict, """{"error":"state conflict"}"""));

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-20"));

        Assert.Equal("API error: Conflict - state conflict", exception.Message);
        Assert.Equal(HttpStatusCode.Conflict, exception.ApiError!.StatusCode);
    }

    [Fact]
    public async Task DefaultStatus_ShouldOmitMessageSegmentWhenThePayloadCarriesNone()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.Conflict, """{"code":"conflict_code"}"""));

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-21"));

        Assert.Equal("API error: Conflict", exception.Message);
        Assert.Equal("conflict_code", exception.ApiError!.Code);
    }

    [Fact]
    public async Task NestedErrorMessage_ShouldBeUsedWhenNoTopLevelMessageExists()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                """{"error":{"message":"nested provider message","code":"nested_code"}}"""));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-22"));

        Assert.Equal("nested provider message", exception.Message);
        Assert.Equal("nested_code", exception.ApiError!.Code);
    }

    [Fact]
    public async Task Unauthorized_ShouldKeepItsFixedMessage()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.Unauthorized, StandardErrorBody));

        RozetkaPayAuthorizationException exception = await Assert.ThrowsAsync<RozetkaPayAuthorizationException>(async () =>
            await service.GetInfoAsync("id-23"));

        Assert.Equal("Unauthorized: Invalid credentials or deactivated account", exception.Message);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.ApiError!.StatusCode);
    }

    [Fact]
    public async Task Forbidden_ShouldKeepItsFixedMessage()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.Forbidden, StandardErrorBody));

        RozetkaPayAuthorizationException exception = await Assert.ThrowsAsync<RozetkaPayAuthorizationException>(async () =>
            await service.GetInfoAsync("id-24"));

        Assert.Equal("Forbidden: Access denied", exception.Message);
        Assert.Equal(HttpStatusCode.Forbidden, exception.ApiError!.StatusCode);
    }

    [Fact]
    public async Task NotFound_ShouldKeepItsFixedMessage()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.NotFound, StandardErrorBody));

        RozetkaPayNotFoundException exception = await Assert.ThrowsAsync<RozetkaPayNotFoundException>(async () =>
            await service.GetInfoAsync("id-25"));

        Assert.Equal("Resource not found", exception.Message);
        Assert.Equal(HttpStatusCode.NotFound, exception.ApiError!.StatusCode);
    }

    [Fact]
    public async Task InternalServerError_ShouldKeepItsFixedMessage()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.InternalServerError, StandardErrorBody));

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-26"));

        Assert.Equal("Internal server error", exception.Message);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.ApiError!.StatusCode);
    }

    [Fact]
    public async Task RateLimit_ShouldKeepRetryAfterMessageAndCarryDetails()
    {
        HttpResponseMessage response = ErrorTestContext.ErrorResponse(HttpStatusCode.TooManyRequests, StandardErrorBody);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(15));

        PaymentService service = ErrorTestContext.CreatePaymentService(response);

        RozetkaPayRateLimitException exception = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(async () =>
            await service.GetInfoAsync("id-27"));

        Assert.Equal("Rate limit exceeded. Retry after 15 seconds", exception.Message);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.ApiError!.StatusCode);
        Assert.Equal("test_code", exception.ApiError.Code);
    }

    [Fact]
    public async Task RateLimit_WithoutRetryAfterHeader_ShouldKeepTheDefaultDelayMessage()
    {
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.TooManyRequests, StandardErrorBody));

        RozetkaPayRateLimitException exception = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(async () =>
            await service.GetInfoAsync("id-28"));

        Assert.Equal("Rate limit exceeded. Retry after 60 seconds", exception.Message);
        Assert.NotNull(exception.ApiError);
    }
}

/// <summary>
/// EXP-334: the raw body is available to callers but must never reach the log or the exception text.
/// </summary>
public class StructuredApiErrorSensitiveDataTests
{
    private const string Canary = "raw-canary-value";

    private const string BodyWithCanary = """
        {"code":"test_code","error_id":"req-canary","details":{"note":"raw-canary-value"}}
        """;

    [Fact]
    public async Task RawBody_ShouldBeReachableOnlyThroughTheApiError()
    {
        TestLogger<PaymentService> logger = new();
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.InternalServerError, BodyWithCanary),
            logger);

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-canary"));

        Assert.Contains(Canary, exception.ApiError!.RawBody, StringComparison.Ordinal);
        Assert.DoesNotContain(Canary, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Canary, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logger_ShouldNotRecordTheRawBody()
    {
        TestLogger<PaymentService> logger = new();
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.InternalServerError, BodyWithCanary),
            logger);

        await Assert.ThrowsAsync<RozetkaPayException>(async () => await service.GetInfoAsync("id-canary"));

        Assert.DoesNotContain(logger.Messages, message => message.Contains(Canary, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Logger_ShouldRecordStatusCodeApiCodeAndRequestId()
    {
        TestLogger<PaymentService> logger = new();
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(HttpStatusCode.InternalServerError, BodyWithCanary),
            logger);

        await Assert.ThrowsAsync<RozetkaPayException>(async () => await service.GetInfoAsync("id-canary"));

        string errorLine = Assert.Single(
            logger.Messages,
            message => message.Contains("RozetkaPay API error", StringComparison.Ordinal));

        Assert.Contains("InternalServerError", errorLine, StringComparison.Ordinal);
        Assert.Contains("test_code", errorLine, StringComparison.Ordinal);
        Assert.Contains("req-canary", errorLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logger_ShouldNotRecordTheProviderMessageOrRequestHeaders()
    {
        TestLogger<PaymentService> logger = new();
        PaymentService service = ErrorTestContext.CreatePaymentService(
            ErrorTestContext.ErrorResponse(
                HttpStatusCode.BadRequest,
                """{"message":"customer customer-detail-placeholder was declined","error_id":"req-1"}"""),
            logger,
            customerAuth: "customer-auth-placeholder");

        await Assert.ThrowsAsync<RozetkaPayValidationException>(async () => await service.GetInfoAsync("id-canary"));

        Assert.DoesNotContain(logger.Messages, message => message.Contains("customer-detail-placeholder", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("customer-auth-placeholder", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Authorization", StringComparison.Ordinal));
    }

    [Fact]
    public void ServiceRegistration_ShouldNotExposeTheErrorDetailAsAService()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = "login";
            options.Password = "password";
        });

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(RozetkaPayApiError));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(RozetkaPayException));
    }
}

/// <summary>
/// EXP-334: every shared HTTP verb helper and the retry/fallback paths must expose the same detail object.
/// </summary>
public class StructuredApiErrorTransportTests
{
    private const string ErrorBody = """
        {"code":"verb_code","error_id":"verb-request-id"}
        """;

    [Fact]
    public async Task GetRequests_ShouldReachTheStructuredHandler()
    {
        int calls = 0;
        StubHttpMessageHandler handler = new((_, _) =>
        {
            calls++;
            return Task.FromResult(ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, ErrorBody));
        });
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-get"));

        Assert.Equal(1, calls);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        ErrorTestContext.AssertVerbDetails(exception);
    }

    [Fact]
    public async Task PostRequests_ShouldReachTheStructuredHandler()
    {
        StubHttpMessageHandler handler = new((_, _) =>
            Task.FromResult(ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, ErrorBody)));
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.RetryRefundAsync(new RetryRefundRequest { ExternalId = "payment-1" }));

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        ErrorTestContext.AssertVerbDetails(exception);
    }

    [Fact]
    public async Task PatchRequests_ShouldReachTheStructuredHandler()
    {
        StubHttpMessageHandler handler = new((_, _) =>
            Task.FromResult(ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, ErrorBody)));
        SubscriptionService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.UpdatePlanAsync("plan-1", new UpdateSubscriptionPlanRequest { Name = "plan" }));

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        ErrorTestContext.AssertVerbDetails(exception);
    }

    [Fact]
    public async Task DeleteRequests_ShouldReachTheStructuredHandler()
    {
        StubHttpMessageHandler handler = new((_, _) =>
            Task.FromResult(ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, ErrorBody)));
        SubscriptionService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.DeactivatePlanAsync("plan-1"));

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        ErrorTestContext.AssertVerbDetails(exception);
    }

    [Fact]
    public async Task PostWithNoContentRequests_ShouldReachTheStructuredHandler()
    {
        StubHttpMessageHandler handler = new((_, _) =>
            Task.FromResult(ErrorTestContext.ErrorResponse(HttpStatusCode.BadRequest, ErrorBody)));
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.ResendCallbackAsync(new ResendCallbackRequest { ExternalId = "payment-1" }));

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        ErrorTestContext.AssertVerbDetails(exception);
    }




    [Fact]
    public async Task RateLimit_WithoutRetries_ShouldExposeTheSingleResponseDetails()
    {
        int calls = 0;
        StubHttpMessageHandler handler = new((_, _) =>
        {
            calls++;
            return Task.FromResult(ErrorTestContext.ErrorResponse(
                HttpStatusCode.TooManyRequests,
                """{"code":"too_many_requests","error_id":"only-attempt"}"""));
        });
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayRateLimitException exception = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(async () =>
            await service.GetInfoAsync("id-rate"));

        Assert.Equal(1, calls);
        Assert.Equal("too_many_requests", exception.ApiError!.Code);
        Assert.Equal("only-attempt", exception.ApiError.RequestId);
    }

    [Fact]
    public async Task RateLimit_AfterExhaustedRetries_ShouldExposeTheFinalResponseDetails()
    {
        int calls = 0;
        StubHttpMessageHandler handler = new((_, _) =>
        {
            calls++;
            return Task.FromResult(ErrorTestContext.ErrorResponse(
                HttpStatusCode.TooManyRequests,
                $"{{\"code\":\"too_many_requests\",\"error_id\":\"attempt-{calls}\"}}"));
        });
        PaymentService service = new(
            ErrorTestContext.CreateConfiguration(retryPolicy: ErrorTestContext.RetryOnceImmediately()),
            ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayRateLimitException exception = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(async () =>
            await service.GetInfoAsync("id-rate"));

        Assert.Equal(2, calls);
        Assert.Equal("attempt-2", exception.ApiError!.RequestId);
    }

    [Fact]
    public async Task NetworkFailures_ShouldNotInventAnHttpApiError()
    {
        StubHttpMessageHandler handler = new((_, _) => throw new HttpRequestException("network glitch"));
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        Exception? exception = await Record.ExceptionAsync(async () => await service.GetInfoAsync("id-network"));

        Assert.NotNull(exception);
        Assert.Null((exception as RozetkaPayException)?.ApiError);
    }

    [Fact]
    public async Task DeserializationFailuresOnSuccessResponses_ShouldNotInventAnHttpApiError()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(ErrorTestContext.SuccessResponse("null")));
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-deserialize"));

        Assert.Equal("Unable to deserialize API response", exception.Message);
        Assert.Null(exception.ApiError);
    }

    [Fact]
    public async Task SuccessfulResponses_ShouldNotBeAffected()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(
            ErrorTestContext.SuccessResponse("""{"external_id":"payment-1"}""")));
        PaymentService service = new(ErrorTestContext.CreateConfiguration(), ErrorTestContext.CreateHttpClient(handler));

        PaymentStatusResult response = await service.GetInfoAsync("payment-1");

        Assert.Equal("payment-1", response.ExternalId);
    }
}

internal static class ErrorTestContext
{
    internal static void AssertVerbDetails(RozetkaPayException exception)
    {
        Assert.NotNull(exception.ApiError);
        Assert.Equal(HttpStatusCode.BadRequest, exception.ApiError!.StatusCode);
        Assert.Equal("verb_code", exception.ApiError.Code);
        Assert.Equal("verb-request-id", exception.ApiError.RequestId);
    }

    internal static PaymentService CreatePaymentService(
        HttpResponseMessage response,
        ILogger? logger = null,
        string? customerAuth = null)
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(response));
        return new PaymentService(
            CreateConfiguration(customerAuth: customerAuth),
            CreateHttpClient(handler),
            logger);
    }

    internal static HttpResponseMessage ErrorResponse(
        HttpStatusCode statusCode,
        string body,
        params (string Name, string Value)[] headers)
    {
        HttpResponseMessage response = new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        foreach ((string name, string value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }

    internal static HttpResponseMessage SuccessResponse(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    internal static RozetkaPayConfiguration CreateConfiguration(
        RetryPolicy? retryPolicy = null,
        string? customerAuth = null)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password",
            RetryPolicy = retryPolicy ?? RetryPolicy.None,
            CustomerAuth = customerAuth,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    internal static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.rozetkapay.com")
        };
    }

    internal static RetryPolicy RetryOnceImmediately()
    {
        return new RetryPolicy
        {
            Enabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffStrategy = BackoffStrategy.Fixed
        };
    }
}
