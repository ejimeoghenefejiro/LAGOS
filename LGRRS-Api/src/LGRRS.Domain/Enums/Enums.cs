namespace LGRRS.Domain.Enums;

public enum MerchantStatus
{
    Pending,
    Verified,
    UnderReview,
    Suspended,
    Rejected
}

public enum DeliveryChannel
{
    WhatsApp,
    Sms,
    InApp
}

public enum DeliveryStatus
{
    ReadyToSend,
    Sent,
    Delivered,
    Failed
}

public enum ReceiptStatus
{
    Valid,
    Voided,
    Refunded,
    UnderReview
}

public enum EligibilityStatus
{
    Eligible,
    Blocked,
    Cancelled,
    Pending
}

public enum RiskStatus
{
    Clear,
    Flagged,
    Review
}

public enum DrawPeriodType
{
    Weekly,
    Monthly
}

public enum DrawPeriodStatus
{
    Open,
    Closed,
    Drawn,
    Published
}

public enum ClaimStatus
{
    Submitted,
    UnderReview,
    Verified,
    Approved,
    Paid,
    Rejected,
    Cancelled,
    Expired
}

public enum FraudRuleCode
{
    DuplicateEntry,
    InvalidReceipt,
    VoidOrRefund,
    HighVelocity,
    RepeatedValueCluster,
    OutOfHoursSpike,
    MerchantSelfEntry,
    ClaimPhoneMismatch,
    DuplicateClaim
}

public enum FraudSeverity
{
    Low,
    Medium,
    High
}

public enum FraudFlagStatus
{
    Open,
    UnderReview,
    Resolved,
    Dismissed
}

public enum FraudEntityType
{
    Receipt,
    Merchant,
    RewardEntry,
    PrizeClaim
}

public enum AppRole
{
    Merchant,
    LgaAdmin,
    ClaimProcessor,
    Auditor
}
