using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;

namespace SYT.RozetkaPay.Models.FinMon;

/// <summary>
/// Request to submit transaction to FinMon for analysis
/// </summary>
public class FinMonTransactionRequest
{
    /// <summary>
    /// Transaction amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// External transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_type")]
    public string? TransactionType { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public FinMonCustomer? Customer { get; set; }

    /// <summary>
    /// Card information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card")]
    public FinMonCard? Card { get; set; }

    /// <summary>
    /// Transaction timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// Customer information for FinMon analysis
/// </summary>
public class FinMonCustomer
{
    /// <summary>
    /// Customer ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Customer email (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer phone (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer IP address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Customer registration date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("registration_date")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? RegistrationDate { get; set; }
}

/// <summary>
/// Card information for FinMon analysis
/// </summary>
public class FinMonCard
{
    /// <summary>
    /// Card BIN (first 6 digits) (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bin")]
    public string? Bin { get; set; }

    /// <summary>
    /// Last four digits of card (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_four")]
    public string? LastFour { get; set; }

    /// <summary>
    /// Card brand (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Card type (debit/credit) (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_type")]
    public string? CardType { get; set; }

    /// <summary>
    /// Card issuing country (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("issuing_country")]
    public string? IssuingCountry { get; set; }
}

/// <summary>
/// FinMon transaction response
/// </summary>
public class FinMonTransactionResponse
{
    /// <summary>
    /// Transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Analysis status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Risk score (0-100) (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("risk_score")]
    public decimal? RiskScore { get; set; }

    /// <summary>
    /// Risk level (low/medium/high) (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("risk_level")]
    public string? RiskLevel { get; set; }

    /// <summary>
    /// Triggered rules (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("triggered_rules")]
    public List<TriggeredRule>? TriggeredRules { get; set; }

    /// <summary>
    /// Recommended action (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recommended_action")]
    public string? RecommendedAction { get; set; }

    /// <summary>
    /// Additional details (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Triggered rule information
/// </summary>
public class TriggeredRule
{
    /// <summary>
    /// Rule ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rule_id")]
    public string? RuleId { get; set; }

    /// <summary>
    /// Rule name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rule_name")]
    public string? RuleName { get; set; }

    /// <summary>
    /// Rule severity (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    /// <summary>
    /// Rule description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// FinMon status response
/// </summary>
public class FinMonStatusResponse
{
    /// <summary>
    /// Transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Current status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Last update timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("updated_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Status history (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_history")]
    public List<StatusHistoryItem>? StatusHistory { get; set; }
}

/// <summary>
/// Status history item
/// </summary>
public class StatusHistoryItem
{
    /// <summary>
    /// Status value (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Status comment (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

/// <summary>
/// FinMon rules response
/// </summary>
public class FinMonRulesResponse
{
    /// <summary>
    /// List of rules (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rules")]
    public List<FinMonRule>? Rules { get; set; }

    /// <summary>
    /// Total count of rules (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }
}

/// <summary>
/// FinMon rule definition
/// </summary>
public class FinMonRule
{
    /// <summary>
    /// Rule ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Rule name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Rule description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Rule category (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Rule severity (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    /// <summary>
    /// Whether rule is active (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Rule conditions (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("conditions")]
    public List<RuleCondition>? Conditions { get; set; }
}

/// <summary>
/// Rule condition definition
/// </summary>
public class RuleCondition
{
    /// <summary>
    /// Field name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    /// <summary>
    /// Operator (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    /// <summary>
    /// Condition value (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Request to submit FinMon transaction
/// </summary>
public class SubmitFinMonTransactionRequest
{
    /// <summary>
    /// Transaction amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// External transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_type")]
    public string? TransactionType { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public FinMonCustomer? Customer { get; set; }

    /// <summary>
    /// Card information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card")]
    public FinMonCard? Card { get; set; }

    /// <summary>
    /// Transaction timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// FinMon P2P payment pre-limits response
/// </summary>
public class FinMonP2PPaymentPreLimitsResponse
{
    /// <summary>
    /// Recipient IPN (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recipient_ipn")]
    public string? RecipientIpn { get; set; }

    /// <summary>
    /// Amount left for transfers (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount_left")]
    public decimal? AmountLeft { get; set; }

    /// <summary>
    /// Total count left for transfers (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("total_count_left")]
    public int? TotalCountLeft { get; set; }

    /// <summary>
    /// Card-only count left for transfers (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_only_count_left")]
    public int? CardOnlyCountLeft { get; set; }
} 