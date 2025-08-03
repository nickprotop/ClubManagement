namespace ClubManagement.Shared.Models;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; } = EventType.Class;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? InstructorId { get; set; }
    public int MaxCapacity { get; set; } = 0;
    public int CurrentEnrollment { get; set; } = 0;
    public decimal? Price { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public RecurrencePattern? Recurrence { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public DateTime? CancellationDeadline { get; set; }
    public string? CancellationPolicy { get; set; }
    public bool AllowWaitlist { get; set; } = true;
    public string? SpecialInstructions { get; set; }
    public List<string> RequiredEquipment { get; set; } = new();
    
    // Navigation properties
    public Facility? Facility { get; set; }
    public User? Instructor { get; set; }
    public List<EventRegistration> Registrations { get; set; } = new();
}

public enum EventType
{
    Class,
    Workshop,
    Tournament,
    Event,
    Private,
    Maintenance
}

public enum EventStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    Rescheduled
}

public class RecurrencePattern
{
    public RecurrenceType Type { get; set; } = RecurrenceType.None;
    public int Interval { get; set; } = 1;
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public DateTime? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
}

public enum RecurrenceType
{
    None,
    Daily,
    Weekly,
    Monthly,
    Yearly
}