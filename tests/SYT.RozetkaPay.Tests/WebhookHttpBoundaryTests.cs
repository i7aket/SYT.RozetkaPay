using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using SYT.RozetkaPay.Security;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Proves the documented incoming-callback integration over a real ASP.NET Core HTTP boundary: raw bytes in
/// over a socket, signature verified before anything is deserialized, and a side effect that happens only for
/// a genuine callback.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint here is deliberately consumer-shaped rather than a new SDK surface. This package is an SDK,
/// not a web host, and <see cref="RozetkaPayWebhookSignatureVerifier"/> already exposes everything a
/// consumer needs; adding a production endpoint helper would be a public API a caller did not ask for. What
/// these tests prove is that the public surface is sufficient to build the pipeline correctly, and that the
/// pipeline fails closed at every step where it could fail open.
/// </para>
/// <para>
/// <see cref="WebhookSignatureVerifierTests"/> owns the algorithm itself, unit level. This file owns what
/// changes once a real HTTP server is in the way: body buffering, header multiplicity, content types, and the
/// ordering between verification and deserialization.
/// </para>
/// <para>
/// Every expected signature below comes from the independent Python reference vectors already pinned by
/// <see cref="WebhookSignatureVerifierTests"/>. None is produced by calling the verifier under test, so a
/// verifier that agreed with itself but not with RozetkaPay would fail here.
/// </para>
/// </remarks>
public class WebhookHttpBoundaryTests
{
    /// <summary>Merchant password from the official documentation example. Not a real credential.</summary>
    private const string OfficialPassword = "your_password";

    /// <summary>Callback body from the official documentation example.</summary>
    private const string OfficialBody = "{\"name\": \"john\", \"age\": 21}";

    /// <summary>Signature the official Python example prints for <see cref="OfficialBody"/>.</summary>
    private const string OfficialSignature = "rHk7tE6V9feV_lCvZ6ZDuzte7O4=";

    /// <summary>
    /// Ukrainian text plus a BMP emoji. Its inner Base64 contains both <c>+</c> and <c>/</c> in the standard
    /// alphabet, so this vector only passes if the inner encoding is translated to the URL-safe alphabet and
    /// the raw bytes survive the HTTP boundary as UTF-8.
    /// </summary>
    private const string NonAsciiBody =
        "{\"status\":\"success\",\"description\":\"Оплата " +
        "пройшла успішно " +
        "✅\"}";

    /// <summary>Independent signature for <see cref="NonAsciiBody"/> under <see cref="OfficialPassword"/>.</summary>
    private const string NonAsciiBodySignature = "Vf5MD_NSLPzpootqQYLax5pOL8U=";

    private const string WebhookRoute = "/webhooks/rozetkapay";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    // ===================== Accepted =====================

    [Fact]
    public async Task Webhook_ShouldAcceptTheOfficialVectorOverHttp_AndProcessItExactlyOnce()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes(OfficialBody),
            OfficialSignature);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, consumer.ProcessedCount);

        // Processing observed the same bytes the signature covers.
        Assert.Equal("john", consumer.LastProcessedName);
    }

    [Fact]
    public async Task Webhook_ShouldAcceptANonAsciiUtf8Body()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        byte[] payload = Encoding.UTF8.GetBytes(NonAsciiBody);

        // Genuinely multi-byte on the wire: if this were ASCII the case would prove nothing about encoding.
        Assert.True(payload.Length > NonAsciiBody.Length);

        HttpResponseMessage response = await consumer.PostAsync(payload, NonAsciiBodySignature);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, consumer.ProcessedCount);
    }

    [Fact]
    public async Task Webhook_ShouldVerifyRawBytesRegardlessOfTheDeclaredContentType()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        // The signature covers bytes, not a parsed document. A provider that labels the callback differently
        // must still verify, and must certainly not produce a 415 or a 500.
        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes(OfficialBody),
            OfficialSignature,
            contentType: "application/octet-stream");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, consumer.ProcessedCount);
    }

    // ===================== Rejected, fail-closed =====================

    [Fact]
    public async Task Webhook_ShouldRejectAMissingSignatureHeader()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes(OfficialBody),
            signature: null);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldRejectAMalformedSignatureHeader()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        // Not canonical base64url, so it cannot be the digest of anything.
        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes(OfficialBody),
            "this-is-not-a-signature");

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldRejectAOneByteBodyMutation()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        byte[] mutated = Encoding.UTF8.GetBytes(OfficialBody);
        mutated[^2] = (byte)(mutated[^2] ^ 0x01);

        // A genuine signature over the original body must not authenticate the mutated one.
        HttpResponseMessage response = await consumer.PostAsync(mutated, OfficialSignature);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldRejectAReserializedBodyWithTheSameSemantics()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        // Same JSON meaning, different bytes: whitespace removed and the two properties swapped. Verifying a
        // re-serialized document instead of the received bytes would accept this, and would let a proxy or a
        // model binder silently invalidate every callback.
        const string reserialized = "{\"age\":21,\"name\":\"john\"}";

        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes(reserialized),
            OfficialSignature);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldFailClosedOnMultipleSignatureHeaderValues()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        // One value is genuine. Picking whichever arrives first - or last - would let an attacker append a
        // header and choose which of two bodies is treated as authentic, so the whole request is refused.
        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes(OfficialBody),
            signatures: [OfficialSignature, "AAAAAAAAAAAAAAAAAAAAAAAAAAA="]);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldRejectAnEmptyBody()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        HttpResponseMessage response = await consumer.PostAsync([], OfficialSignature);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldRejectUnparseableContentWithoutLeakingImplementationDetail()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        // Not JSON at all. Verification runs first and refuses it, so the deserializer is never reached: the
        // answer is a 4xx with a static reason rather than a 500 carrying an exception type or a stack trace.
        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes("<<<not json at all>>>"),
            OfficialSignature);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
    }

    [Fact]
    public async Task Webhook_ShouldVerifyBeforeDeserializing()
    {
        await using WebhookConsumer consumer = await WebhookConsumer.StartAsync();

        // A body that is valid UTF-8 but broken JSON, sent with a signature that does not match it. If the
        // endpoint deserialized before verifying, this request would surface a parser failure - a 500, or a
        // 4xx whose body names the JSON error. It must instead be refused as unauthenticated.
        HttpResponseMessage response = await consumer.PostAsync(
            Encoding.UTF8.GetBytes("{\"name\": \"john\", "),
            OfficialSignature);

        await AssertRejectedWithoutProcessingAsync(consumer, response);
        Assert.Equal(0, consumer.DeserializationAttempts);
    }

    // ===================== Shared assertions =====================

    /// <summary>
    /// A refused callback answers 4xx, never 5xx, never 2xx; performs no processing; and returns a static
    /// reason that names no exception, no type, and no stack frame.
    /// </summary>
    private static async Task AssertRejectedWithoutProcessingAsync(
        WebhookConsumer consumer,
        HttpResponseMessage response)
    {
        int status = (int)response.StatusCode;
        Assert.InRange(status, 400, 499);
        Assert.Equal(0, consumer.ProcessedCount);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(WebhookConsumer.RejectionReason, body);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);

        // The merchant password must never be echoed back to whoever posted the callback.
        Assert.DoesNotContain(OfficialPassword, body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A consumer-shaped callback receiver: one ASP.NET Core endpoint on loopback that implements the
    /// documented pipeline, plus the client used to post to it.
    /// </summary>
    /// <remarks>
    /// The endpoint is the integration this SDK documents, written the way a consumer would write it:
    /// <list type="number">
    /// <item>read the raw request bytes exactly once, before anything interprets them;</item>
    /// <item>require exactly one <c>X-ROZETKAPAY-SIGNATURE</c> header value;</item>
    /// <item>verify those bytes against that value;</item>
    /// <item>only then deserialize and only then perform the side effect;</item>
    /// <item>answer <c>400</c> with a static reason for anything that does not verify.</item>
    /// </list>
    /// </remarks>
    private sealed class WebhookConsumer : IAsyncDisposable
    {
        /// <summary>The only text a refused request ever gets back.</summary>
        internal const string RejectionReason = "invalid signature";

        private readonly LoopbackWebApplication _server;

        private readonly HttpClient _client;

        private int _processedCount;

        private int _deserializationAttempts;

        private WebhookConsumer(LoopbackWebApplication server, HttpClient client)
        {
            _server = server;
            _client = client;
        }

        /// <summary>Callbacks that passed verification and were processed.</summary>
        internal int ProcessedCount => Volatile.Read(ref _processedCount);

        /// <summary>
        /// Times the endpoint attempted to deserialize a body. Stays zero for every refused request, which is
        /// what makes "verified before deserialization" observable rather than merely intended.
        /// </summary>
        internal int DeserializationAttempts => Volatile.Read(ref _deserializationAttempts);

        /// <summary>The <c>name</c> field of the last processed callback, or <see langword="null"/>.</summary>
        internal string? LastProcessedName { get; private set; }

        internal static async Task<WebhookConsumer> StartAsync()
        {
            RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);
            WebhookConsumer? consumer = null;

            LoopbackWebApplication server = await LoopbackWebApplication.StartAsync(endpoints =>
                endpoints.MapPost(WebhookRoute, context => consumer!.HandleAsync(verifier, context)));

            HttpClient client = new() { BaseAddress = server.BaseAddress, Timeout = RequestTimeout };
            consumer = new WebhookConsumer(server, client);
            return consumer;
        }

        internal Task<HttpResponseMessage> PostAsync(
            byte[] body,
            string? signature,
            string contentType = "application/json")
        {
            return PostAsync(body, signature is null ? [] : [signature], contentType);
        }

        internal async Task<HttpResponseMessage> PostAsync(
            byte[] body,
            string[] signatures,
            string contentType = "application/json")
        {
            using HttpRequestMessage request = new(HttpMethod.Post, WebhookRoute)
            {
                Content = new ByteArrayContent(body)
            };
            request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

            foreach (string signature in signatures)
            {
                // TryAddWithoutValidation so a deliberately malformed value reaches the endpoint as sent, and
                // so two values really arrive as two values.
                request.Headers.TryAddWithoutValidation(
                    RozetkaPayWebhookSignatureVerifier.SignatureHeaderName,
                    signature);
            }

            using CancellationTokenSource timeout = new(RequestTimeout);
            return await _client.SendAsync(request, timeout.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _server.DisposeAsync();
        }

        private async Task HandleAsync(
            RozetkaPayWebhookSignatureVerifier verifier,
            HttpContext context)
        {
            // 1. The raw bytes, read once, before anything interprets them. The signature covers exactly
            //    these bytes, so a re-serialized or model-bound copy would not do.
            using MemoryStream buffer = new();
            await context.Request.Body.CopyToAsync(buffer, context.RequestAborted);
            byte[] payload = buffer.ToArray();

            // 2. Exactly one signature header value. Zero is unauthenticated; more than one is ambiguous, and
            //    picking one of them would hand the choice to whoever sent the request.
            StringValues header = context.Request.Headers[RozetkaPayWebhookSignatureVerifier.SignatureHeaderName];
            if (header.Count != 1)
            {
                await RejectAsync(context);
                return;
            }

            // 3. Verify before anything else happens.
            if (!verifier.Verify(payload, header[0]))
            {
                await RejectAsync(context);
                return;
            }

            // 4. Only now is the body interpreted, and only now does the side effect happen.
            Interlocked.Increment(ref _deserializationAttempts);
            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                LastProcessedName = document.RootElement.TryGetProperty("name", out JsonElement name)
                    ? name.GetString()
                    : null;
            }
            catch (JsonException)
            {
                // An authentic callback that this consumer cannot parse is a consumer-side problem, not an
                // authentication failure. It is still not processed, and the reason stays static.
                await RejectAsync(context);
                return;
            }

            Interlocked.Increment(ref _processedCount);
            context.Response.StatusCode = StatusCodes.Status200OK;
        }

        /// <summary>
        /// Refuse a callback. The status is a plain <c>400</c> and the body is a fixed string: nothing about
        /// why verification failed, which byte differed, or what the expected digest was.
        /// </summary>
        private static async Task RejectAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(RejectionReason, Encoding.UTF8, context.RequestAborted);
        }
    }
}
