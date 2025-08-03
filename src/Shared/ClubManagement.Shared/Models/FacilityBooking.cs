namespace ClubManagement.Shared.Models;

public class FacilityBooking : BaseEntity
{
    public Guid FacilityId { get; set; }
    public Guid MemberId { get; set; }
    public Guid? BookedByUserId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public decimal? Cost { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Purpose { get; set; }
    public int? ParticipantCount { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInByUserId { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public Guid? CheckedOutByUserId { get; set; }
    public bool NoShow { get; set; } = false;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    
    // Navigation properties
    public Facility Facility { get; set; } = null!;
    public Member Member { get; set; } = null!;
    public User? BookedByUser { get; set; }
    public User? CheckedInByUser { get; set; }
    public User? CheckedOutByUser { get; set; }
    public User? CancelledByUser { get; set; }
}

public enum BookingStatus
{
    Confirmed,
    Pending,
    CheckedIn,
    CheckedOut,
    Cancelled,
    NoShow,
    Completed
}