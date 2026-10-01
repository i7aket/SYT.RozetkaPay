using System.Text.Json;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// <see cref="PaymentWebhook"/> can receive every field the document's processing callback declares.
/// </summary>
/// <remarks>
/// <para>
/// The callback body is the <c>PaymentOperationResult</c> response, but the webhook types carry their own
/// names (<c>PaymentWebhookDetails</c>, <c>WebhookCustomer</c>, …), so <c>ModelFieldCoverageTests</c>, which
/// pairs schemas with types by name, never saw them — and the 2026-09-30 fields (<c>child_of</c>,
/// <c>has_child</c>, <c>status_description_en/uk</c>, <c>recipient_*</c>) were missing from the webhook while
/// present on the response. This pairs them explicitly, following the callback's own <c>$ref</c>s.
/// </para>
/// <para>
/// Only "nothing declared is missing" is asserted. The webhook types also carry a few fields the document
/// does not declare (<c>recipient_cc_mask</c>, the customer's address); removing them is a separate decision.
/// </para>
/// </remarks>
public class WebhookModelCoverageTests
{
    private const string Callback = "CPAYProcessingCallback";

    [Fact]
    public void BothProcessingCallbacks_ShouldPostPaymentOperationResult()
    {
        Assert.Equal(("responses", "PaymentOperationResult"), OpenApiSnapshot.CallbackRequestBody(Callback));
        Assert.Equal(("responses", "PaymentOperationResult"), OpenApiSnapshot.CallbackRequestBody("CardPayProcessingCallback"));
    }

    public static TheoryData<string, string, Type> CallbackComponents()
    {
        (string section, string body) = OpenApiSnapshot.CallbackRequestBody(Callback);
        string details = OpenApiSnapshot.ReferencedSchemaOf(section, body, "details");
        string paymentMethod = OpenApiSnapshot.ReferencedSchemaOf(section, body, "payment_method");
        string customer = OpenApiSnapshot.ReferencedSchemaOf(section, body, "customer");

        return new()
        {
            { section, body, typeof(PaymentWebhook) },
            { "schemas", details, typeof(PaymentWebhookDetails) },
            { "schemas", OpenApiSnapshot.ReferencedSchemaOf("schemas", details, "fee"), typeof(WebhookFee) },
            { "schemas", paymentMethod, typeof(WebhookPaymentMethod) },
            { "schemas", OpenApiSnapshot.ReferencedSchemaOf("schemas", paymentMethod, "cc_token"), typeof(WebhookCardToken) },
            { "schemas", customer, typeof(WebhookCustomer) },
        };
    }

    [Theory]
    [MemberData(nameof(CallbackComponents))]
    public void WebhookType_ShouldCarryEveryDeclaredField(string section, string name, Type webhookType)
    {
        string[] missing = [.. OpenApiSnapshot.PropertyNamesOfComponent(section, name)
            .Except(OpenApiSnapshot.JsonPropertyNamesOf(webhookType), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(missing);
    }

    /// <summary>
    /// A callback carrying the 2026-09-30 fields reads into the webhook model. Values are the document's own
    /// examples where it gives one (<c>recipient_name</c>, <c>recipient_tin</c>, <c>recipient_bank_name</c>,
    /// <c>bin_alias_name</c>).
    /// </summary>
    [Fact]
    public void Callback_WithTheNewFields_ShouldDeserialize()
    {
        const string body = """
            {
              "id": "0",
              "external_id": "order-2",
              "batch_external_id": "batch-1",
              "child_of": "order-1",
              "has_child": false,
              "is_success": true,
              "operation": "payment",
              "metadata": { "promo_id": "12345-54321" },
              "details": {
                "status": "success",
                "status_description_en": "Payment successful",
                "status_description_uk": "Оплата успішна",
                "recipient_iban": "UA213223130000026007233566001",
                "recipient_name": "Державне казначейство України",
                "recipient_tin": "12345676",
                "recipient_bank_name": "АКЦІОНЕРНЕ ТОВАРИСТВО \"ПЕРШИЙ УКРАЇНСЬКИЙ МІЖНАРОДНИЙ БАНК\"",
                "recurrent_id": "rec-1",
                "mid": "mid-1",
                "tid": "tid-1",
                "subscription_id": "sub-1"
              },
              "payment_method": {
                "type": "cc_token",
                "cc_token": { "token": "tok", "saved_card": true, "bin_country": "UA", "bin_alias_name": "ROZETKA CARD" },
                "google_pay": { "bin_alias_name": "ROZETKA CARD" }
              },
              "customer": { "external_id": "user-1" }
            }
            """;

        PaymentWebhook webhook = JsonSerializer.Deserialize<PaymentWebhook>(body, SdkSerializerOptions.Value)!;

        Assert.Equal("batch-1", webhook.BatchExternalId);
        Assert.Equal("order-1", webhook.ChildOf);
        Assert.False(webhook.HasChild);
        Assert.Equal("12345-54321", webhook.Metadata!["promo_id"]);

        PaymentWebhookDetails details = webhook.Details!;
        Assert.Equal("Payment successful", details.StatusDescriptionEn);
        Assert.Equal("Оплата успішна", details.StatusDescriptionUk);
        Assert.Equal("UA213223130000026007233566001", details.RecipientIban);
        Assert.Equal("Державне казначейство України", details.RecipientName);
        Assert.Equal("12345676", details.RecipientTin);
        Assert.StartsWith("АКЦІОНЕРНЕ ТОВАРИСТВО", details.RecipientBankName, StringComparison.Ordinal);
        Assert.Equal("rec-1", details.RecurrentId);
        Assert.Equal("mid-1", details.Mid);
        Assert.Equal("tid-1", details.Tid);
        Assert.Equal("sub-1", details.SubscriptionId);

        Assert.True(webhook.PaymentMethod!.CardToken!.SavedCard);
        Assert.Equal("UA", webhook.PaymentMethod.CardToken.BinCountry);
        Assert.Equal("ROZETKA CARD", webhook.PaymentMethod.CardToken.BinAliasName);
        Assert.Equal("ROZETKA CARD", webhook.PaymentMethod.GooglePay!.BinAliasName);
        Assert.Equal("user-1", webhook.Customer!.ExternalId);
    }

    /// <summary>
    /// A callback from before 2026-09-30 still reads, with every new field <see langword="null"/>, and the
    /// delivery key is unchanged by them.
    /// </summary>
    [Fact]
    public void Callback_WithoutTheNewFields_ShouldLeaveThemNull()
    {
        const string body = """{"id":"p-1","operation":"payment","is_success":true,"details":{"operation_id":"op-1","status":"success"}}""";

        PaymentWebhook webhook = JsonSerializer.Deserialize<PaymentWebhook>(body, SdkSerializerOptions.Value)!;

        Assert.Null(webhook.ChildOf);
        Assert.Null(webhook.HasChild);
        Assert.Null(webhook.BatchExternalId);
        Assert.Null(webhook.Metadata);
        Assert.Null(webhook.Details!.RecipientName);
        Assert.Null(webhook.Details.StatusDescriptionEn);
        Assert.Equal("p-1|payment|op-1|success|1", webhook.EventKey);
    }
}
