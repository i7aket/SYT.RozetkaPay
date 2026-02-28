using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.Reports;

// ===================== PAYMENTS REPORT MODELS =====================

/// <summary>
/// Request for payments report
/// </summary>
public class PaymentsReportRequest
{
    /// <summary>
    /// Start date for the report (required)
    /// </summary>
    [Required]
    [JsonPropertyName("date_from")]
    public required DateOnly DateFrom { get; set; }

    /// <summary>
    /// End date for the report (required)
    /// </summary>
    [Required]
    [JsonPropertyName("date_to")]
    public required DateOnly DateTo { get; set; }

    /// <summary>
    /// Fields to include in the report (optional, all fields if not specified)
    /// </summary>
    [JsonPropertyName("fields")]
    public List<string>? Fields { get; set; }

    /// <summary>
    /// Report scope: "current_login" or "all_keys"
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "current_login";

    /// <summary>
    /// Register type: "transactions_list" or "transactions_list_dwh"
    /// </summary>
    [JsonPropertyName("register_type")]
    public string RegisterType { get; set; } = "transactions_list";
}

/// <summary>
/// Payments report row (OpenAPI schema)
/// </summary>
public class PaymentsReportRow
{
    /// <summary>
    /// Payment amount
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Masked card number
    /// </summary>
    [JsonPropertyName("card_pan")]
    public string? CardPan { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// External payment ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Internal commission amount
    /// </summary>
    [JsonPropertyName("internal_commission")]
    public decimal? InternalCommission { get; set; }

    /// <summary>
    /// Payer external fee
    /// </summary>
    [JsonPropertyName("payer_external_fee")]
    public decimal? PayerExternalFee { get; set; }

    /// <summary>
    /// Payment ID
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Payment PayParts installments count
    /// </summary>
    [JsonPropertyName("payment_pay_parts")]
    public int? PaymentPayParts { get; set; }

    /// <summary>
    /// Payment system
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment type
    /// </summary>
    [JsonPropertyName("payment_type")]
    public string? PaymentType { get; set; }

    /// <summary>
    /// Payout amount
    /// </summary>
    [JsonPropertyName("payout_amount")]
    public decimal? PayoutAmount { get; set; }

    /// <summary>
    /// Payout date
    /// </summary>
    [JsonPropertyName("payout_date")]
    public string? PayoutDate { get; set; }

    /// <summary>
    /// Processing date
    /// </summary>
    [JsonPropertyName("processing_date")]
    public string? ProcessingDate { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Client email
    /// </summary>
    [JsonPropertyName("client_email")]
    public string? ClientEmail { get; set; }

    /// <summary>
    /// Client first name
    /// </summary>
    [JsonPropertyName("client_first_name")]
    public string? ClientFirstName { get; set; }

    /// <summary>
    /// Client last name
    /// </summary>
    [JsonPropertyName("client_last_name")]
    public string? ClientLastName { get; set; }

    /// <summary>
    /// Client patronym (middle name)
    /// </summary>
    [JsonPropertyName("client_patronym")]
    public string? ClientPatronym { get; set; }
}

/// <summary>
/// Payments report response (OpenAPI schema)
/// </summary>
public class PaymentsReportResponse
{
    /// <summary>
    /// List of payment report rows
    /// </summary>
    [JsonPropertyName("payments")]
    public List<PaymentsReportRow>? Payments { get; set; }
}

/// <summary>
/// Individual payment item in the report
/// </summary>
public class PaymentReportItem
{
    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Masked card PAN
    /// </summary>
    [JsonPropertyName("card_pan")]
    public string? CardPan { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// External payment ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Internal commission amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("internal_commission")]
    public decimal? InternalCommission { get; set; }

    /// <summary>
    /// Payer external fee (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer_external_fee")]
    public decimal? PayerExternalFee { get; set; }

    /// <summary>
    /// Payment ID
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Number of PayParts installments
    /// </summary>
    [JsonPropertyName("payment_pay_parts")]
    public int? PaymentPayParts { get; set; }

    /// <summary>
    /// Payment system (Visa, MasterCard, etc.)
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment type
    /// </summary>
    [JsonPropertyName("payment_type")]
    public string? PaymentType { get; set; }

    /// <summary>
    /// Payout amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payout_amount")]
    public decimal? PayoutAmount { get; set; }

    /// <summary>
    /// Payout date
    /// </summary>
    [JsonPropertyName("payout_date")]
    public DateTime? PayoutDate { get; set; }

    /// <summary>
    /// Processing date
    /// </summary>
    [JsonPropertyName("processing_date")]
    public DateTime? ProcessingDate { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Client email
    /// </summary>
    [JsonPropertyName("client_email")]
    public string? ClientEmail { get; set; }

    /// <summary>
    /// Client first name
    /// </summary>
    [JsonPropertyName("client_first_name")]
    public string? ClientFirstName { get; set; }

    /// <summary>
    /// Client last name
    /// </summary>
    [JsonPropertyName("client_last_name")]
    public string? ClientLastName { get; set; }

    /// <summary>
    /// Client patronym
    /// </summary>
    [JsonPropertyName("client_patronym")]
    public string? ClientPatronym { get; set; }
}

// ===================== TRANSACTIONS REPORT MODELS =====================

/// <summary>
/// Request for transactions report
/// </summary>
public class TransactionsReportRequest
{
    /// <summary>
    /// Start date for the report (required)
    /// Must not be more than 90 days in the past
    /// </summary>
    [Required]
    [JsonPropertyName("date_from")]
    public required DateOnly DateFrom { get; set; }

    /// <summary>
    /// End date for the report (required)
    /// Must not exceed 14 days from start date
    /// </summary>
    [Required]
    [JsonPropertyName("date_to")]
    public required DateOnly DateTo { get; set; }

    /// <summary>
    /// Register type
    /// </summary>
    [JsonPropertyName("register_type")]
    public string RegisterType { get; set; } = "transactions_list";

    /// <summary>
    /// Operation types to include
    /// </summary>
    [JsonPropertyName("operation_types")]
    public List<string>? OperationTypes { get; set; }

    /// <summary>
    /// Transaction statuses to include
    /// </summary>
    [JsonPropertyName("statuses")]
    public List<string>? Statuses { get; set; }
}

/// <summary>
/// Transactions report row (OpenAPI schema)
/// </summary>
public class TransactionsReportRow
{
    /// <summary>
    /// Order ID
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Transaction ID
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// External ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Operation type
    /// </summary>
    [JsonPropertyName("operation_type")]
    public string? OperationType { get; set; }

    /// <summary>
    /// Transaction status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status code
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    [JsonPropertyName("order_description")]
    public string? OrderDescription { get; set; }

    /// <summary>
    /// Payer card mask
    /// </summary>
    [JsonPropertyName("payer_card_mask")]
    public string? PayerCardMask { get; set; }

    /// <summary>
    /// Payer bank name
    /// </summary>
    [JsonPropertyName("payer_bank_name")]
    public string? PayerBankName { get; set; }

    /// <summary>
    /// BIN payment system
    /// </summary>
    [JsonPropertyName("bin_payment_system")]
    public string? BinPaymentSystem { get; set; }

    /// <summary>
    /// BIN country digit code
    /// </summary>
    [JsonPropertyName("bin_country_digit_code")]
    public string? BinCountryDigitCode { get; set; }

    /// <summary>
    /// Payer IP address
    /// </summary>
    [JsonPropertyName("payer_ip")]
    public string? PayerIp { get; set; }

    /// <summary>
    /// Original amount
    /// </summary>
    [JsonPropertyName("original_amount")]
    public decimal? OriginalAmount { get; set; }

    /// <summary>
    /// Payer amount
    /// </summary>
    [JsonPropertyName("payer_amount")]
    public decimal? PayerAmount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payer fee
    /// </summary>
    [JsonPropertyName("payer_fee")]
    public decimal? PayerFee { get; set; }

    /// <summary>
    /// Merchant fee
    /// </summary>
    [JsonPropertyName("merchant_fee")]
    public decimal? MerchantFee { get; set; }

    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Reference number (RRN)
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// Processing timestamp
    /// </summary>
    [JsonPropertyName("processed_at")]
    public string? ProcessedAt { get; set; }
}

/// <summary>
/// Transactions report response (OpenAPI schema)
/// </summary>
public class TransactionsReportResponse
{
    /// <summary>
    /// List of transaction report rows
    /// </summary>
    [JsonPropertyName("transactions")]
    public List<TransactionsReportRow>? Transactions { get; set; }
}

/// <summary>
/// Individual transaction item in the report
/// </summary>
public class TransactionReportItem
{
    /// <summary>
    /// Order ID
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Transaction ID
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// External ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Operation type
    /// </summary>
    [JsonPropertyName("operation_type")]
    public string? OperationType { get; set; }

    /// <summary>
    /// Transaction status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status code
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    [JsonPropertyName("order_description")]
    public string? OrderDescription { get; set; }

    /// <summary>
    /// Masked payer card number
    /// </summary>
    [JsonPropertyName("payer_card_mask")]
    public string? PayerCardMask { get; set; }

    /// <summary>
    /// Payer bank name
    /// </summary>
    [JsonPropertyName("payer_bank_name")]
    public string? PayerBankName { get; set; }

    /// <summary>
    /// BIN payment system
    /// </summary>
    [JsonPropertyName("bin_payment_system")]
    public string? BinPaymentSystem { get; set; }

    /// <summary>
    /// BIN country digit code
    /// </summary>
    [JsonPropertyName("bin_country_digit_code")]
    public string? BinCountryDigitCode { get; set; }

    /// <summary>
    /// Payer IP address
    /// </summary>
    [JsonPropertyName("payer_ip")]
    public string? PayerIp { get; set; }

    /// <summary>
    /// Original amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("original_amount")]
    public decimal? OriginalAmount { get; set; }

    /// <summary>
    /// Payer amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer_amount")]
    public decimal? PayerAmount { get; set; }

    /// <summary>
    /// Transaction currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payer fee (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer_fee")]
    public decimal? PayerFee { get; set; }

    /// <summary>
    /// Merchant fee (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("merchant_fee")]
    public decimal? MerchantFee { get; set; }

    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Reference Retrieval Number
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Transaction creation timestamp
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Transaction processing timestamp
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }
} 