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
    [JsonStringEnumMemberName("payment")]
    Payment,
    
    /// <summary>
    /// Confirm operation
    /// </summary>
    [JsonStringEnumMemberName("confirm")]
    Confirm,
    
    /// <summary>
    /// Refund operation
    /// </summary>
    [JsonStringEnumMemberName("refund")]
    Refund,
    
    /// <summary>
    /// Cancel operation
    /// </summary>
    [JsonStringEnumMemberName("cancel")]
    Cancel,

    /// <summary>
    /// Card lookup operation
    /// </summary>
    [JsonStringEnumMemberName("lookup")]
    Lookup,

    /// <summary>
    /// Recurrent payment operation
    /// </summary>
    [JsonStringEnumMemberName("recurrent")]
    Recurrent
}

/// <summary>
/// Operation status for transactions
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// Operation is initialized
    /// </summary>
    [JsonStringEnumMemberName("init")]
    Init,
    
    /// <summary>
    /// Operation is pending
    /// </summary>
    [JsonStringEnumMemberName("pending")]
    Pending,
    
    /// <summary>
    /// Operation was successful
    /// </summary>
    [JsonStringEnumMemberName("success")]
    Success,
    
    /// <summary>
    /// Operation failed
    /// </summary>
    [JsonStringEnumMemberName("failure")]
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
    [JsonStringEnumMemberName("direct")]
    Direct,
    
    /// <summary>
    /// Hosted checkout page
    /// </summary>
    [JsonStringEnumMemberName("hosted")]
    Hosted,
    
    /// <summary>
    /// Express checkout with products
    /// </summary>
    [JsonStringEnumMemberName("express_checkout")]
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
    [JsonStringEnumMemberName("url")]
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
    [JsonStringEnumMemberName("white")]
    White,
    
    /// <summary>
    /// Dark color scheme
    /// </summary>
    [JsonStringEnumMemberName("dark")]
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
    [JsonStringEnumMemberName("UK")]
    UK,
    
    /// <summary>
    /// English locale
    /// </summary>
    [JsonStringEnumMemberName("EN")]
    EN,
    
    /// <summary>
    /// Polish locale
    /// </summary>
    [JsonStringEnumMemberName("PL")]
    PL
}

/// <summary>
/// Campaign a batch payment belongs to.
/// </summary>
public enum CampaignName
{
    /// <summary>R-card campaign.</summary>
    [JsonStringEnumMemberName("r_card")]
    RCard,

    /// <summary>Diia card campaign.</summary>
    [JsonStringEnumMemberName("diia_card")]
    DiiaCard
}

/// <summary>
/// Payment method types
/// </summary>
public enum PaymentMethodType
{
    /// <summary>
    /// Card token
    /// </summary>
    [JsonStringEnumMemberName("cc_token")]
    CCToken,
    
    /// <summary>
    /// Card number
    /// </summary>
    [JsonStringEnumMemberName("cc_number")]
    CCNumber,
    
    /// <summary>
    /// Wallet payment
    /// </summary>
    [JsonStringEnumMemberName("wallet")]
    Wallet,
    
    /// <summary>
    /// Google Pay
    /// </summary>
    [JsonStringEnumMemberName("google_pay")]
    GooglePay,
    
    /// <summary>
    /// Apple Pay
    /// </summary>
    [JsonStringEnumMemberName("apple_pay")]
    ApplePay,
    
    /// <summary>
    /// Credit/debit card
    /// </summary>
    [JsonStringEnumMemberName("card")]
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
    [JsonStringEnumMemberName("create")]
    Create,
    
    /// <summary>
    /// Confirm operation
    /// </summary>
    [JsonStringEnumMemberName("confirm")]
    Confirm,
    
    /// <summary>
    /// Refund operation
    /// </summary>
    [JsonStringEnumMemberName("refund")]
    Refund,
    
    /// <summary>
    /// Cancel operation
    /// </summary>
    [JsonStringEnumMemberName("cancel")]
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

    /// <summary>
    /// Provider field &lt;c&gt;currency&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;description&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;image&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;net_amount&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("net_amount")]
    public decimal? NetAmount { get; set; }
    /// <summary>
    /// Tax group is required for projects with enabled fiscalization.
    /// </summary>
    [JsonPropertyName("tax_group")]
    public int? TaxGroup { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;url&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;vat_amount&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("vat_amount")]
    public decimal? VatAmount { get; set; }
}

/// <summary>
/// Error type enumeration (JSON string as per CDN documentation)
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Invalid request error
    /// </summary>
    [JsonStringEnumMemberName("invalid_request_error")]
    InvalidRequestError,
    
    /// <summary>
    /// Payment method error
    /// </summary>
    [JsonStringEnumMemberName("payment_method_error")]
    PaymentMethodError,
    
    /// <summary>
    /// Payment settings error
    /// </summary>
    [JsonStringEnumMemberName("payment_settings_error")]
    PaymentSettingsError,
    
    /// <summary>
    /// Payment error
    /// </summary>
    [JsonStringEnumMemberName("payment_error")]
    PaymentError,
    
    /// <summary>
    /// API error
    /// </summary>
    [JsonStringEnumMemberName("api_error")]
    ApiError,
    
    /// <summary>
    /// Customer error
    /// </summary>
    [JsonStringEnumMemberName("customer_error")]
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
    [JsonStringEnumMemberName("authorization_failed")]
    AuthorizationFailed,
    
    /// <summary>
    /// Customer authentication not found
    /// </summary>
    [JsonStringEnumMemberName("customer_auth_not_found")]
    CustomerAuthNotFound,
    
    /// <summary>
    /// Request failed
    /// </summary>
    [JsonStringEnumMemberName("request_failed")]
    RequestFailed,
    
    /// <summary>
    /// Internal error
    /// </summary>
    [JsonStringEnumMemberName("internal_error")]
    InternalError,
    
    /// <summary>
    /// Access not allowed
    /// </summary>
    [JsonStringEnumMemberName("access_not_allowed")]
    AccessNotAllowed,
    
    /// <summary>
    /// Invalid request body
    /// </summary>
    [JsonStringEnumMemberName("invalid_request_body")]
    InvalidRequestBody,
    
    /// <summary>
    /// Payment settings not found
    /// </summary>
    [JsonStringEnumMemberName("payment_settings_not_found")]
    PaymentSettingsNotFound,
    
    /// <summary>
    /// Transaction already paid
    /// </summary>
    [JsonStringEnumMemberName("transaction_already_paid")]
    TransactionAlreadyPaid,
    
    /// <summary>
    /// Action not allowed
    /// </summary>
    [JsonStringEnumMemberName("action_not_allowed")]
    ActionNotAllowed,
    
    /// <summary>
    /// Action already done
    /// </summary>
    [JsonStringEnumMemberName("action_already_done")]
    ActionAlreadyDone,
    
    /// <summary>
    /// Transaction success primary not found
    /// </summary>
    [JsonStringEnumMemberName("transaction_success_primary_not_found")]
    TransactionSuccessPrimaryNotFound,
    
    /// <summary>
    /// Payment method not allowed
    /// </summary>
    [JsonStringEnumMemberName("payment_method_not_allowed")]
    PaymentMethodNotAllowed,
    
    /// <summary>
    /// Wallet not configured
    /// </summary>
    [JsonStringEnumMemberName("wallet_not_configured")]
    WalletNotConfigured,
    
    /// <summary>
    /// Payment method already confirmed
    /// </summary>
    [JsonStringEnumMemberName("payment_method_already_confirmed")]
    PaymentMethodAlreadyConfirmed,
    
    /// <summary>
    /// Payment method not found
    /// </summary>
    [JsonStringEnumMemberName("payment_method_not_found")]
    PaymentMethodNotFound,
    
    /// <summary>
    /// Invalid card token
    /// </summary>
    [JsonStringEnumMemberName("invalid_card_token")]
    InvalidCardToken,
    
    /// <summary>
    /// Customer auth token expired or invalid
    /// </summary>
    [JsonStringEnumMemberName("customer_auth_token_expired_or_invalid")]
    CustomerAuthTokenExpiredOrInvalid,
    
    /// <summary>
    /// Customer profile not found
    /// </summary>
    [JsonStringEnumMemberName("customer_profile_not_found")]
    CustomerProfileNotFound,
    
    /// <summary>
    /// Customer ID not passed
    /// </summary>
    [JsonStringEnumMemberName("customer_id_not_passed")]
    CustomerIdNotPassed,
    
    /// <summary>
    /// Transaction not found
    /// </summary>
    [JsonStringEnumMemberName("transaction_not_found")]
    TransactionNotFound,
    
    /// <summary>
    /// Waiting for verification
    /// </summary>
    [JsonStringEnumMemberName("waiting_for_verification")]
    WaitingForVerification,
    
    /// <summary>
    /// Transaction amount limit
    /// </summary>
    [JsonStringEnumMemberName("transaction_amount_limit")]
    TransactionAmountLimit,
    
    /// <summary>
    /// Invalid data
    /// </summary>
    [JsonStringEnumMemberName("invalid_data")]
    InvalidData,
    
    /// <summary>
    /// Transaction declined
    /// </summary>
    [JsonStringEnumMemberName("transaction_declined")]
    TransactionDeclined,
    
    /// <summary>
    /// Authorization error
    /// </summary>
    [JsonStringEnumMemberName("authorization_error")]
    AuthorizationError,
    
    /// <summary>
    /// Transaction rejected
    /// </summary>
    [JsonStringEnumMemberName("transaction_rejected")]
    TransactionRejected,
    
    /// <summary>
    /// Transaction successful
    /// </summary>
    [JsonStringEnumMemberName("transaction_successful")]
    TransactionSuccessful,
    
    /// <summary>
    /// Anti fraud check
    /// </summary>
    [JsonStringEnumMemberName("anti_fraud_check")]
    AntiFraudCheck,
    
    /// <summary>
    /// Card not supported
    /// </summary>
    [JsonStringEnumMemberName("card_not_supported")]
    CardNotSupported,
    
    /// <summary>
    /// Confirmation timeout
    /// </summary>
    [JsonStringEnumMemberName("confirmation_timeout")]
    ConfirmationTimeout,
    
    /// <summary>
    /// Invalid card data
    /// </summary>
    [JsonStringEnumMemberName("invalid_card_data")]
    InvalidCardData,
    
    /// <summary>
    /// Invalid currency
    /// </summary>
    [JsonStringEnumMemberName("invalid_currency")]
    InvalidCurrency,
    
    /// <summary>
    /// Pending
    /// </summary>
    [JsonStringEnumMemberName("pending")]
    Pending,
    
    /// <summary>
    /// Waiting for complete
    /// </summary>
    [JsonStringEnumMemberName("waiting_for_complete")]
    WaitingForComplete,
    
    /// <summary>
    /// Access error
    /// </summary>
    [JsonStringEnumMemberName("access_error")]
    AccessError,
    
    /// <summary>
    /// Card expired
    /// </summary>
    [JsonStringEnumMemberName("card_expired")]
    CardExpired,
    
    /// <summary>
    /// Receiver info error
    /// </summary>
    [JsonStringEnumMemberName("receiver_info_error")]
    ReceiverInfoError,
    
    /// <summary>
    /// Transaction limit exceeded
    /// </summary>
    [JsonStringEnumMemberName("transaction_limit_exceeded")]
    TransactionLimitExceeded,
    
    /// <summary>
    /// Transaction not supported
    /// </summary>
    [JsonStringEnumMemberName("transaction_not_supported")]
    TransactionNotSupported,
    
    /// <summary>
    /// 3DS not supported
    /// </summary>
    [JsonStringEnumMemberName("3ds_not_supported")]
    ThreeDsNotSupported,
    
    /// <summary>
    /// 3DS required
    /// </summary>
    [JsonStringEnumMemberName("3ds_required")]
    ThreeDsRequired,
    
    /// <summary>
    /// Failed to create transaction
    /// </summary>
    [JsonStringEnumMemberName("failed_to_create_transaction")]
    FailedToCreateTransaction,
    
    /// <summary>
    /// Failed to finish transaction
    /// </summary>
    [JsonStringEnumMemberName("failed_to_finish_transaction")]
    FailedToFinishTransaction,
    
    /// <summary>
    /// Insufficient funds
    /// </summary>
    [JsonStringEnumMemberName("insufficient_funds")]
    InsufficientFunds,
    
    /// <summary>
    /// Invalid phone number
    /// </summary>
    [JsonStringEnumMemberName("invalid_phone_number")]
    InvalidPhoneNumber,
    
    /// <summary>
    /// Card has constraints
    /// </summary>
    [JsonStringEnumMemberName("card_has_constraints")]
    CardHasConstraints,
    
    /// <summary>
    /// PIN tries exceeded
    /// </summary>
    [JsonStringEnumMemberName("pin_tries_exceeded")]
    PinTriesExceeded,
    
    /// <summary>
    /// Session expired
    /// </summary>
    [JsonStringEnumMemberName("session_expired")]
    SessionExpired,
    
    /// <summary>
    /// Timeout
    /// </summary>
    [JsonStringEnumMemberName("timeout")]
    Timeout,
    
    /// <summary>
    /// Transaction created
    /// </summary>
    [JsonStringEnumMemberName("transaction_created")]
    TransactionCreated,
    
    /// <summary>
    /// Waiting for redirect
    /// </summary>
    [JsonStringEnumMemberName("waiting_for_redirect")]
    WaitingForRedirect,
    
    /// <summary>
    /// Wrong amount
    /// </summary>
    [JsonStringEnumMemberName("wrong_amount")]
    WrongAmount,
    
    /// <summary>
    /// Test transaction
    /// </summary>
    [JsonStringEnumMemberName("test_transaction")]
    TestTransaction,
    
    /// <summary>
    /// Subscription successful
    /// </summary>
    [JsonStringEnumMemberName("subscription_successful")]
    SubscriptionSuccessful,
    
    /// <summary>
    /// Unsubscribed successfully
    /// </summary>
    [JsonStringEnumMemberName("unsubscribed_successfully")]
    UnsubscribedSuccessfully,
    
    /// <summary>
    /// Wrong PIN
    /// </summary>
    [JsonStringEnumMemberName("wrong_pin")]
    WrongPin,
    
    /// <summary>
    /// Wrong authorization code
    /// </summary>
    [JsonStringEnumMemberName("wrong_authorization_code")]
    WrongAuthorizationCode,

    /// <summary>
    /// Provider response code <c>wrong_cavv</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_cavv")]
    WrongCavv,

    /// <summary>
    /// Provider response code <c>wrong_cvv</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_cvv")]
    WrongCvv,

    /// <summary>
    /// Provider response code <c>wrong_account_number</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_account_number")]
    WrongAccountNumber,

    /// <summary>
    /// Provider response code <c>confirm_required</c>.
    /// </summary>
    [JsonStringEnumMemberName("confirm_required")]
    ConfirmRequired,

    /// <summary>
    /// Provider response code <c>cvv_is_required</c>.
    /// </summary>
    [JsonStringEnumMemberName("cvv_is_required")]
    CvvIsRequired,

    /// <summary>
    /// Provider response code <c>confirmation_required</c>.
    /// </summary>
    [JsonStringEnumMemberName("confirmation_required")]
    ConfirmationRequired,

    /// <summary>
    /// Provider response code <c>sender_info_required</c>.
    /// </summary>
    [JsonStringEnumMemberName("sender_info_required")]
    SenderInfoRequired,

    /// <summary>
    /// Provider response code <c>missed_payout_method_data</c>.
    /// </summary>
    [JsonStringEnumMemberName("missed_payout_method_data")]
    MissedPayoutMethodData,

    /// <summary>
    /// Provider response code <c>card_verification_required</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_verification_required")]
    CardVerificationRequired,

    /// <summary>
    /// Provider response code <c>incorrect_refund_sum_or_currency</c>.
    /// </summary>
    [JsonStringEnumMemberName("incorrect_refund_sum_or_currency")]
    IncorrectRefundSumOrCurrency,

    /// <summary>
    /// Provider response code <c>payment_card_has_invalid_status</c>.
    /// </summary>
    [JsonStringEnumMemberName("payment_card_has_invalid_status")]
    PaymentCardHasInvalidStatus,

    /// <summary>
    /// Provider response code <c>wrong_card_number</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_card_number")]
    WrongCardNumber,

    /// <summary>
    /// Provider response code <c>user_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("user_not_found")]
    UserNotFound,

    /// <summary>
    /// Provider response code <c>failed_to_send_sms</c>.
    /// </summary>
    [JsonStringEnumMemberName("failed_to_send_sms")]
    FailedToSendSms,

    /// <summary>
    /// Provider response code <c>wrong_sms_password</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_sms_password")]
    WrongSmsPassword,

    /// <summary>
    /// Provider response code <c>card_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_not_found")]
    CardNotFound,

    /// <summary>
    /// Provider response code <c>payment_system_not_supported</c>.
    /// </summary>
    [JsonStringEnumMemberName("payment_system_not_supported")]
    PaymentSystemNotSupported,

    /// <summary>
    /// Provider response code <c>country_not_supported</c>.
    /// </summary>
    [JsonStringEnumMemberName("country_not_supported")]
    CountryNotSupported,

    /// <summary>
    /// Provider response code <c>no_discount_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("no_discount_found")]
    NoDiscountFound,

    /// <summary>
    /// Provider response code <c>failed_to_load_wallet</c>.
    /// </summary>
    [JsonStringEnumMemberName("failed_to_load_wallet")]
    FailedToLoadWallet,

    /// <summary>
    /// Provider response code <c>invalid_verification_code</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_verification_code")]
    InvalidVerificationCode,

    /// <summary>
    /// Provider response code <c>additional_information_is_pending</c>.
    /// </summary>
    [JsonStringEnumMemberName("additional_information_is_pending")]
    AdditionalInformationIsPending,

    /// <summary>
    /// Provider response code <c>transaction_is_not_recurring</c>.
    /// </summary>
    [JsonStringEnumMemberName("transaction_is_not_recurring")]
    TransactionIsNotRecurring,

    /// <summary>
    /// Provider response code <c>confirm_amount_cannot_be_more_than_the_transaction_amount</c>.
    /// </summary>
    [JsonStringEnumMemberName("confirm_amount_cannot_be_more_than_the_transaction_amount")]
    ConfirmAmountCannotBeMoreThanTheTransactionAmount,

    /// <summary>
    /// Provider response code <c>card_bin_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_bin_not_found")]
    CardBinNotFound,

    /// <summary>
    /// Provider response code <c>currency_rate_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("currency_rate_not_found")]
    CurrencyRateNotFound,

    /// <summary>
    /// Provider response code <c>invalid_recipient_name</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_recipient_name")]
    InvalidRecipientName,

    /// <summary>
    /// Provider response code <c>daily_card_usage_limit_reached</c>.
    /// </summary>
    [JsonStringEnumMemberName("daily_card_usage_limit_reached")]
    DailyCardUsageLimitReached,

    /// <summary>
    /// Provider response code <c>invalid_transaction_amount</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_transaction_amount")]
    InvalidTransactionAmount,

    /// <summary>
    /// Provider response code <c>card_type_is_not_supported</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_type_is_not_supported")]
    CardTypeIsNotSupported,

    /// <summary>
    /// Provider response code <c>store_is_blocked</c>.
    /// </summary>
    [JsonStringEnumMemberName("store_is_blocked")]
    StoreIsBlocked,

    /// <summary>
    /// Provider response code <c>store_is_not_active</c>.
    /// </summary>
    [JsonStringEnumMemberName("store_is_not_active")]
    StoreIsNotActive,

    /// <summary>
    /// Provider response code <c>transaction_cannot_be_processed</c>.
    /// </summary>
    [JsonStringEnumMemberName("transaction_cannot_be_processed")]
    TransactionCannotBeProcessed,

    /// <summary>
    /// Provider response code <c>invalid_transaction_status</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_transaction_status")]
    InvalidTransactionStatus,

    /// <summary>
    /// Provider response code <c>public_key_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("public_key_not_found")]
    PublicKeyNotFound,

    /// <summary>
    /// Provider response code <c>terminal_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("terminal_not_found")]
    TerminalNotFound,

    /// <summary>
    /// Provider response code <c>fee_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("fee_not_found")]
    FeeNotFound,

    /// <summary>
    /// Provider response code <c>failed_to_verify_card</c>.
    /// </summary>
    [JsonStringEnumMemberName("failed_to_verify_card")]
    FailedToVerifyCard,

    /// <summary>
    /// Provider response code <c>invalid_transaction_type</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_transaction_type")]
    InvalidTransactionType,

    /// <summary>
    /// Provider response code <c>restricted_ip</c>.
    /// </summary>
    [JsonStringEnumMemberName("restricted_ip")]
    RestrictedIp,

    /// <summary>
    /// Provider response code <c>invalid_token</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_token")]
    InvalidToken,

    /// <summary>
    /// Provider response code <c>preauth_not_allowed</c>.
    /// </summary>
    [JsonStringEnumMemberName("preauth_not_allowed")]
    PreauthNotAllowed,

    /// <summary>
    /// Provider response code <c>token_does_not_exist</c>.
    /// </summary>
    [JsonStringEnumMemberName("token_does_not_exist")]
    TokenDoesNotExist,

    /// <summary>
    /// Provider response code <c>reached_the_limit_of_attempts_for_ip</c>.
    /// </summary>
    [JsonStringEnumMemberName("reached_the_limit_of_attempts_for_ip")]
    ReachedTheLimitOfAttemptsForIp,

    /// <summary>
    /// Provider response code <c>card_branch_is_blocked</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_branch_is_blocked")]
    CardBranchIsBlocked,

    /// <summary>
    /// Provider response code <c>card_branch_daily_limit_reached</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_branch_daily_limit_reached")]
    CardBranchDailyLimitReached,

    /// <summary>
    /// Provider response code <c>completion_limit_reached</c>.
    /// </summary>
    [JsonStringEnumMemberName("completion_limit_reached")]
    CompletionLimitReached,

    /// <summary>
    /// Provider response code <c>recurring_transactions_not_allowed</c>.
    /// </summary>
    [JsonStringEnumMemberName("recurring_transactions_not_allowed")]
    RecurringTransactionsNotAllowed,

    /// <summary>
    /// Provider response code <c>transaction_is_canceled_by_payer</c>.
    /// </summary>
    [JsonStringEnumMemberName("transaction_is_canceled_by_payer")]
    TransactionIsCanceledByPayer,

    /// <summary>
    /// Provider response code <c>payment_was_refunded</c>.
    /// </summary>
    [JsonStringEnumMemberName("payment_was_refunded")]
    PaymentWasRefunded,

    /// <summary>
    /// Provider response code <c>card_is_lost_or_stolen</c>.
    /// </summary>
    [JsonStringEnumMemberName("card_is_lost_or_stolen")]
    CardIsLostOrStolen,

    /// <summary>
    /// Provider response code <c>plan_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("plan_not_found")]
    PlanNotFound,

    /// <summary>
    /// Provider response code <c>plan_not_active</c>.
    /// </summary>
    [JsonStringEnumMemberName("plan_not_active")]
    PlanNotActive,

    /// <summary>
    /// Provider response code <c>plan_project_missing</c>.
    /// </summary>
    [JsonStringEnumMemberName("plan_project_missing")]
    PlanProjectMissing,

    /// <summary>
    /// Provider response code <c>subscription_auto_renew_locked</c>.
    /// </summary>
    [JsonStringEnumMemberName("subscription_auto_renew_locked")]
    SubscriptionAutoRenewLocked,

    /// <summary>
    /// Provider response code <c>subscription_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("subscription_not_found")]
    SubscriptionNotFound,

    /// <summary>
    /// Provider response code <c>subscription_not_active</c>.
    /// </summary>
    [JsonStringEnumMemberName("subscription_not_active")]
    SubscriptionNotActive,

    /// <summary>
    /// Provider response code <c>subscription_already_exists</c>.
    /// </summary>
    [JsonStringEnumMemberName("subscription_already_exists")]
    SubscriptionAlreadyExists,

    /// <summary>
    /// Provider response code <c>customer_email_not_allowed</c>.
    /// </summary>
    [JsonStringEnumMemberName("customer_email_not_allowed")]
    CustomerEmailNotAllowed,

    /// <summary>
    /// Provider response code <c>order_canceled</c>.
    /// </summary>
    [JsonStringEnumMemberName("order_canceled")]
    OrderCanceled,

    /// <summary>
    /// Provider response code <c>3ds_verification_failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("3ds_verification_failed")]
    ThreeDsVerificationFailed,

    /// <summary>
    /// Provider response code <c>account_number_and_phone_passed</c>.
    /// </summary>
    [JsonStringEnumMemberName("account_number_and_phone_passed")]
    AccountNumberAndPhonePassed,

    /// <summary>
    /// Provider response code <c>acs_service_unavailable</c>.
    /// </summary>
    [JsonStringEnumMemberName("acs_service_unavailable")]
    AcsServiceUnavailable,

    /// <summary>
    /// Provider response code <c>agreements_service_unavailable</c>.
    /// </summary>
    [JsonStringEnumMemberName("agreements_service_unavailable")]
    AgreementsServiceUnavailable,

    /// <summary>
    /// Provider response code <c>amount_was_successfully_blocked</c>.
    /// </summary>
    [JsonStringEnumMemberName("amount_was_successfully_blocked")]
    AmountWasSuccessfullyBlocked,

    /// <summary>
    /// Provider response code <c>balances_limitation</c>.
    /// </summary>
    [JsonStringEnumMemberName("balances_limitation")]
    BalancesLimitation,

    /// <summary>
    /// Provider response code <c>bank_is_not_supported</c>.
    /// </summary>
    [JsonStringEnumMemberName("bank_is_not_supported")]
    BankIsNotSupported,

    /// <summary>
    /// Provider response code <c>banking_application_is_not_installed</c>.
    /// </summary>
    [JsonStringEnumMemberName("banking_application_is_not_installed")]
    BankingApplicationIsNotInstalled,

    /// <summary>
    /// Provider response code <c>callback_url_missing</c>.
    /// </summary>
    [JsonStringEnumMemberName("callback_url_missing")]
    CallbackUrlMissing,

    /// <summary>
    /// Provider response code <c>cancel_successful</c>.
    /// </summary>
    [JsonStringEnumMemberName("cancel_successful")]
    CancelSuccessful,

    /// <summary>
    /// Provider response code <c>cancel_transfer</c>.
    /// </summary>
    [JsonStringEnumMemberName("cancel_transfer")]
    CancelTransfer,

    /// <summary>
    /// Provider response code <c>chargeback-failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("chargeback-failed")]
    ChargebackFailed,

    /// <summary>
    /// Provider response code <c>chargeback_successful</c>.
    /// </summary>
    [JsonStringEnumMemberName("chargeback_successful")]
    ChargebackSuccessful,

    /// <summary>
    /// Provider response code <c>contract_was_signed_on_client_side</c>.
    /// </summary>
    [JsonStringEnumMemberName("contract_was_signed_on_client_side")]
    ContractWasSignedOnClientSide,

    /// <summary>
    /// Provider response code <c>credit_limit_exceeded</c>.
    /// </summary>
    [JsonStringEnumMemberName("credit_limit_exceeded")]
    CreditLimitExceeded,

    /// <summary>
    /// Provider response code <c>data_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("data_not_found")]
    DataNotFound,

    /// <summary>
    /// Provider response code <c>entity_agreement_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("entity_agreement_not_found")]
    EntityAgreementNotFound,

    /// <summary>
    /// Provider response code <c>entity_blocked</c>.
    /// </summary>
    [JsonStringEnumMemberName("entity_blocked")]
    EntityBlocked,

    /// <summary>
    /// Provider response code <c>entity_contract_terminated</c>.
    /// </summary>
    [JsonStringEnumMemberName("entity_contract_terminated")]
    EntityContractTerminated,

    /// <summary>
    /// Provider response code <c>entity_not_active</c>.
    /// </summary>
    [JsonStringEnumMemberName("entity_not_active")]
    EntityNotActive,

    /// <summary>
    /// Provider response code <c>entity_suspended</c>.
    /// </summary>
    [JsonStringEnumMemberName("entity_suspended")]
    EntitySuspended,

    /// <summary>
    /// Provider response code <c>expired_or_invalid_token</c>.
    /// </summary>
    [JsonStringEnumMemberName("expired_or_invalid_token")]
    ExpiredOrInvalidToken,

    /// <summary>
    /// Provider response code <c>failed_as_part_of_failed_batch</c>.
    /// </summary>
    [JsonStringEnumMemberName("failed_as_part_of_failed_batch")]
    FailedAsPartOfFailedBatch,

    /// <summary>
    /// Provider response code <c>failed_to_fetch_recipient_data_from_diia</c>.
    /// </summary>
    [JsonStringEnumMemberName("failed_to_fetch_recipient_data_from_diia")]
    FailedToFetchRecipientDataFromDiia,

    /// <summary>
    /// Provider response code <c>finmon_payer_validation_failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("finmon_payer_validation_failed")]
    FinmonPayerValidationFailed,

    /// <summary>
    /// Provider response code <c>finmon_recipient_validation_failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("finmon_recipient_validation_failed")]
    FinmonRecipientValidationFailed,

    /// <summary>
    /// Provider response code <c>finmon_validation_failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("finmon_validation_failed")]
    FinmonValidationFailed,

    /// <summary>
    /// Provider response code <c>free_bank_details_limit</c>.
    /// </summary>
    [JsonStringEnumMemberName("free_bank_details_limit")]
    FreeBankDetailsLimit,

    /// <summary>
    /// Provider response code <c>hold_cancelled_by_bank</c>.
    /// </summary>
    [JsonStringEnumMemberName("hold_cancelled_by_bank")]
    HoldCancelledByBank,

    /// <summary>
    /// Provider response code <c>insufficient_funds_for_refund</c>.
    /// </summary>
    [JsonStringEnumMemberName("insufficient_funds_for_refund")]
    InsufficientFundsForRefund,

    /// <summary>
    /// Provider response code <c>invalid_apple_pay_token</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_apple_pay_token")]
    InvalidApplePayToken,

    /// <summary>
    /// Provider response code <c>invalid_google_pay_token</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_google_pay_token")]
    InvalidGooglePayToken,

    /// <summary>
    /// Provider response code <c>invalid_operation</c>.
    /// </summary>
    [JsonStringEnumMemberName("invalid_operation")]
    InvalidOperation,

    /// <summary>
    /// Provider response code <c>missing_route</c>.
    /// </summary>
    [JsonStringEnumMemberName("missing_route")]
    MissingRoute,

    /// <summary>
    /// Provider response code <c>operation_not_allowed</c>.
    /// </summary>
    [JsonStringEnumMemberName("operation_not_allowed")]
    OperationNotAllowed,

    /// <summary>
    /// Provider response code <c>operation_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("operation_not_found")]
    OperationNotFound,

    /// <summary>
    /// Provider response code <c>order_expired</c>.
    /// </summary>
    [JsonStringEnumMemberName("order_expired")]
    OrderExpired,

    /// <summary>
    /// Provider response code <c>order_is_duplicated</c>.
    /// </summary>
    [JsonStringEnumMemberName("order_is_duplicated")]
    OrderIsDuplicated,

    /// <summary>
    /// Provider response code <c>partner_account_check_failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("partner_account_check_failed")]
    PartnerAccountCheckFailed,

    /// <summary>
    /// Provider response code <c>partner_account_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("partner_account_not_found")]
    PartnerAccountNotFound,

    /// <summary>
    /// Provider response code <c>payer_card_error</c>.
    /// </summary>
    [JsonStringEnumMemberName("payer_card_error")]
    PayerCardError,

    /// <summary>
    /// Provider response code <c>payment_flow_not_supported</c>.
    /// </summary>
    [JsonStringEnumMemberName("payment_flow_not_supported")]
    PaymentFlowNotSupported,

    /// <summary>
    /// Provider response code <c>payment_method_verification_failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("payment_method_verification_failed")]
    PaymentMethodVerificationFailed,

    /// <summary>
    /// Provider response code <c>payment_provider_failure</c>.
    /// </summary>
    [JsonStringEnumMemberName("payment_provider_failure")]
    PaymentProviderFailure,

    /// <summary>
    /// Provider response code <c>pin_data_required</c>.
    /// </summary>
    [JsonStringEnumMemberName("pin_data_required")]
    PinDataRequired,

    /// <summary>
    /// Provider response code <c>receiver_card_error</c>.
    /// </summary>
    [JsonStringEnumMemberName("receiver_card_error")]
    ReceiverCardError,

    /// <summary>
    /// Provider response code <c>recipient_payment_method_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("recipient_payment_method_not_found")]
    RecipientPaymentMethodNotFound,

    /// <summary>
    /// Provider response code <c>recurrent_not_found</c>.
    /// </summary>
    [JsonStringEnumMemberName("recurrent_not_found")]
    RecurrentNotFound,

    /// <summary>
    /// Provider response code <c>refund_is_cancelled_by_initiator</c>.
    /// </summary>
    [JsonStringEnumMemberName("refund_is_cancelled_by_initiator")]
    RefundIsCancelledByInitiator,

    /// <summary>
    /// Provider response code <c>refund_is_cancelled_by_system</c>.
    /// </summary>
    [JsonStringEnumMemberName("refund_is_cancelled_by_system")]
    RefundIsCancelledBySystem,

    /// <summary>
    /// Provider response code <c>refund_pending</c>.
    /// </summary>
    [JsonStringEnumMemberName("refund_pending")]
    RefundPending,

    /// <summary>
    /// Provider response code <c>refund_period_is_expired</c>.
    /// </summary>
    [JsonStringEnumMemberName("refund_period_is_expired")]
    RefundPeriodIsExpired,

    /// <summary>
    /// Provider response code <c>refund_successful</c>.
    /// </summary>
    [JsonStringEnumMemberName("refund_successful")]
    RefundSuccessful,

    /// <summary>
    /// Provider response code <c>refund_transaction_bank_daily_limit_exceeded</c>.
    /// </summary>
    [JsonStringEnumMemberName("refund_transaction_bank_daily_limit_exceeded")]
    RefundTransactionBankDailyLimitExceeded,

    /// <summary>
    /// Provider response code <c>rejected_by_bank</c>.
    /// </summary>
    [JsonStringEnumMemberName("rejected_by_bank")]
    RejectedByBank,

    /// <summary>
    /// Provider response code <c>representment-failed</c>.
    /// </summary>
    [JsonStringEnumMemberName("representment-failed")]
    RepresentmentFailed,

    /// <summary>
    /// Provider response code <c>representment_successful</c>.
    /// </summary>
    [JsonStringEnumMemberName("representment_successful")]
    RepresentmentSuccessful,

    /// <summary>
    /// Provider response code <c>simultaneous_open_orders_not_supported_by_bank</c>.
    /// </summary>
    [JsonStringEnumMemberName("simultaneous_open_orders_not_supported_by_bank")]
    SimultaneousOpenOrdersNotSupportedByBank,

    /// <summary>
    /// Provider response code <c>too_many_requests</c>.
    /// </summary>
    [JsonStringEnumMemberName("too_many_requests")]
    TooManyRequests,

    /// <summary>
    /// Provider response code <c>transaction_is_canceled</c>.
    /// </summary>
    [JsonStringEnumMemberName("transaction_is_canceled")]
    TransactionIsCanceled,

    /// <summary>
    /// Provider response code <c>unknown_entity</c>.
    /// </summary>
    [JsonStringEnumMemberName("unknown_entity")]
    UnknownEntity,

    /// <summary>
    /// Provider response code <c>user_collision_by_financial_number</c>.
    /// </summary>
    [JsonStringEnumMemberName("user_collision_by_financial_number")]
    UserCollisionByFinancialNumber,

    /// <summary>
    /// Provider response code <c>wrong_cooperation_type</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_cooperation_type")]
    WrongCooperationType,

    /// <summary>
    /// Provider response code <c>wrong_otp_code</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_otp_code")]
    WrongOtpCode,

    /// <summary>
    /// Provider response code <c>wrong_payment_count</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_payment_count")]
    WrongPaymentCount,

    /// <summary>
    /// Provider response code <c>wrong_payparts_period</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_payparts_period")]
    WrongPaypartsPeriod,

    /// <summary>
    /// Provider response code <c>wrong_project_settings</c>.
    /// </summary>
    [JsonStringEnumMemberName("wrong_project_settings")]
    WrongProjectSettings
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