namespace ClubManagement.Shared.Models;

public class EventRegistration : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid MemberId { get; set; }
    public Guid? RegisteredByUserId { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;
    public decimal? AmountPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IsWaitlisted { get; set; } = false;
    public int? WaitlistPosition { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInByUserId { get; set; }
    public bool NoShow { get; set; } = false;
    public string? Notes { get; set; }
    
    // Navigation properties
    public Event Event { get; set; } = null!;
    public Member Member { get; set; } = null!;
    public User? RegisteredByUser { get; set; }
    public User? CheckedInByUser { get; set; }
}

public enum RegistrationStatus
{
    Confirmed,
    Pending,
    Cancelled,
    Waitlisted,
    NoShow,
    Completed
}