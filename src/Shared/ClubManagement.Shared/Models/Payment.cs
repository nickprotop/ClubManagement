namespace ClubManagement.Shared.Models;

public class Payment : BaseEntity
{
    public Guid MemberId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string StripeChargeId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentType Type { get; set; } = PaymentType.OneTime;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;
    public string Description { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundReason { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Related entity IDs
    public Guid? EventRegistrationId { get; set; }
    public Guid? FacilityBookingId { get; set; }
    public Guid? SubscriptionId { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public EventRegistration? EventRegistration { get; set; }
    public FacilityBooking? FacilityBooking { get; set; }
    public Subscription? Subscription { get; set; }
}

public enum PaymentType
{
    OneTime,
    Subscription,
    Refund,
    Fee,
    Deposit
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public enum PaymentMethod
{
    Card,
    BankTransfer,
    Cash,
    Check,
    PayPal,
    ApplePay,
    GooglePay
}