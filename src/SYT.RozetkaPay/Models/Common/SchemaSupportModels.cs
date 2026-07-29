using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Models.Common;

// Types the published schemas reference and the SDK had no counterpart for, which is why the
// properties pointing at them could not be modelled.

/// <summary>
/// The <c>Fiscalization</c> schema.
/// </summary>
public class Fiscalization
{
    /// <summary>
    /// Fiscalization identifier
    /// </summary>
    [JsonPropertyName("fiscal_id")]
    public string? FiscalId { get; set; }
    /// <summary>
    /// Fiscalization receipt URL
    /// </summary>
    [JsonPropertyName("fiscal_url")]
    public string? FiscalUrl { get; set; }
    /// <summary>
    /// Fiscalization status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    /// <summary>
    /// Fiscalization status code - 1000: Test result - 1001: Operation successful - 1002: Operation in progress - 200
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }
    /// <summary>
    /// Fiscalization status description
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }
}

/// <summary>
/// The <c>SubscriptionPaymentMeta</c> schema.
/// </summary>
public class SubscriptionPaymentMeta
{

    /// <summary>
    /// Payment method the subscription payment used.
    /// </summary>
    [JsonPropertyName("payment_method")]
    public SubscriptionPaymentMetaPaymentMethod? PaymentMethod { get; set; }
}

/// <summary>
/// The <c>PayPartsPaymentMethod</c> schema.
/// </summary>
public class PayPartsPaymentMethod
{
    /// <summary>
    /// Provider field <c>type</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    /// <summary>
    /// Provider field <c>card</c>.
    /// </summary>
    [JsonPropertyName("card")]
    public PayPartsCardPaymentMethod? Card { get; set; }
    /// <summary>
    /// Provider field <c>cc_token</c>.
    /// </summary>
    [JsonPropertyName("cc_token")]
    public CustomerCCTokenRequestPaymentMethod? CcToken { get; set; }
}

/// <summary>
/// The <c>RecipientIbanRequestPaymentMethod</c> schema.
/// </summary>
public class RecipientIbanRequestPaymentMethod
{
    /// <summary>
    /// Provider field <c>iban</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }
}

/// <summary>
/// The <c>CustomerDecryptedApplePayRequestPaymentMethod</c> schema.
/// </summary>
public class CustomerDecryptedApplePayRequestPaymentMethod
{
    /// <summary>
    /// Provider field <c>application_primary_account_number</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("application_primary_account_number")]
    public string? ApplicationPrimaryAccountNumber { get; set; }
    /// <summary>
    /// Provider field <c>application_expiration_date</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("application_expiration_date")]
    public string? ApplicationExpirationDate { get; set; }
    /// <summary>
    /// Provider field <c>online_payment_cryptogram</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("online_payment_cryptogram")]
    public string? OnlinePaymentCryptogram { get; set; }
    /// <summary>
    /// Provider field <c>currency_code</c>.
    /// </summary>
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }
    /// <summary>
    /// Provider field <c>device_manufacturer_identifier</c>.
    /// </summary>
    [JsonPropertyName("device_manufacturer_identifier")]
    public string? DeviceManufacturerIdentifier { get; set; }
    /// <summary>
    /// Provider field <c>transaction_id</c>.
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
    /// <summary>
    /// Provider field <c>ephemeral_public_key</c>.
    /// </summary>
    [JsonPropertyName("ephemeral_public_key")]
    public string? EphemeralPublicKey { get; set; }
    /// <summary>
    /// Provider field <c>public_key_hash</c>.
    /// </summary>
    [JsonPropertyName("public_key_hash")]
    public string? PublicKeyHash { get; set; }
    /// <summary>
    /// Provider field <c>eci_indicator</c>.
    /// </summary>
    [JsonPropertyName("eci_indicator")]
    public string? EciIndicator { get; set; }
    /// <summary>
    /// Provider field <c>use_3ds_flow</c>.
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3dsFlow { get; set; }
}

/// <summary>
/// The <c>CustomerDecryptedGooglePayRequestPaymentMethod</c> schema.
/// </summary>
public class CustomerDecryptedGooglePayRequestPaymentMethod
{
    /// <summary>
    /// Provider field <c>pan</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("pan")]
    public string? Pan { get; set; }
    /// <summary>
    /// Provider field <c>exp_month</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("exp_month")]
    public int? ExpMonth { get; set; }
    /// <summary>
    /// Provider field <c>exp_year</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("exp_year")]
    public int? ExpYear { get; set; }
    /// <summary>
    /// Provider field <c>cryptogram</c>.
    /// </summary>
    [Required]
    [JsonPropertyName("cryptogram")]
    public string? Cryptogram { get; set; }
    /// <summary>
    /// Provider field <c>eci_indicator</c>.
    /// </summary>
    [JsonPropertyName("eci_indicator")]
    public string? EciIndicator { get; set; }
    /// <summary>
    /// Provider field <c>auth_method</c>.
    /// </summary>
    [JsonPropertyName("auth_method")]
    public string? AuthMethod { get; set; }
    /// <summary>
    /// Provider field <c>gateway_merchant_id</c>.
    /// </summary>
    [JsonPropertyName("gateway_merchant_id")]
    public string? GatewayMerchantId { get; set; }
    /// <summary>
    /// Provider field <c>message_id</c>.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
    /// <summary>
    /// Provider field <c>message_expiration</c>.
    /// </summary>
    [JsonPropertyName("message_expiration")]
    public string? MessageExpiration { get; set; }
    /// <summary>
    /// Provider field <c>payment_method</c>.
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }
    /// <summary>
    /// Provider field <c>protocol_version</c>.
    /// </summary>
    [JsonPropertyName("protocol_version")]
    public string? ProtocolVersion { get; set; }
    /// <summary>
    /// Provider field <c>use_3ds_flow</c>.
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3dsFlow { get; set; }
}

/// <summary>
/// Payment method recorded on a subscription payment's metadata.
/// </summary>
public class SubscriptionPaymentMetaPaymentMethod
{
    /// <summary>
    /// Provider field <c>type</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public PaymentMethodType? Type { get; set; }
    /// <summary>
    /// Card expire date
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Card mask
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }
    /// <summary>
    /// Bank short name
    /// </summary>
    [JsonPropertyName("bank_short_name")]
    public string? BankShortName { get; set; }
}
