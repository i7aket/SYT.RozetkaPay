using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.Common;

/// <summary>
/// Operation type for various API calls
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Payment operation
    /// </summary>
    [JsonPropertyName("payment")]
    Payment,
    
    /// <summary>
    /// Confirm operation
    /// </summary>
    [JsonPropertyName("confirm")]
    Confirm,
    
    /// <summary>
    /// Refund operation
    /// </summary>
    [JsonPropertyName("refund")]
    Refund,
    
    /// <summary>
    /// Cancel operation
    /// </summary>
    [JsonPropertyName("cancel")]
    Cancel
}

/// <summary>
/// Operation status for transactions
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// Operation is initialized
    /// </summary>
    [JsonPropertyName("init")]
    Init,
    
    /// <summary>
    /// Operation is pending
    /// </summary>
    [JsonPropertyName("pending")]
    Pending,
    
    /// <summary>
    /// Operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    Success,
    
    /// <summary>
    /// Operation failed
    /// </summary>
    [JsonPropertyName("failure")]
    Failure
}

/// <summary>
/// Payment mode types
/// </summary>
public enum PaymentMode
{
    /// <summary>
    /// Direct payment with customer details
    /// </summary>
    [JsonPropertyName("direct")]
    Direct,
    
    /// <summary>
    /// Hosted checkout page
    /// </summary>
    [JsonPropertyName("hosted")]
    Hosted,
    
    /// <summary>
    /// Express checkout with products
    /// </summary>
    [JsonPropertyName("express_checkout")]
    ExpressCheckout
}

/// <summary>
/// Action type for user actions
/// </summary>
public enum ActionType
{
    /// <summary>
    /// URL action
    /// </summary>
    [JsonPropertyName("url")]
    Url
}

/// <summary>
/// Checkout color mode options
/// </summary>
public enum CheckoutColorMode
{
    /// <summary>
    /// White color scheme
    /// </summary>
    [JsonPropertyName("white")]
    White,
    
    /// <summary>
    /// Dark color scheme
    /// </summary>
    [JsonPropertyName("dark")]
    Dark
}

/// <summary>
/// Customer checkout locale options
/// </summary>
public enum CustomerCheckoutLocale
{
    /// <summary>
    /// Ukrainian locale
    /// </summary>
    [JsonPropertyName("UK")]
    UK,
    
    /// <summary>
    /// English locale
    /// </summary>
    [JsonPropertyName("EN")]
    EN,
    
    /// <summary>
    /// Spanish locale
    /// </summary>
    [JsonPropertyName("ES")]
    ES,
    
    /// <summary>
    /// Polish locale
    /// </summary>
    [JsonPropertyName("PL")]
    PL,
    
    /// <summary>
    /// French locale
    /// </summary>
    [JsonPropertyName("FR")]
    FR,
    
    /// <summary>
    /// Slovak locale
    /// </summary>
    [JsonPropertyName("SK")]
    SK,
    
    /// <summary>
    /// German locale
    /// </summary>
    [JsonPropertyName("DE")]
    DE
}

/// <summary>
/// Payment method types
/// </summary>
public enum PaymentMethodType
{
    /// <summary>
    /// Card token
    /// </summary>
    [JsonPropertyName("cc_token")]
    CCToken,
    
    /// <summary>
    /// Card number
    /// </summary>
    [JsonPropertyName("cc_number")]
    CCNumber,
    
    /// <summary>
    /// Wallet payment
    /// </summary>
    [JsonPropertyName("wallet")]
    Wallet,
    
    /// <summary>
    /// Google Pay
    /// </summary>
    [JsonPropertyName("google_pay")]
    GooglePay,
    
    /// <summary>
    /// Apple Pay
    /// </summary>
    [JsonPropertyName("apple_pay")]
    ApplePay,
    
    /// <summary>
    /// Credit/debit card
    /// </summary>
    [JsonPropertyName("card")]
    Card
}

/// <summary>
/// Batch operation method types (JSON string as per CDN documentation)
/// </summary>
public enum BatchMethodType
{
    /// <summary>
    /// Create operation
    /// </summary>
    [JsonPropertyName("create")]
    Create,
    
    /// <summary>
    /// Confirm operation
    /// </summary>
    [JsonPropertyName("confirm")]
    Confirm,
    
    /// <summary>
    /// Refund operation
    /// </summary>
    [JsonPropertyName("refund")]
    Refund,
    
    /// <summary>
    /// Cancel operation
    /// </summary>
    [JsonPropertyName("cancel")]
    Cancel
}

/// <summary>
/// User action required for payment processing
/// </summary>
public class UserAction
{
    /// <summary>
    /// Type of the required action (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Action value/URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Browser fingerprint information for fraud detection
/// </summary>
public class BrowserFingerprint
{
    /// <summary>
    /// Browser accept header (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_accept_header")]
    [Required]
    public string BrowserAcceptHeader { get; set; } = string.Empty;

    /// <summary>
    /// Browser color depth (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_color_depth")]
    [Required]
    public string BrowserColorDepth { get; set; } = string.Empty;

    /// <summary>
    /// Browser IP address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_ip_address")]
    [Required]
    public string BrowserIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whether Java is enabled in browser (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_java_enabled")]
    [Required]
    public string BrowserJavaEnabled { get; set; } = string.Empty;

    /// <summary>
    /// Browser language (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_language")]
    [Required]
    public string BrowserLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Browser screen height (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_screen_height")]
    [Required]
    public string BrowserScreenHeight { get; set; } = string.Empty;

    /// <summary>
    /// Browser screen width (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_screen_width")]
    [Required]
    public string BrowserScreenWidth { get; set; } = string.Empty;

    /// <summary>
    /// Browser time zone (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_time_zone")]
    [Required]
    public string BrowserTimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Browser time zone offset (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_time_zone_offset")]
    [Required]
    public string BrowserTimeZoneOffset { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_user_agent")]
    [Required]
    public string BrowserUserAgent { get; set; } = string.Empty;
}

/// <summary>
/// User information base model
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Browser user agent (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_user_agent")]
    public string? BrowserUserAgent { get; set; }

    /// <summary>
    /// Email address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// External user ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// First name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// IP address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Last name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Patronym (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("patronym")]
    public string? Patronym { get; set; }

    /// <summary>
    /// Phone number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

/// <summary>
/// Fee details for transactions
/// </summary>
public class FeeDetails
{
    /// <summary>
    /// Fee amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Fee currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Fee rate (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }
}

/// <summary>
/// Fee item with various fee components
/// </summary>
public class FeeItem
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
/// Product information for orders
/// </summary>
public class Product
{
    /// <summary>
    /// Product name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Product price (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Product quantity (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    /// <summary>
    /// Product SKU (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    /// <summary>
    /// Product category (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

/// <summary>
/// Error type enumeration (JSON string as per CDN documentation)
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Invalid request error
    /// </summary>
    [JsonPropertyName("invalid_request_error")]
    InvalidRequestError,
    
    /// <summary>
    /// Payment method error
    /// </summary>
    [JsonPropertyName("payment_method_error")]
    PaymentMethodError,
    
    /// <summary>
    /// Payment settings error
    /// </summary>
    [JsonPropertyName("payment_settings_error")]
    PaymentSettingsError,
    
    /// <summary>
    /// Payment error
    /// </summary>
    [JsonPropertyName("payment_error")]
    PaymentError,
    
    /// <summary>
    /// API error
    /// </summary>
    [JsonPropertyName("api_error")]
    ApiError,
    
    /// <summary>
    /// Customer error
    /// </summary>
    [JsonPropertyName("customer_error")]
    CustomerError
}

/// <summary>
/// Response codes for operations (JSON string as per CDN documentation)
/// </summary>
public enum ResponseCode
{
    /// <summary>
    /// Authorization failed
    /// </summary>
    [JsonPropertyName("authorization_failed")]
    AuthorizationFailed,
    
    /// <summary>
    /// Customer authentication not found
    /// </summary>
    [JsonPropertyName("customer_auth_not_found")]
    CustomerAuthNotFound,
    
    /// <summary>
    /// Request failed
    /// </summary>
    [JsonPropertyName("request_failed")]
    RequestFailed,
    
    /// <summary>
    /// Internal error
    /// </summary>
    [JsonPropertyName("internal_error")]
    InternalError,
    
    /// <summary>
    /// Access not allowed
    /// </summary>
    [JsonPropertyName("access_not_allowed")]
    AccessNotAllowed,
    
    /// <summary>
    /// Invalid request body
    /// </summary>
    [JsonPropertyName("invalid_request_body")]
    InvalidRequestBody,
    
    /// <summary>
    /// Payment settings not found
    /// </summary>
    [JsonPropertyName("payment_settings_not_found")]
    PaymentSettingsNotFound,
    
    /// <summary>
    /// Transaction already paid
    /// </summary>
    [JsonPropertyName("transaction_already_paid")]
    TransactionAlreadyPaid,
    
    /// <summary>
    /// Action not allowed
    /// </summary>
    [JsonPropertyName("action_not_allowed")]
    ActionNotAllowed,
    
    /// <summary>
    /// Action already done
    /// </summary>
    [JsonPropertyName("action_already_done")]
    ActionAlreadyDone,
    
    /// <summary>
    /// Transaction success primary not found
    /// </summary>
    [JsonPropertyName("transaction_success_primary_not_found")]
    TransactionSuccessPrimaryNotFound,
    
    /// <summary>
    /// Payment method not allowed
    /// </summary>
    [JsonPropertyName("payment_method_not_allowed")]
    PaymentMethodNotAllowed,
    
    /// <summary>
    /// Wallet not configured
    /// </summary>
    [JsonPropertyName("wallet_not_configured")]
    WalletNotConfigured,
    
    /// <summary>
    /// Payment method already confirmed
    /// </summary>
    [JsonPropertyName("payment_method_already_confirmed")]
    PaymentMethodAlreadyConfirmed,
    
    /// <summary>
    /// Payment method not found
    /// </summary>
    [JsonPropertyName("payment_method_not_found")]
    PaymentMethodNotFound,
    
    /// <summary>
    /// Invalid card token
    /// </summary>
    [JsonPropertyName("invalid_card_token")]
    InvalidCardToken,
    
    /// <summary>
    /// Customer auth token expired or invalid
    /// </summary>
    [JsonPropertyName("customer_auth_token_expired_or_invalid")]
    CustomerAuthTokenExpiredOrInvalid,
    
    /// <summary>
    /// Customer profile not found
    /// </summary>
    [JsonPropertyName("customer_profile_not_found")]
    CustomerProfileNotFound,
    
    /// <summary>
    /// Customer ID not passed
    /// </summary>
    [JsonPropertyName("customer_id_not_passed")]
    CustomerIdNotPassed,
    
    /// <summary>
    /// Transaction not found
    /// </summary>
    [JsonPropertyName("transaction_not_found")]
    TransactionNotFound,
    
    /// <summary>
    /// Waiting for verification
    /// </summary>
    [JsonPropertyName("waiting_for_verification")]
    WaitingForVerification,
    
    /// <summary>
    /// Transaction amount limit
    /// </summary>
    [JsonPropertyName("transaction_amount_limit")]
    TransactionAmountLimit,
    
    /// <summary>
    /// Invalid data
    /// </summary>
    [JsonPropertyName("invalid_data")]
    InvalidData,
    
    /// <summary>
    /// Transaction declined
    /// </summary>
    [JsonPropertyName("transaction_declined")]
    TransactionDeclined,
    
    /// <summary>
    /// Authorization error
    /// </summary>
    [JsonPropertyName("authorization_error")]
    AuthorizationError,
    
    /// <summary>
    /// Transaction rejected
    /// </summary>
    [JsonPropertyName("transaction_rejected")]
    TransactionRejected,
    
    /// <summary>
    /// Transaction successful
    /// </summary>
    [JsonPropertyName("transaction_successful")]
    TransactionSuccessful,
    
    /// <summary>
    /// Anti fraud check
    /// </summary>
    [JsonPropertyName("anti_fraud_check")]
    AntiFraudCheck,
    
    /// <summary>
    /// Card not supported
    /// </summary>
    [JsonPropertyName("card_not_supported")]
    CardNotSupported,
    
    /// <summary>
    /// Confirmation timeout
    /// </summary>
    [JsonPropertyName("confirmation_timeout")]
    ConfirmationTimeout,
    
    /// <summary>
    /// Invalid card data
    /// </summary>
    [JsonPropertyName("invalid_card_data")]
    InvalidCardData,
    
    /// <summary>
    /// Invalid currency
    /// </summary>
    [JsonPropertyName("invalid_currency")]
    InvalidCurrency,
    
    /// <summary>
    /// Pending
    /// </summary>
    [JsonPropertyName("pending")]
    Pending,
    
    /// <summary>
    /// Waiting for complete
    /// </summary>
    [JsonPropertyName("waiting_for_complete")]
    WaitingForComplete,
    
    /// <summary>
    /// Access error
    /// </summary>
    [JsonPropertyName("access_error")]
    AccessError,
    
    /// <summary>
    /// Card expired
    /// </summary>
    [JsonPropertyName("card_expired")]
    CardExpired,
    
    /// <summary>
    /// Receiver info error
    /// </summary>
    [JsonPropertyName("receiver_info_error")]
    ReceiverInfoError,
    
    /// <summary>
    /// Transaction limit exceeded
    /// </summary>
    [JsonPropertyName("transaction_limit_exceeded")]
    TransactionLimitExceeded,
    
    /// <summary>
    /// Transaction not supported
    /// </summary>
    [JsonPropertyName("transaction_not_supported")]
    TransactionNotSupported,
    
    /// <summary>
    /// 3DS not supported
    /// </summary>
    [JsonPropertyName("3ds_not_supported")]
    ThreeDsNotSupported,
    
    /// <summary>
    /// 3DS required
    /// </summary>
    [JsonPropertyName("3ds_required")]
    ThreeDsRequired,
    
    /// <summary>
    /// Failed to create transaction
    /// </summary>
    [JsonPropertyName("failed_to_create_transaction")]
    FailedToCreateTransaction,
    
    /// <summary>
    /// Failed to finish transaction
    /// </summary>
    [JsonPropertyName("failed_to_finish_transaction")]
    FailedToFinishTransaction,
    
    /// <summary>
    /// Insufficient funds
    /// </summary>
    [JsonPropertyName("insufficient_funds")]
    InsufficientFunds,
    
    /// <summary>
    /// Invalid phone number
    /// </summary>
    [JsonPropertyName("invalid_phone_number")]
    InvalidPhoneNumber,
    
    /// <summary>
    /// Card has constraints
    /// </summary>
    [JsonPropertyName("card_has_constraints")]
    CardHasConstraints,
    
    /// <summary>
    /// PIN tries exceeded
    /// </summary>
    [JsonPropertyName("pin_tries_exceeded")]
    PinTriesExceeded,
    
    /// <summary>
    /// Session expired
    /// </summary>
    [JsonPropertyName("session_expired")]
    SessionExpired,
    
    /// <summary>
    /// Timeout
    /// </summary>
    [JsonPropertyName("timeout")]
    Timeout,
    
    /// <summary>
    /// Transaction created
    /// </summary>
    [JsonPropertyName("transaction_created")]
    TransactionCreated,
    
    /// <summary>
    /// Waiting for redirect
    /// </summary>
    [JsonPropertyName("waiting_for_redirect")]
    WaitingForRedirect,
    
    /// <summary>
    /// Wrong amount
    /// </summary>
    [JsonPropertyName("wrong_amount")]
    WrongAmount,
    
    /// <summary>
    /// Test transaction
    /// </summary>
    [JsonPropertyName("test_transaction")]
    TestTransaction,
    
    /// <summary>
    /// Subscription successful
    /// </summary>
    [JsonPropertyName("subscription_successful")]
    SubscriptionSuccessful,
    
    /// <summary>
    /// Unsubscribed successfully
    /// </summary>
    [JsonPropertyName("unsubscribed_successfully")]
    UnsubscribedSuccessfully,
    
    /// <summary>
    /// Wrong PIN
    /// </summary>
    [JsonPropertyName("wrong_pin")]
    WrongPin,
    
    /// <summary>
    /// Wrong authorization code
    /// </summary>
    [JsonPropertyName("wrong_authorization_code")]
    WrongAuthorizationCode
}

/// <summary>
/// Error response structure (JSON object as per CDN documentation)
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error response code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("code")]
    public ResponseCode? Code { get; set; }

    /// <summary>
    /// Error message (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Error parameter name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("param")]
    public string? Param { get; set; }

    /// <summary>
    /// Payment ID related to error (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Error type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public ErrorType? Type { get; set; }

    /// <summary>
    /// Unique error identifier (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("error_id")]
    public string? ErrorId { get; set; }
}

/// <summary>
/// Too many requests response structure (JSON object as per CDN documentation)
/// </summary>
public class TooManyRequestsResponse
{
    /// <summary>
    /// Error code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; } = "too_many_requests";

    /// <summary>
    /// Error message (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; } = "Too many requests";

    /// <summary>
    /// Error parameter name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("param")]
    public string? Param { get; set; }

    /// <summary>
    /// Payment ID related to error (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Error type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; } = "api_error";

    /// <summary>
    /// Unique error identifier (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("error_id")]
    public string? ErrorId { get; set; }
}

/// <summary>
/// Default response structure (JSON object as per CDN documentation)
/// </summary>
public class DefaultResponse
{
    /// <summary>
    /// Response message (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Base request user details (JSON object as per CDN documentation)
/// </summary>
public class BaseRequestUserDetails
{
    /// <summary>
    /// User address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("address")]
    [MaxLength(50)]
    public string? Address { get; set; }

    /// <summary>
    /// User city (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// User country (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// User email address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// External user ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// User first name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User last name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// User patronym (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("patronym")]
    public string? Patronym { get; set; }

    /// <summary>
    /// User phone number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// User postal code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }
}

// ===================== MISSING OPENAPI SCHEMAS =====================

/// <summary>
/// Customer information (OpenAPI schema)
/// </summary>
public class Customer
{
    /// <summary>
    /// Customer first name
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Customer last name
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Customer email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer phone number
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer IP address
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser user agent
    /// </summary>
    [JsonPropertyName("browser_user_agent")]
    public string? BrowserUserAgent { get; set; }
}

/// <summary>
/// Partners fee details (OpenAPI schema)
/// </summary>
public class PartnersFeeDetails
{
    /// <summary>
    /// Inner fee details
    /// </summary>
    [JsonPropertyName("inner_fee")]
    public FeeItem? InnerFee { get; set; }

    /// <summary>
    /// Outer fee details
    /// </summary>
    [JsonPropertyName("outer_fee")]
    public FeeItem? OuterFee { get; set; }
}

/// <summary>
/// Partners transaction details (OpenAPI schema)
/// </summary>
public class PartnersTransactionDetails
{
    /// <summary>
    /// Card mask
    /// </summary>
    [JsonPropertyName("card_mask")]
    public string? CardMask { get; set; }

    /// <summary>
    /// Merchant entity ID
    /// </summary>
    [JsonPropertyName("merchant_entity_id")]
    public string? MerchantEntityId { get; set; }

    /// <summary>
    /// Merchant fee amount
    /// </summary>
    [JsonPropertyName("merchant_fee_amount")]
    public string? MerchantFeeAmount { get; set; }

    /// <summary>
    /// Merchant order ID
    /// </summary>
    [JsonPropertyName("merchant_order_id")]
    public string? MerchantOrderId { get; set; }

    /// <summary>
    /// Unified external ID
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    [JsonPropertyName("order_description")]
    public string? OrderDescription { get; set; }

    /// <summary>
    /// Order ID
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Payment way
    /// </summary>
    [JsonPropertyName("pay_way")]
    public string? PayWay { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [JsonPropertyName("payment_amount")]
    public string? PaymentAmount { get; set; }

    /// <summary>
    /// Payment original amount
    /// </summary>
    [JsonPropertyName("payment_original_amount")]
    public string? PaymentOriginalAmount { get; set; }

    /// <summary>
    /// Payment recipient amount
    /// </summary>
    [JsonPropertyName("payment_recipient_amount")]
    public string? PaymentRecipientAmount { get; set; }

    /// <summary>
    /// Processing date
    /// </summary>
    [JsonPropertyName("processed_at")]
    public string? ProcessedAt { get; set; }

    /// <summary>
    /// Recipient card mask
    /// </summary>
    [JsonPropertyName("recipient_card_mask")]
    public string? RecipientCardMask { get; set; }

    /// <summary>
    /// Transaction status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
} 