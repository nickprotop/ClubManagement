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
    
    // Navigation properties
    public FacilityType FacilityType { get; set; } = null!;
}

public enum FacilityStatus
{
    Available,
    Occupied,
    Maintenance,
    OutOfOrder,
    Retired
}