using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services.Interfaces;

public interface IEventFacilityService
{
    /// <summary>
    /// Validates if a member can register for an event based on facility requirements
    /// </summary>
    Task<ClubManagement.Shared.DTOs.EventEligibilityResult> CheckMemberEligibilityAsync(Guid eventId, Guid memberId);
    
    /// <summary>
    /// Gets facility requirements for an event
    /// </summary>
    Task<ClubManagement.Shared.DTOs.EventFacilityRequirementsDto> GetEventFacilityRequirementsAsync(Guid eventId);
    
    /// <summary>
    /// Validates if a facility is available for an event
    /// </summary>
    Task<ClubManagement.Shared.DTOs.FacilityAvailabilityResult> CheckFacilityAvailabilityAsync(Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeEventId = null);
    
    /// <summary>
    /// Books facility for an event (creates facility booking)
    /// </summary>
    Task<FacilityBookingDto> BookFacilityForEventAsync(Guid eventId, Guid facilityId, string notes = "");
    
    /// <summary>
    /// Cancels facility booking for an event
    /// </summary>
    Task CancelFacilityBookingForEventAsync(Guid eventId);
    
    /// <summary>
    /// Gets all events that require a specific certification
    /// </summary>
    Task<List<EventListDto>> GetEventsByCertificationRequirementAsync(string certificationType);
    
    /// <summary>
    /// Gets all events available to a specific membership tier
    /// </summary>
    Task<List<EventListDto>> GetEventsForMembershipTierAsync(MembershipTier tier);
    
    /// <summary>
    /// Validates event creation/update for facility conflicts
    /// </summary>
    Task<ClubManagement.Shared.DTOs.ValidationResult> ValidateEventFacilityRequirementsAsync(CreateEventRequest request);
    Task<ClubManagement.Shared.DTOs.ValidationResult> ValidateEventFacilityRequirementsAsync(Guid eventId, UpdateEventRequest request);
}

public class EventEligibilityResult
{
    public bool IsEligible { get; set; }
    public List<string> RequirementViolations { get; set; } = new();
    public List<string> MissingCertifications { get; set; } = new();
    public bool HasFacilityAccess { get; set; }
    public bool MeetsTierRequirement { get; set; }
    public bool MeetsAgeRequirement { get; set; }
    public string[] Warnings { get; set; } = Array.Empty<string>();
}

public class EventFacilityRequirementsDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public Guid? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public List<string> RequiredCertifications { get; set; } = new();
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    public bool RequiresFacilityAccess { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public bool HasFacilityConflicts { get; set; }
    public List<string> ConflictDetails { get; set; } = new();
}

public class FacilityAvailabilityResult
{
    public bool IsAvailable { get; set; }
    public List<string> ConflictReasons { get; set; } = new();
    public List<FacilityBookingDto> ConflictingBookings { get; set; } = new();
    public List<EventListDto> ConflictingEvents { get; set; } = new();
    public DateTime? NextAvailableSlot { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}