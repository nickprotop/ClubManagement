using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IEventService
{
    // Event CRUD operations
    Task<ApiResponse<PagedResult<EventListDto>>?> GetEventsAsync(EventSearchRequest request);
    Task<ApiResponse<EventDto>?> GetEventAsync(Guid id);
    Task<ApiResponse<EventDto>?> CreateEventAsync(CreateEventRequest request);
    Task<ApiResponse<EventDto>?> UpdateEventAsync(Guid id, UpdateEventRequest request);
    Task<ApiResponse<bool>?> DeleteEventAsync(Guid id);
    Task<ApiResponse<bool>?> UpdateEventStatusAsync(Guid id, EventStatus status);
    
    // Permission checking
    Task<EventPermissions?> GetEventPermissionsAsync(Guid eventId);
    Task<EventPermissions?> GetGeneralEventPermissionsAsync();
    
    // Registration operations
    Task<ApiResponse<EventRegistrationDto>?> RegisterForEventAsync(Guid eventId, EventRegistrationRequest request);
    Task<ApiResponse<List<EventRegistrationDto>>?> GetEventRegistrationsAsync(Guid eventId);
    Task<ApiResponse<EventRegistrationDto>?> GetUserRegistrationStatusAsync(Guid eventId);
    Task<ApiResponse<EventRegistrationDto>?> UpdateRegistrationAsync(Guid eventId, Guid registrationId, UpdateRegistrationRequest request);
    Task<ApiResponse<bool>?> CancelRegistrationAsync(Guid eventId, Guid registrationId);
    
    // Bulk registration operations
    Task<ApiResponse<RecurringRegistrationResponse>?> BulkRegisterForEventsAsync(BulkEventRegistrationRequest request);
    Task<ApiResponse<RecurringEventOptionsDto>?> GetRecurringEventOptionsAsync(Guid masterEventId);
    Task<ApiResponse<List<RecurringRegistrationSummary>>?> GetUserRecurringRegistrationsAsync();
    
    // Check-in operations
    Task<ApiResponse<bool>?> CheckInMemberAsync(Guid eventId, Guid memberId);
    Task<ApiResponse<bool>?> CheckInSelfAsync(Guid eventId);
    Task<ApiResponse<bool>?> UndoCheckInAsync(Guid eventId, Guid memberId);
    Task<ApiResponse<BulkCheckInResult>?> BulkCheckInAsync(Guid eventId, BulkCheckInRequest request);
    Task<ApiResponse<CheckInStatusDto>?> GetCheckInStatusAsync(Guid eventId);
    
    // Event management
    Task<ApiResponse<bool>?> CancelEventAsync(Guid eventId, CancelEventRequest request);
    Task<ApiResponse<bool>?> RescheduleEventAsync(Guid eventId, RescheduleEventRequest request);
    
    // Recurrence management
    Task<ApiResponse<EventDto>?> GetMasterEventAsync(Guid eventId);
    Task<ApiResponse<RecurrenceUpdateResult>?> PreviewRecurrenceUpdateAsync(Guid eventId, RecurrencePattern newPattern);
    Task<ApiResponse<RecurrenceUpdateResult>?> UpdateRecurrenceSeriesAsync(Guid eventId, UpdateRecurrenceRequest request);
    
    // Facility Integration
    Task<ApiResponse<EventEligibilityResult>?> CheckMemberEligibilityAsync(Guid eventId, Guid memberId);
    Task<ApiResponse<EventFacilityRequirementsDto>?> GetEventFacilityRequirementsAsync(Guid eventId);
    Task<ApiResponse<FacilityAvailabilityResult>?> CheckFacilityAvailabilityAsync(Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeEventId = null);
    Task<ApiResponse<FacilityBookingDto>?> BookFacilityForEventAsync(Guid eventId, Guid facilityId, string? notes = null);
    Task<ApiResponse<object>?> CancelFacilityBookingForEventAsync(Guid eventId);
    Task<ApiResponse<List<EventListDto>>?> GetEventsByCertificationRequirementAsync(string certificationType);
    Task<ApiResponse<List<EventListDto>>?> GetEventsForMembershipTierAsync(MembershipTier tier);
    Task<ApiResponse<ValidationResult>?> ValidateEventFacilityRequirementsAsync(CreateEventRequest request);
    Task<ApiResponse<ValidationResult>?> ValidateEventFacilityRequirementsForUpdateAsync(Guid eventId, UpdateEventRequest request);
}