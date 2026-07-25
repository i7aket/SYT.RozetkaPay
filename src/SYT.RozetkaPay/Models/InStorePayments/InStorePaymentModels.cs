using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.InStorePayments;

/// <summary>
/// Currency accepted by the official in-store payment operations.
/// </summary>
/// <remarks>
/// The official schema declares a string enumeration with the single value <c>"980"</c> (the ISO 4217
/// numeric code for the hryvnia). The token is pinned explicitly, because no naming policy could turn a
/// C# identifier into <c>980</c>.
/// </remarks>
public enum InStorePaymentCurrency
{
    /// <summary>
    /// Ukrainian hryvnia, sent as the literal string <c>"980"</c>.
    /// </summary>
    [JsonStringEnumMemberName("980")]
    Uah
}

/// <summary>
/// Request body of the official <c>createInStorePayment</c> operation:
/// <c>POST /api/in-store-payments/v1/create</c>.
/// </summary>
/// <remarks>
/// <see cref="Amount"/> is a string because the official schema declares a string in the smallest
/// monetary unit. It is carried verbatim so that leading zeros and the exact provider spelling survive;
/// mapping it onto <see cref="decimal"/> would rewrite the value.
/// </remarks>
public class InStorePaymentCreateRequest
{
    /// <summary>
    /// Payment identifier in the caller's system (JSON string). Required.
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// POS terminal identifier (JSON string). Required.
    /// </summary>
    [JsonPropertyName("pos_terminal_id")]
    [Required]
    public string PosTerminalId { get; set; } = string.Empty;

    /// <summary>
    /// Terminal serial number (JSON string). Required.
    /// </summary>
    [JsonPropertyName("terminal_sn")]
    [Required]
    public string TerminalSn { get; set; } = string.Empty;

    /// <summary>
    /// Amount in the smallest monetary unit, as declared text (JSON string). Required.
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// Payment currency (JSON string). Required.
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public InStorePaymentCurrency Currency { get; set; } = InStorePaymentCurrency.Uah;

    /// <summary>
    /// Optional system trace audit number (JSON string).
    /// </summary>
    [JsonPropertyName("stan")]
    public string? Stan { get; set; }

    /// <summary>
    /// Optional terminal batch identifier (JSON string).
    /// </summary>
    [JsonPropertyName("batch_id")]
    public string? BatchId { get; set; }

    /// <summary>
    /// Optional merchant identifier (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }

    /// <summary>
    /// Optional order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }
}

/// <summary>
/// Request body of the official <c>confirmInStorePayment</c> operation:
/// <c>POST /api/in-store-payments/v1/confirm</c>.
/// </summary>
/// <remarks>
/// <see cref="CardNumber"/> and <see cref="EncryptedTrack2"/> are cardholder data. The SDK never logs
/// this request, and callers must keep it out of their own logs and error reports.
/// </remarks>
public class InStorePaymentConfirmRequest
{
    /// <summary>
    /// Payment identifier in the caller's system (JSON string). Required.
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// POS terminal identifier (JSON string). Required.
    /// </summary>
    [JsonPropertyName("pos_terminal_id")]
    [Required]
    public string PosTerminalId { get; set; } = string.Empty;

    /// <summary>
    /// Optional terminal serial number (JSON string).
    /// </summary>
    [JsonPropertyName("terminal_sn")]
    public string? TerminalSn { get; set; }

    /// <summary>
    /// Optional fiscal identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fiscal_id")]
    public string? FiscalId { get; set; }

    /// <summary>
    /// Optional fiscal receipt (JSON string).
    /// </summary>
    [JsonPropertyName("fiscal_receipt")]
    public string? FiscalReceipt { get; set; }

    /// <summary>
    /// Amount in the smallest monetary unit, as declared text (JSON string). Required.
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// Optional retrieval reference number (JSON string).
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Optional card number (JSON string). Cardholder data: never log this value.
    /// </summary>
    [JsonPropertyName("card_number")]
    public string? CardNumber { get; set; }

    /// <summary>
    /// Optional acquiring bank (JSON string).
    /// </summary>
    [JsonPropertyName("bank_acquirer")]
    public string? BankAcquirer { get; set; }

    /// <summary>
    /// Optional authorization code (JSON string).
    /// </summary>
    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Optional description (JSON string).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Optional payment system (JSON string).
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Terminal payment status (JSON string). Required.
    /// </summary>
    [JsonPropertyName("pos_payment_status")]
    [Required]
    public string PosPaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Optional terminal status description (JSON string).
    /// </summary>
    [JsonPropertyName("pos_status_description")]
    public string? PosStatusDescription { get; set; }

    /// <summary>
    /// Optional merchant identifier (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }

    /// <summary>
    /// Optional order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Optional base64 encrypted track 2 data (JSON string). Cardholder data: never log this value.
    /// </summary>
    [JsonPropertyName("encrypted_track2")]
    public string? EncryptedTrack2 { get; set; }
}

/// <summary>
/// Request body of the official <c>refundInStorePayment</c> operation:
/// <c>POST /api/in-store-payments/v1/refund</c>.
/// </summary>
/// <remarks>
/// <see cref="CardNumber"/> and <see cref="EncryptedTrack2"/> are cardholder data. The SDK never logs
/// this request, and callers must keep it out of their own logs and error reports.
/// </remarks>
public class InStorePaymentRefundRequest
{
    /// <summary>
    /// Identifier of the refunded payment in the caller's system (JSON string). Required.
    /// </summary>
    [JsonPropertyName("payment_external_id")]
    [Required]
    public string PaymentExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the refund itself in the caller's system (JSON string). Required.
    /// </summary>
    [JsonPropertyName("refund_external_id")]
    [Required]
    public string RefundExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Terminal serial number (JSON string). Required.
    /// </summary>
    [JsonPropertyName("terminal_sn")]
    [Required]
    public string TerminalSn { get; set; } = string.Empty;

    /// <summary>
    /// Optional merchant identifier (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }

    /// <summary>
    /// Optional order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Optional fiscal identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fiscal_id")]
    public string? FiscalId { get; set; }

    /// <summary>
    /// Optional fiscal receipt (JSON string).
    /// </summary>
    [JsonPropertyName("fiscal_receipt")]
    public string? FiscalReceipt { get; set; }

    /// <summary>
    /// Amount in the smallest monetary unit, as declared text (JSON string). Required.
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// Payment system (JSON string). Required.
    /// </summary>
    [JsonPropertyName("payment_system")]
    [Required]
    public string PaymentSystem { get; set; } = string.Empty;

    /// <summary>
    /// Optional description (JSON string).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// POS terminal identifier (JSON string). Required.
    /// </summary>
    [JsonPropertyName("pos_terminal_id")]
    [Required]
    public string PosTerminalId { get; set; } = string.Empty;

    /// <summary>
    /// Card number (JSON string). Required. Cardholder data: never log this value.
    /// </summary>
    [JsonPropertyName("card_number")]
    [Required]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional retrieval reference number (JSON string).
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Acquiring bank (JSON string). Required.
    /// </summary>
    [JsonPropertyName("bank_acquirer")]
    [Required]
    public string BankAcquirer { get; set; } = string.Empty;

    /// <summary>
    /// Authorization code (JSON string). Required.
    /// </summary>
    [JsonPropertyName("authorization_code")]
    [Required]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional terminal payment status (JSON string).
    /// </summary>
    [JsonPropertyName("pos_payment_status")]
    public string? PosPaymentStatus { get; set; }

    /// <summary>
    /// Optional terminal status description (JSON string).
    /// </summary>
    [JsonPropertyName("pos_status_description")]
    public string? PosStatusDescription { get; set; }

    /// <summary>
    /// Optional base64 encrypted track 2 data (JSON string). Cardholder data: never log this value.
    /// </summary>
    [JsonPropertyName("encrypted_track2")]
    public string? EncryptedTrack2 { get; set; }
}

/// <summary>
/// Receipt data returned by the official <c>createInStorePayment</c> operation.
/// </summary>
/// <remarks>
/// The create, confirm and refund receipts are three different official shapes. They are modelled
/// separately so that no field appears on an operation that never returns it.
/// </remarks>
public class InStorePaymentCreateReceiptData
{
    /// <summary>
    /// Payment instruction date (JSON string).
    /// </summary>
    [JsonPropertyName("payment_instruction_date")]
    public string? PaymentInstructionDate { get; set; }

    /// <summary>
    /// Merchant name (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_name")]
    public string? MerchantName { get; set; }

    /// <summary>
    /// Merchant EDRPOU or IPN code (JSON string).
    /// </summary>
    [JsonPropertyName("edrpou_ipn")]
    public string? EdrpouIpn { get; set; }

    /// <summary>
    /// Merchant IBAN (JSON string).
    /// </summary>
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }

    /// <summary>
    /// Financial company name (JSON string).
    /// </summary>
    [JsonPropertyName("fc_name")]
    public string? FcName { get; set; }

    /// <summary>
    /// Amount as declared text (JSON string).
    /// </summary>
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    /// <summary>
    /// Fee amount as declared text (JSON string).
    /// </summary>
    [JsonPropertyName("fee_amount")]
    public string? FeeAmount { get; set; }

    /// <summary>
    /// Receipt description (JSON string).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Financial company license (JSON string).
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; set; }

    /// <summary>
    /// Merchant address (JSON string).
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// National Bank of Ukraine identifier (JSON string).
    /// </summary>
    [JsonPropertyName("id_nbu")]
    public string? IdNbu { get; set; }
}

/// <summary>
/// Receipt data returned by the official <c>confirmInStorePayment</c> operation.
/// </summary>
public class InStorePaymentConfirmReceiptData
{
    /// <summary>
    /// Payment instruction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("payment_instruction_id")]
    public string? PaymentInstructionId { get; set; }

    /// <summary>
    /// Payment system (JSON string).
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment instruction date (JSON string).
    /// </summary>
    [JsonPropertyName("payment_instruction_date")]
    public string? PaymentInstructionDate { get; set; }

    /// <summary>
    /// Acquiring bank (JSON string).
    /// </summary>
    [JsonPropertyName("bank_acquirer")]
    public string? BankAcquirer { get; set; }

    /// <summary>
    /// Acquiring bank EDRPOU code (JSON string).
    /// </summary>
    [JsonPropertyName("bank_edrpou")]
    public string? BankEdrpou { get; set; }

    /// <summary>
    /// POS terminal identifier (JSON string).
    /// </summary>
    [JsonPropertyName("pos_terminal_id")]
    public string? PosTerminalId { get; set; }

    /// <summary>
    /// Merchant name (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_name")]
    public string? MerchantName { get; set; }

    /// <summary>
    /// Merchant EDRPOU or IPN code (JSON string).
    /// </summary>
    [JsonPropertyName("edrpou_ipn")]
    public string? EdrpouIpn { get; set; }

    /// <summary>
    /// Merchant IBAN (JSON string).
    /// </summary>
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }

    /// <summary>
    /// Sale point address (JSON string).
    /// </summary>
    [JsonPropertyName("address_sale_point")]
    public string? AddressSalePoint { get; set; }

    /// <summary>
    /// Retrieval reference number (JSON string).
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Financial company transaction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fc_id")]
    public string? FcId { get; set; }

    /// <summary>
    /// Financial company name (JSON string). Declared by the confirm receipt only.
    /// </summary>
    [JsonPropertyName("fc_name")]
    public string? FcName { get; set; }

    /// <summary>
    /// Receipt description (JSON string).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Receipt data returned by the official <c>refundInStorePayment</c> operation.
/// </summary>
/// <remarks>
/// Identical to the confirm receipt except that the official refund schema declares no
/// <c>fc_name</c>, so this type does not offer one.
/// </remarks>
public class InStorePaymentRefundReceiptData
{
    /// <summary>
    /// Payment instruction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("payment_instruction_id")]
    public string? PaymentInstructionId { get; set; }

    /// <summary>
    /// Payment system (JSON string).
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment instruction date (JSON string).
    /// </summary>
    [JsonPropertyName("payment_instruction_date")]
    public string? PaymentInstructionDate { get; set; }

    /// <summary>
    /// Acquiring bank (JSON string).
    /// </summary>
    [JsonPropertyName("bank_acquirer")]
    public string? BankAcquirer { get; set; }

    /// <summary>
    /// Acquiring bank EDRPOU code (JSON string).
    /// </summary>
    [JsonPropertyName("bank_edrpou")]
    public string? BankEdrpou { get; set; }

    /// <summary>
    /// POS terminal identifier (JSON string).
    /// </summary>
    [JsonPropertyName("pos_terminal_id")]
    public string? PosTerminalId { get; set; }

    /// <summary>
    /// Merchant name (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_name")]
    public string? MerchantName { get; set; }

    /// <summary>
    /// Merchant EDRPOU or IPN code (JSON string).
    /// </summary>
    [JsonPropertyName("edrpou_ipn")]
    public string? EdrpouIpn { get; set; }

    /// <summary>
    /// Merchant IBAN (JSON string).
    /// </summary>
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }

    /// <summary>
    /// Sale point address (JSON string).
    /// </summary>
    [JsonPropertyName("address_sale_point")]
    public string? AddressSalePoint { get; set; }

    /// <summary>
    /// Retrieval reference number (JSON string).
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Financial company transaction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fc_id")]
    public string? FcId { get; set; }

    /// <summary>
    /// Receipt description (JSON string).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Response of the official <c>createInStorePayment</c> operation.
/// </summary>
public class InStorePaymentCreateResponse
{
    /// <summary>
    /// Financial company transaction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fc_id")]
    public string? FcId { get; set; }

    /// <summary>
    /// Payment identifier in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Creation timestamp (JSON string, <c>date-time</c>).
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Transaction status (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status")]
    public string? TransactionStatus { get; set; }

    /// <summary>
    /// Transaction status code (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status_code")]
    public string? TransactionStatusCode { get; set; }

    /// <summary>
    /// Amount as declared text (JSON string).
    /// </summary>
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    /// <summary>
    /// Order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Receipt data of the created payment.
    /// </summary>
    [JsonPropertyName("receipt_data")]
    public InStorePaymentCreateReceiptData? ReceiptData { get; set; }
}

/// <summary>
/// Response of the official <c>confirmInStorePayment</c> operation.
/// </summary>
public class InStorePaymentConfirmResponse
{
    /// <summary>
    /// Financial company transaction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fc_id")]
    public string? FcId { get; set; }

    /// <summary>
    /// Finalisation timestamp (JSON string). The official schema declares no format, so the value is
    /// carried verbatim instead of being parsed.
    /// </summary>
    [JsonPropertyName("finalised_at")]
    public string? FinalisedAt { get; set; }

    /// <summary>
    /// Payment identifier in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Transaction status (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status")]
    public string? TransactionStatus { get; set; }

    /// <summary>
    /// Transaction status code (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status_code")]
    public string? TransactionStatusCode { get; set; }

    /// <summary>
    /// Amount as declared text (JSON string).
    /// </summary>
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    /// <summary>
    /// Order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Fiscal receipt (JSON string).
    /// </summary>
    [JsonPropertyName("fiscal_receipt")]
    public string? FiscalReceipt { get; set; }

    /// <summary>
    /// Receipt data of the confirmed payment.
    /// </summary>
    [JsonPropertyName("receipt_data")]
    public InStorePaymentConfirmReceiptData? ReceiptData { get; set; }
}

/// <summary>
/// Response of the official <c>refundInStorePayment</c> operation.
/// </summary>
public class InStorePaymentRefundResponse
{
    /// <summary>
    /// Financial company transaction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fc_id")]
    public string? FcId { get; set; }

    /// <summary>
    /// Finalisation timestamp (JSON string). The official schema declares no format, so the value is
    /// carried verbatim instead of being parsed.
    /// </summary>
    [JsonPropertyName("finalised_at")]
    public string? FinalisedAt { get; set; }

    /// <summary>
    /// Refund identifier in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Transaction status (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status")]
    public string? TransactionStatus { get; set; }

    /// <summary>
    /// Transaction status code (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status_code")]
    public string? TransactionStatusCode { get; set; }

    /// <summary>
    /// Amount as declared text (JSON string).
    /// </summary>
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    /// <summary>
    /// Order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Receipt data of the refund.
    /// </summary>
    [JsonPropertyName("receipt_data")]
    public InStorePaymentRefundReceiptData? ReceiptData { get; set; }
}

/// <summary>
/// Response of the official <c>getInStorePaymentInfo</c> operation.
/// </summary>
public class InStorePaymentInfoResponse
{
    /// <summary>
    /// Financial company transaction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("fc_id")]
    public string? FcId { get; set; }

    /// <summary>
    /// Creation timestamp (JSON string, <c>date-time</c>).
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Transaction status (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status")]
    public string? TransactionStatus { get; set; }

    /// <summary>
    /// Transaction status code (JSON string).
    /// </summary>
    [JsonPropertyName("transaction_status_code")]
    public string? TransactionStatusCode { get; set; }
}
