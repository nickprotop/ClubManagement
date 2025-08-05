using System.Text.Json;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public class EventService : IEventService
{
    private readonly IApiService _apiService;

    public EventService(IApiService apiService)
    {
        _apiService = apiService;
    }

    // Event CRUD operations
    public async Task<ApiResponse<PagedResult<EventListDto>>?> GetEventsAsync(EventSearchRequest request)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        
        if (request.Type.HasValue)
            queryParams.Add($"type={request.Type}");
        
        if (request.Status.HasValue)
            queryParams.Add($"status={request.Status}");
        
        if (request.FacilityId.HasValue)
            queryParams.Add($"facilityId={request.FacilityId}");
        
        if (request.InstructorId.HasValue)
            queryParams.Add($"instructorId={request.InstructorId}");
        
        if (request.StartDate.HasValue)
            queryParams.Add($"startDate={request.StartDate:yyyy-MM-dd}");
        
        if (request.EndDate.HasValue)
            queryParams.Add($"endDate={request.EndDate:yyyy-MM-dd}");
        
        queryParams.Add($"page={request.Page}");
        queryParams.Add($"pageSize={request.PageSize}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        
        return await _apiService.GetAsync<PagedResult<EventListDto>>($"api/events{queryString}");
    }

    public async Task<ApiResponse<EventDto>?> GetEventAsync(Guid id)
    {
        return await _apiService.GetAsync<EventDto>($"api/events/{id}");
    }

    public async Task<ApiResponse<EventDto>?> CreateEventAsync(CreateEventRequest request)
    {
        return await _apiService.PostAsync<EventDto>("api/events", request);
    }

    public async Task<ApiResponse<EventDto>?> UpdateEventAsync(Guid id, UpdateEventRequest request)
    {
        return await _apiService.PutAsync<EventDto>($"api/events/{id}", request);
    }

    public async Task<ApiResponse<bool>?> DeleteEventAsync(Guid id)
    {
        return await _apiService.DeleteAsync<bool>($"api/events/{id}");
    }

    public async Task<ApiResponse<bool>?> UpdateEventStatusAsync(Guid id, EventStatus status)
    {
        return await _apiService.PostAsync<bool>($"api/events/{id}/status", status);
    }

    // Permission checking
    public async Task<EventPermissions?> GetEventPermissionsAsync(Guid eventId)
    {
        try
        {
            // Use raw HttpClient approach via manual HTTP call
            var response = await _apiService.GetAsync<EventPermissions>($"api/events/{eventId}/permissions");
            return response?.Data;
        }
        catch
        {
            return null;
        }
    }

    public async Task<EventPermissions?> GetGeneralEventPermissionsAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<EventPermissions>("api/events/permissions");
            return response?.Data;
        }
        catch
        {
            return null;
        }
    }

    // Registration operations
    public async Task<ApiResponse<EventRegistrationDto>?> RegisterForEventAsync(Guid eventId, EventRegistrationRequest request)
    {
        return await _apiService.PostAsync<EventRegistrationDto>($"api/events/{eventId}/register", request);
    }

    public async Task<ApiResponse<List<EventRegistrationDto>>?> GetEventRegistrationsAsync(Guid eventId)
    {
        return await _apiService.GetAsync<List<EventRegistrationDto>>($"api/events/{eventId}/registrations");
    }

    public async Task<ApiResponse<EventRegistrationDto>?> GetUserRegistrationStatusAsync(Guid eventId)
    {
        return await _apiService.GetAsync<EventRegistrationDto>($"api/events/{eventId}/registrations/my-status");
    }

    public async Task<ApiResponse<EventRegistrationDto>?> UpdateRegistrationAsync(Guid eventId, Guid registrationId, UpdateRegistrationRequest request)
    {
        return await _apiService.PutAsync<EventRegistrationDto>($"api/events/{eventId}/registrations/{registrationId}", request);
    }

    public async Task<ApiResponse<bool>?> CancelRegistrationAsync(Guid eventId, Guid registrationId)
    {
        return await _apiService.DeleteAsync<bool>($"api/events/{eventId}/registrations/{registrationId}");
    }

    // Check-in operations
    public async Task<ApiResponse<bool>?> CheckInMemberAsync(Guid eventId, Guid memberId)
    {
        return await _apiService.PostAsync<bool>($"api/events/{eventId}/checkin/{memberId}", null);
    }

    public async Task<ApiResponse<bool>?> CheckInSelfAsync(Guid eventId)
    {
        return await _apiService.PostAsync<bool>($"api/events/{eventId}/checkin/self", null);
    }

    public async Task<ApiResponse<bool>?> UndoCheckInAsync(Guid eventId, Guid memberId)
    {
        return await _apiService.DeleteAsync<bool>($"api/events/{eventId}/checkin/{memberId}");
    }

    public async Task<ApiResponse<BulkCheckInResult>?> BulkCheckInAsync(Guid eventId, BulkCheckInRequest request)
    {
        return await _apiService.PostAsync<BulkCheckInResult>($"api/events/{eventId}/bulk-checkin", request);
    }

    public async Task<ApiResponse<CheckInStatusDto>?> GetCheckInStatusAsync(Guid eventId)
    {
        return await _apiService.GetAsync<CheckInStatusDto>($"api/events/{eventId}/checkin-status");
    }

    // Event management
    public async Task<ApiResponse<bool>?> CancelEventAsync(Guid eventId, CancelEventRequest request)
    {
        return await _apiService.PostAsync<bool>($"api/events/{eventId}/cancel", request);
    }

    public async Task<ApiResponse<bool>?> RescheduleEventAsync(Guid eventId, RescheduleEventRequest request)
    {
        return await _apiService.PostAsync<bool>($"api/events/{eventId}/reschedule", request);
    }

    // Recurrence management
    public async Task<ApiResponse<EventDto>?> GetMasterEventAsync(Guid eventId)
    {
        return await _apiService.GetAsync<EventDto>($"api/events/{eventId}/master");
    }

    public async Task<ApiResponse<RecurrenceUpdateResult>?> PreviewRecurrenceUpdateAsync(Guid eventId, RecurrencePattern newPattern)
    {
        return await _apiService.PostAsync<RecurrenceUpdateResult>($"api/events/{eventId}/recurrence/preview", newPattern);
    }

    public async Task<ApiResponse<RecurrenceUpdateResult>?> UpdateRecurrenceSeriesAsync(Guid eventId, UpdateRecurrenceRequest request)
    {
        return await _apiService.PutAsync<RecurrenceUpdateResult>($"api/events/{eventId}/recurrence", request);
    }

    // Bulk registration operations
    public async Task<ApiResponse<RecurringRegistrationResponse>?> BulkRegisterForEventsAsync(BulkEventRegistrationRequest request)
    {
        return await _apiService.PostAsync<RecurringRegistrationResponse>("api/events/bulk-register", request);
    }

    public async Task<ApiResponse<RecurringEventOptionsDto>?> GetRecurringEventOptionsAsync(Guid masterEventId)
    {
        return await _apiService.GetAsync<RecurringEventOptionsDto>($"api/events/{masterEventId}/recurring-options");
    }

    public async Task<ApiResponse<List<RecurringRegistrationSummary>>?> GetUserRecurringRegistrationsAsync()
    {
        return await _apiService.GetAsync<List<RecurringRegistrationSummary>>("api/events/user/recurring-registrations");
    }
}