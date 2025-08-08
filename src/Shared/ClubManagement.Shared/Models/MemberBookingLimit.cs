namespace ClubManagement.Shared.Models;

public class MemberBookingLimit : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? FacilityTypeId { get; set; }
    public MembershipTier? ApplicableTier { get; set; }
    
    // Booking Limits
    public int MaxConcurrentBookings { get; set; } = 3;
    public int MaxBookingsPerDay { get; set; } = 2;
    public int MaxBookingsPerWeek { get; set; } = 5;
    public int MaxBookingsPerMonth { get; set; } = 20;
    
    // Duration Limits
    public int MaxBookingDurationHours { get; set; } = 4;
    public int MaxAdvanceBookingDays { get; set; } = 30;
    public int MinAdvanceBookingHours { get; set; } = 2;
    
    // Time Restrictions
    public TimeSpan? EarliestBookingTime { get; set; }
    public TimeSpan? LatestBookingTime { get; set; }
    public DayOfWeek[]? AllowedDays { get; set; }
    
    // Additional Restrictions
    public bool RequiresApproval { get; set; } = false;
    public bool AllowRecurringBookings { get; set; } = true;
    public int CancellationPenaltyHours { get; set; } = 24;
    
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public Facility? Facility { get; set; }
    public FacilityType? FacilityType { get; set; }
}

public enum BookingLimitType
{
    PerMember,
    PerFacility,
    PerFacilityType,
    Global
}

public class MemberBookingUsage
{
    public Guid MemberId { get; set; }
    public DateTime Date { get; set; }
    public int BookingsToday { get; set; }
    public int BookingsThisWeek { get; set; }
    public int BookingsThisMonth { get; set; }
    public int ConcurrentBookings { get; set; }
    public decimal TotalHoursBooked { get; set; } = 0;
    public DateTime LastCalculated { get; set; }
}