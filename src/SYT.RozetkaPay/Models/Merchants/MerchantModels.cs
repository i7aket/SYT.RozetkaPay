using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.Merchants;

/// <summary>
/// Merchant status enumeration (JSON string as per CDN documentation)
/// </summary>
public enum MerchantStatus
{
    /// <summary>
    /// Merchant is in onboarding process
    /// </summary>
    [JsonStringEnumMemberName("onboarding")]
    Onboarding,
    
    /// <summary>
    /// Merchant is activated
    /// </summary>
    [JsonStringEnumMemberName("activated")]
    Activated,
    
    /// <summary>
    /// Merchant is blocked
    /// </summary>
    [JsonStringEnumMemberName("blocked")]
    Blocked,
    
    /// <summary>
    /// External merchant
    /// </summary>
    [JsonStringEnumMemberName("external_merchant")]
    ExternalMerchant
}

/// <summary>
/// Entity status details (JSON object as per CDN documentation)
/// </summary>
public class EntityStatusDetails
{
    /// <summary>
    /// Bank details number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank_details_number")]
    public string? BankDetailsNumber { get; set; }

    /// <summary>
    /// Business registration number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("business_registration_number")]
    public string? BusinessRegistrationNumber { get; set; }

    /// <summary>
    /// Entity ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Entity name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Entity status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Entry status details (JSON object as per CDN documentation)
/// </summary>
public class EntryStatusDetails
{
    /// <summary>
    /// Entry ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Entry name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Entry status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Merchant status response (JSON object as per CDN documentation)
/// </summary>
public class MerchantStatusResponse
{
    /// <summary>
    /// Entity details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("entity")]
    public EntityStatusDetails? Entity { get; set; }

    /// <summary>
    /// Project details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("project")]
    public EntryStatusDetails? Project { get; set; }

    /// <summary>
    /// Merchant status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public MerchantStatus? Status { get; set; }
}

/// <summary>
/// Merchant validation response
/// </summary>
public class MerchantValidationResponse
{
    /// <summary>
    /// Merchant status (e.g., "activated")
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}


/// <summary>
/// Keys validation result response (JSON object as per CDN documentation)
/// </summary>
public class KeysValidationResult
{
    /// <summary>
    /// Validation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Get merchant balance response (JSON object as per CDN documentation)
/// </summary>
public class GetMerchantBalanceResponse
{
    /// <summary>
    /// Amount on merchant's balance available for payouts (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("available_amount")]
    public decimal? AvailableAmount { get; set; }

    /// <summary>
    /// Amount on merchant's balance reserved for payout but not yet used (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("commitment_amount")]
    public decimal? CommitmentAmount { get; set; }
}

/// <summary>
/// Fee details response (JSON object as per CDN documentation)
/// </summary>
public class FeeDetailsResponse
{
    /// <summary>
    /// Online fee details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("online")]
    public PartnersFeeDetails? Online { get; set; }

    /// <summary>
    /// PNFP fee details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("pnfp")]
    public PartnersFeeDetails? Pnfp { get; set; }
}

/// <summary>
/// Transaction details list response (JSON object as per CDN documentation)
/// </summary>
public class TransactionDetailsListResponse
{
    /// <summary>
    /// List of transactions (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transactions")]
    public List<PartnersTransactionDetails>? Transactions { get; set; }
}

/// <summary>
/// Partners fee details (JSON object as per CDN documentation)
/// </summary>
public class PartnersFeeDetails
{
    /// <summary>
    /// Fixed fee amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fix")]
    public decimal? Fix { get; set; }

    /// <summary>
    /// Maximum fee amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("max")]
    public decimal? Max { get; set; }

    /// <summary>
    /// Minimum fee amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("min")]
    public decimal? Min { get; set; }

    /// <summary>
    /// Percentage fee (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("percent")]
    public decimal? Percent { get; set; }
}

/// <summary>
/// Partners transaction details (JSON object as per CDN documentation)
/// </summary>
public class PartnersTransactionDetails
{
    /// <summary>
    /// Transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Transaction amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Transaction currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Transaction status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Transaction creation timestamp (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Transaction processing timestamp (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }
} 