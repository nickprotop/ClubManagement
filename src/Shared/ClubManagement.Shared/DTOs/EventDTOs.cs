using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class EventListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? FacilityName { get; set; }
    public string? InstructorName { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentEnrollment { get; set; }
    public decimal? Price { get; set; }
    public EventStatus Status { get; set; }
    
    // Recurrence info
    public bool IsRecurringMaster { get; set; }
    public Guid? MasterEventId { get; set; }
    public int? OccurrenceNumber { get; set; }
    public RecurrenceType? RecurrenceType { get; set; }
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public Guid? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid? InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentEnrollment { get; set; }
    public decimal? Price { get; set; }
    public EventStatus Status { get; set; }
    public RecurrencePattern? Recurrence { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public DateTime? CancellationDeadline { get; set; }
    public string? CancellationPolicy { get; set; }
    public bool AllowWaitlist { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<string> RequiredEquipment { get; set; } = new();
    public List<EventRegistrationDto> Registrations { get; set; } = new();
    
    // Recurrence info
    public bool IsRecurringMaster { get; set; }
    public Guid? MasterEventId { get; set; }
    public int? OccurrenceNumber { get; set; }
}

public class EventSearchRequest
{
    public string? SearchTerm { get; set; }
    public EventType? Type { get; set; }
    public EventStatus? Status { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? InstructorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? InstructorId { get; set; }
    public int MaxCapacity { get; set; }
    public decimal? Price { get; set; }
    public RecurrencePattern? Recurrence { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public DateTime? CancellationDeadline { get; set; }
    public string? CancellationPolicy { get; set; }
    public bool AllowWaitlist { get; set; } = true;
    public string? SpecialInstructions { get; set; }
    public List<string>? RequiredEquipment { get; set; }
    
    // Recurrence helper properties for UI
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int RecurrenceInterval { get; set; } = 1;
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public DateTime? RecurrenceEndDate { get; set; }
    public int? MaxOccurrences { get; set; }
}

public class UpdateEventRequest : CreateEventRequest
{
}

public class EventRegistrationDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string? RegisteredByName { get; set; }
    public string? Notes { get; set; }
    public bool IsWaitlisted { get; set; }
    public int? WaitlistPosition { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? CheckedInByName { get; set; }
    public bool NoShow { get; set; }
}

public class EventRegistrationRequest
{
    public Guid? MemberId { get; set; } // Null for self-registration
    public string? Notes { get; set; }
}

public class UpdateRegistrationRequest
{
    public RegistrationStatus Status { get; set; }
    public string? Notes { get; set; }
    public int? WaitlistPosition { get; set; }
    public bool NotifyMember { get; set; } = false;
}

public class BulkCheckInRequest
{
    public List<Guid> MemberIds { get; set; } = new();
}

public class BulkCheckInResult
{
    public int TotalRequested { get; set; }
    public int SuccessfulCheckIns { get; set; }
    public int AlreadyCheckedIn { get; set; }
    public int NotFound { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class CheckInStatusDto
{
    public Guid EventId { get; set; }
    public int TotalRegistered { get; set; }
    public int CheckedIn { get; set; }
    public int NotCheckedIn { get; set; }
    public int NoShows { get; set; }
    public DateTime CheckInStartTime { get; set; }
    public DateTime CheckInEndTime { get; set; }
}

public class CancelEventRequest
{
    public string? Reason { get; set; }
    public bool NotifyRegistrants { get; set; } = true;
}

public class RescheduleEventRequest
{
    public DateTime NewStartDateTime { get; set; }
    public DateTime NewEndDateTime { get; set; }
    public string? Reason { get; set; }
    public bool NotifyRegistrants { get; set; } = true;
}

// Recurrence Update DTOs
public class UpdateRecurrenceRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? InstructorId { get; set; }
    public int MaxCapacity { get; set; }
    public decimal? Price { get; set; }
    public bool AllowWaitlist { get; set; } = true;
    public string? SpecialInstructions { get; set; }
    public List<string>? RequiredEquipment { get; set; }
    public RecurrencePattern RecurrencePattern { get; set; } = new();
    public RecurrenceUpdateStrategy UpdateStrategy { get; set; } = RecurrenceUpdateStrategy.PreserveRegistrations;
}

public class RecurrenceUpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int OccurrencesDeleted { get; set; }
    public int OccurrencesCreated { get; set; }
    public int OccurrencesPreserved { get; set; }
    public int RegistrationsAffected { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<ConflictingEventDto> ConflictingEvents { get; set; } = new();
}

public class ConflictingEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public int RegistrationCount { get; set; }
    public List<string> MemberNames { get; set; } = new();
}

public enum RecurrenceUpdateStrategy
{
    PreserveRegistrations, // Keep occurrences with registrations, recreate others
    ForceUpdate,          // Update all future occurrences regardless of registrations
    CancelConflicts       // Cancel conflicting occurrences with registrations
}

public class BulkEventRegistrationRequest
{
    public List<Guid> EventIds { get; set; } = new();
    public List<Guid> MemberIds { get; set; } = new();
    public RecurringRegistrationOption RegistrationOption { get; set; }
    public int? NextOccurrences { get; set; }
    public string? Notes { get; set; }
}

public enum RecurringRegistrationOption
{
    ThisOccurrenceOnly,
    AllFutureOccurrences,
    SelectSpecific,
    NextN
}

public class RecurringRegistrationResponse
{
    public int SuccessfulRegistrations { get; set; }
    public int FailedRegistrations { get; set; }
    public List<EventRegistrationResult> Results { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class EventRegistrationResult
{
    public Guid EventId { get; set; }
    public Guid MemberId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public RegistrationStatus Status { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateTime EventDateTime { get; set; }
}

public class RecurringEventOptionsDto
{
    public Guid MasterEventId { get; set; }
    public string SeriesTitle { get; set; } = string.Empty;
    public List<EventOccurrenceDto> UpcomingOccurrences { get; set; } = new();
    public int TotalOccurrences { get; set; }
    public RecurrencePattern? RecurrencePattern { get; set; }
}

public class EventOccurrenceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int CurrentEnrollment { get; set; }
    public int? MaxCapacity { get; set; }
    public bool IsUserRegistered { get; set; }
    public bool IsFullyBooked { get; set; }
    public bool AllowWaitlist { get; set; }
}

public class RecurringRegistrationSummary
{
    public Guid MasterEventId { get; set; }
    public string EventSeriesName { get; set; } = string.Empty;
    public DateTime? NextOccurrence { get; set; }
    public int TotalRegistered { get; set; }
    public int TotalOccurrences { get; set; }
    public RecurringRegistrationOption RegistrationType { get; set; }
    public List<EventOccurrenceDto> RegisteredEvents { get; set; } = new();
}