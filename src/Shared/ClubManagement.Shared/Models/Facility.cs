namespace ClubManagement.Shared.Models;

public class Facility : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid FacilityTypeId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public FacilityStatus Status { get; set; } = FacilityStatus.Available;
    public int? Capacity { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool RequiresBooking { get; set; } = true;
    public int MaxBookingDaysInAdvance { get; set; } = 30;
    public int MinBookingDurationMinutes { get; set; } = 60;
    public int MaxBookingDurationMinutes { get; set; } = 180;
    public TimeSpan? OperatingHoursStart { get; set; }
    public TimeSpan? OperatingHoursEnd { get; set; }
    public List<DayOfWeek> OperatingDays { get; set; } = new();
    
    // Member Integration Properties
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    public List<string> RequiredCertifications { get; set; } = new();
    public int MemberConcurrentBookingLimit { get; set; } = 1;
    public bool RequiresMemberSupervision { get; set; } = false;
    public decimal MemberHourlyRate { get; set; }
    public decimal NonMemberHourlyRate { get; set; }
    public string? Location { get; set; }
    public string? Icon { get; set; }
    
    // Navigation properties
    public FacilityType FacilityType { get; set; } = null!;
    public List<FacilityBooking> Bookings { get; set; } = new();
}

public enum FacilityStatus
{
    Available,
    Occupied,
    Maintenance,
    OutOfOrder,
    Retired
}