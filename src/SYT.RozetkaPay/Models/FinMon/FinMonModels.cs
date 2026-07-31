using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;

namespace SYT.RozetkaPay.Models.FinMon;


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