using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Models.Subscriptions;

/// <summary>
/// Payment-method kind accepted by the official <c>UpdateSubscriptionPaymentMethod</c> operation.
/// </summary>
/// <remarks>
/// The wire tokens are pinned with <see cref="JsonStringEnumMemberNameAttribute"/> rather than left to
/// the SDK naming policy. <c>cc_token</c> happens to survive a snake-case policy, but pinning the whole
/// enum keeps a future policy change from silently renaming a documented token.
/// </remarks>
public enum SubscriptionPaymentMethodUpdateType
{
    /// <summary>
    /// Tokenized card (<c>cc_token</c>).
    /// </summary>
    [JsonStringEnumMemberName("cc_token")]
    CcToken,

    /// <summary>
    /// Saved wallet option (<c>wallet</c>).
    /// </summary>
    [JsonStringEnumMemberName("wallet")]
    Wallet,

    /// <summary>
    /// Google Pay token (<c>google_pay</c>).
    /// </summary>
    [JsonStringEnumMemberName("google_pay")]
    GooglePay,

    /// <summary>
    /// Apple Pay token (<c>apple_pay</c>).
    /// </summary>
    [JsonStringEnumMemberName("apple_pay")]
    ApplePay,

    /// <summary>
    /// Existing recurrent identifier (<c>recurrent_id</c>).
    /// </summary>
    [JsonStringEnumMemberName("recurrent_id")]
    RecurrentId
}

/// <summary>
/// Recurrent-identifier payment method of the official <c>UpdateSubscriptionPaymentMethod</c> request.
/// </summary>
public class SubscriptionRecurrentIdPaymentMethod
{
    /// <summary>
    /// Recurrent identifier. The official schema declares a string, so the value is carried verbatim and
    /// never parsed into a number.
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    [Required]
    public string RecurrentId { get; set; } = string.Empty;
}

/// <summary>
/// Payment method of the official <c>UpdateSubscriptionPaymentMethod</c> request.
/// </summary>
/// <remarks>
/// This is a different shape from the historical <see cref="SubscriptionPaymentMethod"/>, which stays
/// untouched for the operations that already use it.
/// </remarks>
public class SubscriptionPaymentMethodUpdate
{
    /// <summary>
    /// Which of the payment-method objects below carries the value (JSON string).
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    public SubscriptionPaymentMethodUpdateType Type { get; set; }

    /// <summary>
    /// Tokenized card details, used with <see cref="SubscriptionPaymentMethodUpdateType.CcToken"/>.
    /// </summary>
    [JsonPropertyName("cc_token")]
    public CustomerCCTokenRequestPaymentMethod? CcToken { get; set; }

    /// <summary>
    /// Saved wallet option, used with <see cref="SubscriptionPaymentMethodUpdateType.Wallet"/>.
    /// </summary>
    [JsonPropertyName("wallet")]
    public CustomerWalletRequestPaymentMethod? Wallet { get; set; }

    /// <summary>
    /// Apple Pay token, used with <see cref="SubscriptionPaymentMethodUpdateType.ApplePay"/>.
    /// </summary>
    [JsonPropertyName("apple_pay")]
    public CustomerAppleGooglePayRequestPaymentMethod? ApplePay { get; set; }

    /// <summary>
    /// Google Pay token, used with <see cref="SubscriptionPaymentMethodUpdateType.GooglePay"/>.
    /// </summary>
    [JsonPropertyName("google_pay")]
    public CustomerAppleGooglePayRequestPaymentMethod? GooglePay { get; set; }

    /// <summary>
    /// Recurrent identifier, used with <see cref="SubscriptionPaymentMethodUpdateType.RecurrentId"/>.
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public SubscriptionRecurrentIdPaymentMethod? RecurrentId { get; set; }
}

/// <summary>
/// Request body of the official <c>UpdateSubscriptionPaymentMethod</c> operation:
/// <c>PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method</c>.
/// </summary>
public class UpdateSubscriptionPaymentMethodRequest
{
    /// <summary>
    /// Optional user ID in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Optional URL the customer is returned to after a required user action (JSON string).
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// New payment method. Required by the official schema.
    /// </summary>
    [JsonPropertyName("payment_method")]
    [Required]
    public SubscriptionPaymentMethodUpdate PaymentMethod { get; set; } = new();

    /// <summary>
    /// Optional browser fingerprint used by the 3DS flow.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public BrowserFingerprint? Fingerprint { get; set; }

    /// <summary>
    /// Optional auto-renew switch (JSON boolean). This is an external wire value, not SDK state:
    /// <see langword="null"/> leaves the provider setting untouched.
    /// </summary>
    [JsonPropertyName("auto_renew")]
    public bool? AutoRenew { get; set; }
}

/// <summary>
/// Response of the official <c>UpdateSubscriptionPaymentMethod</c> operation.
/// </summary>
public class UpdateSubscriptionPaymentMethodResponse
{
    /// <summary>
    /// Provider message (JSON string). Required by the official schema.
    /// </summary>
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Action the customer still has to perform, when the provider requires one.
    /// </summary>
    [JsonPropertyName("user_action")]
    public UserAction? UserAction { get; set; }
}
