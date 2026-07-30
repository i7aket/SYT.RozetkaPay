using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;

namespace SYT.RozetkaPay.Models.FinMon;


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
/// FinMon P2P payment pre-limits response
/// </summary>
public class FinMonP2PPaymentPreLimitsResponse
{
    /// <summary>
    /// Recipient IPN (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recipient_ipn")]
    public long? RecipientIpn { get; set; }

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