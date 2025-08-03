namespace ClubManagement.Shared.Models;

public class Subscription : BaseEntity
{
    public Guid MemberId { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Basic;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public BillingInterval Interval { get; set; } = BillingInterval.Monthly;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public List<Payment> Payments { get; set; } = new();
}

public enum SubscriptionStatus
{
    Active,
    PastDue,
    Cancelled,
    Unpaid,
    Trialing,
    Incomplete,
    IncompleteExpired
}

public enum BillingInterval
{
    Monthly,
    Yearly,
    Weekly,
    Daily
}