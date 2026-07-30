using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.PayParts;


/// <summary>
/// PayParts bank information (JSON object as per CDN documentation)
/// </summary>
public class PayPartsBankInfo
{
    /// <summary>
    /// Bank name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Available periods for installments (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("available_periods")]
    public List<int>? AvailablePeriods { get; set; }

    /// <summary>
    /// Bank limits for order amounts (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("limits")]
    public PayPartsBankLimits? Limits { get; set; }

    /// <summary>
    /// Fee information for different periods (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("periods")]
    public List<PayPartsPeriodInfo>? Periods { get; set; }

    /// <summary>
    /// Provider field &lt;c&gt;logo_url&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;product_name&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }
}

/// <summary>
/// PayParts bank limits (JSON object as per CDN documentation)
/// </summary>
public class PayPartsBankLimits
{
    /// <summary>
    /// Minimum amount in UAH (JSON number as per CDN documentation)
    /// If absent, minimum equals to minimum required amount in create order operation
    /// </summary>
    [JsonPropertyName("min_amount")]
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Maximum amount in UAH (JSON number as per CDN documentation)
    /// If absent, there is no upper limit
    /// </summary>
    [JsonPropertyName("max_amount")]
    public decimal? MaxAmount { get; set; }
}

/// <summary>
/// PayParts period information (JSON object as per CDN documentation)
/// </summary>
public class PayPartsPeriodInfo
{
    /// <summary>
    /// Fee for the period (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fee")]
    public decimal? Fee { get; set; }

    /// <summary>
    /// Period in months (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("period")]
    public int? Period { get; set; }
}


/// <summary>
/// PayParts banks info response (JSON array as per CDN documentation)
/// </summary>
public class PayPartsBanksInfo
{
    /// <summary>
    /// List of banks information (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("banks")]
    public List<PayPartsBankInfo>? Banks { get; set; }
} 