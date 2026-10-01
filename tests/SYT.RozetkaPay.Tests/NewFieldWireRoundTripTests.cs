using System.Text.Json;
using System.Text.Json.Nodes;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The fields added on 2026-09-30 go on the wire under exactly the names and tokens the document declares, and
/// read back into the same values.
/// </summary>
/// <remarks>
/// <para>
/// <c>ModelFieldCoverageTests</c> compares names by reflection; nothing serialized a request carrying
/// <c>order_recipient</c>, <c>document</c>, <c>campaign_name</c> or <c>consolidated</c> and looked at the
/// result. These tests do.
/// </para>
/// <para>
/// Expected names and tokens are read from the pinned snapshot, never retyped: a typo in a
/// <c>[JsonPropertyName]</c> or <c>[JsonStringEnumMemberName]</c> (<c>bank-name</c>, <c>foreign_passport</c>)
/// produces a key or token the document does not declare, and the test fails.
/// </para>
/// </remarks>
public class NewFieldWireRoundTripTests
{
    private static OrderRecipient Recipient() => new()
    {
        Name = "Державне казначейство України",
        Tin = "12345676",
        Iban = "UA213223130000026007233566001",
        BankName = "Казначейська служба України",
    };

    public static TheoryData<CustomerDocumentType> DocumentTypes =>
        new(Enum.GetValues<CustomerDocumentType>());

    public static TheoryData<CampaignName> CampaignNames => new(Enum.GetValues<CampaignName>());

    [Fact]
    public void OrderRecipient_ShouldWriteExactlyTheDocumentedFieldsAndReadThemBack()
    {
        OrderRecipient recipient = Recipient();

        JsonObject json = Write(recipient);

        AssertKeysAreExactly("OrderRecipient", json);
        Assert.Equal(recipient.Iban, json[Key("OrderRecipient", "iban")]!.GetValue<string>());
        Assert.Equivalent(recipient, Read<OrderRecipient>(json), strict: true);
    }

    [Theory]
    [MemberData(nameof(DocumentTypes))]
    public void CustomerDocument_ShouldWriteTheDocumentedFieldsAndTokenAndReadThemBack(CustomerDocumentType type)
    {
        CustomerDocument document = new() { Type = type, Series = "АА", Number = "123456" };

        JsonObject json = Write(document);

        AssertKeysAreExactly("CustomerDocument", json);
        Assert.Contains(
            json[Key("CustomerDocument", "type")]!.GetValue<string>(),
            OpenApiSnapshot.InlineEnumValues("CustomerDocument", "type"));
        Assert.Equivalent(document, Read<CustomerDocument>(json), strict: true);
    }

    [Fact]
    public void BatchOrder_ShouldCarryTheRecipientAndParentUnderTheDocumentedNames()
    {
        BatchOrder order = new()
        {
            ApiKey = "api-key",
            Amount = 123.45m,
            Description = "Budget payment",
            ExternalId = "order-2",
            ChildOf = "order-1",
            OrderRecipient = Recipient(),
        };

        JsonObject json = Write(order);

        AssertKeysAreDeclared("BatchOrder", json, "order_recipient", "child_of");
        AssertKeysAreExactly("OrderRecipient", json["order_recipient"]!.AsObject());

        BatchOrder back = Read<BatchOrder>(json);
        Assert.Equal("order-1", back.ChildOf);
        Assert.Equivalent(order.OrderRecipient, back.OrderRecipient, strict: true);
    }

    [Fact]
    public void PaymentInstructionOrder_ShouldCarryTheRecipientUnderTheDocumentedName()
    {
        PaymentInstructionOrder order = new()
        {
            ApiKey = "00000000-0000-0000-0000-000000000001",
            Amount = 100.5m,
            ExternalId = "pi-order-1",
            OrderRecipient = Recipient(),
        };

        JsonObject json = Write(order);

        AssertKeysAreDeclared("PaymentInstructionOrder", json, "order_recipient");
        AssertKeysAreExactly("OrderRecipient", json["order_recipient"]!.AsObject());
        Assert.Equivalent(order.OrderRecipient, Read<PaymentInstructionOrder>(json).OrderRecipient, strict: true);
    }

    [Fact]
    public void BatchCustomer_ShouldCarryTheTinAndDocumentUnderTheDocumentedNames()
    {
        BatchCustomerRequestUserDetails customer = new()
        {
            Tin = "1234567890",
            Document = new CustomerDocument { Type = CustomerDocumentType.ForeignPassport, Number = "FA123456" },
        };

        JsonObject json = Write(customer);

        AssertKeysAreDeclared("BatchCustomerRequestUserDetails", json, "tin", "document");
        AssertKeysAreExactly("CustomerDocument", json["document"]!.AsObject(), except: "series");

        BatchCustomerRequestUserDetails back = Read<BatchCustomerRequestUserDetails>(json);
        Assert.Equal("1234567890", back.Tin);
        Assert.Equivalent(customer.Document, back.Document, strict: true);
    }

    [Theory]
    [MemberData(nameof(CampaignNames))]
    public void CreatePayment_ShouldCarryTheCampaignUnderTheDocumentedNameAndToken(CampaignName campaign)
    {
        CreatePaymentRequest request = new()
        {
            Amount = 10m,
            Currency = "UAH",
            ExternalId = "order-1",
            Mode = PaymentMode.Hosted,
            CampaignName = campaign,
        };

        JsonObject json = Write(request);

        // The body createPayment references (a request body, not a component schema), read through the operation.
        AssertKeysAreDeclared(
            "createPayment", OpenApiSnapshot.RequestFieldsOf("POST", "/api/payments/v1/new"), json, "campaign_name");
        Assert.Contains(json["campaign_name"]!.GetValue<string>(), OpenApiSnapshot.EnumValues("CampaignName"));
        Assert.Equal(campaign, Read<CreatePaymentRequest>(json).CampaignName);
    }

    /// <summary>
    /// <c>consolidated</c> is a query parameter, not a body field: the decline request carries exactly the
    /// parameters the operation declares, under their declared names.
    /// </summary>
    [Fact]
    public async Task Decline_Consolidated_ShouldSendOnlyTheDeclaredQueryParameters()
    {
        const string declinePath = "/api/payment-instructions/v1/decline";
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);

        await Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient)
            .DeclineAsync("project-1", "pi-1", consolidated: true);

        Exp354Request recorded = Assert.Single(decline.Requests);
        Dictionary<string, string> sent = recorded.RequestUri.Query.TrimStart('?')
            .Split('&')
            .Select(static pair => pair.Split('=', 2))
            .ToDictionary(static pair => pair[0], static pair => pair[1], StringComparer.Ordinal);

        Assert.Equal(declinePath, recorded.RequestUri.AbsolutePath);
        Assert.Equal(
            OpenApiSnapshot.QueryParameterNamesOf("GET", declinePath).Order(StringComparer.Ordinal),
            sent.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("true", sent["consolidated"]);
    }

    private static JsonObject Write<T>(T value)
    {
        return JsonSerializer.SerializeToNode(value, SdkSerializerOptions.Value)!.AsObject();
    }

    private static T Read<T>(JsonObject json)
    {
        return JsonSerializer.Deserialize<T>(json.ToJsonString(), SdkSerializerOptions.Value)!;
    }

    /// <summary>
    /// The document's spelling of a key, asserted to exist there rather than trusted.
    /// </summary>
    private static string Key(string schemaName, string key)
    {
        Assert.Contains(key, OpenApiSnapshot.PropertyNamesOfSchema(schemaName));
        return key;
    }

    private static void AssertKeysAreExactly(string schemaName, JsonObject json, params string[] except)
    {
        Assert.Equal(
            OpenApiSnapshot.PropertyNamesOfSchema(schemaName).Except(except).Order(StringComparer.Ordinal),
            json.Select(static property => property.Key).Order(StringComparer.Ordinal));
    }

    private static void AssertKeysAreDeclared(string schemaName, JsonObject json, params string[] mustBePresent)
    {
        AssertKeysAreDeclared(schemaName, OpenApiSnapshot.PropertyNamesOfSchema(schemaName), json, mustBePresent);
    }

    private static void AssertKeysAreDeclared(
        string schemaName,
        IReadOnlyCollection<string> declared,
        JsonObject json,
        params string[] mustBePresent)
    {
        string[] undeclared = [.. json.Select(static property => property.Key).Where(key => !declared.Contains(key))];
        Assert.Empty(undeclared);
        foreach (string key in mustBePresent)
        {
            Assert.Contains(key, declared);
            Assert.True(json.ContainsKey(key), $"{schemaName} must carry '{key}' on the wire.");
        }
    }
}
