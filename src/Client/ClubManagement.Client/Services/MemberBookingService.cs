using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Client.Services;

public class MemberBookingService : IMemberBookingService
{
    private readonly IApiService _apiService;

    public MemberBookingService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<PagedResult<FacilityBookingDto>>?> GetMemberBookingsAsync(Guid memberId, MemberBookingFilter filter)
    {
        var queryParams = new List<string>();
        
        if (filter.Status.HasValue)
            queryParams.Add($"status={filter.Status}");
            
        if (filter.FacilityId.HasValue)
            queryParams.Add($"facilityId={filter.FacilityId}");
            
        if (filter.FacilityTypeId.HasValue)
            queryParams.Add($"facilityTypeId={filter.FacilityTypeId}");
            
        if (filter.StartDate.HasValue)
            queryParams.Add($"startDate={filter.StartDate:yyyy-MM-dd}");
            
        if (filter.EndDate.HasValue)
            queryParams.Add($"endDate={filter.EndDate:yyyy-MM-dd}");
            
        if (filter.IncludeRecurring.HasValue)
            queryParams.Add($"includeRecurring={filter.IncludeRecurring}");
            
        if (!string.IsNullOrEmpty(filter.SearchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
            
        queryParams.Add($"page={filter.Page}");
        queryParams.Add($"pageSize={filter.PageSize}");
        queryParams.Add($"sortBy={filter.SortBy}");
        queryParams.Add($"sortDescending={filter.SortDescending}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<PagedResult<FacilityBookingDto>>($"api/member-booking/{memberId}/bookings{queryString}");
    }

    public async Task<ApiResponse<List<FacilityBookingDto>>?> GetMemberUpcomingBookingsAsync(Guid memberId, int daysAhead = 7)
    {
        return await _apiService.GetAsync<List<FacilityBookingDto>>($"api/member-booking/{memberId}/bookings/upcoming?daysAhead={daysAhead}");
    }

    public async Task<ApiResponse<MemberBookingHistoryDto>?> GetMemberBookingHistoryAsync(Guid memberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var queryParams = new List<string>();
        
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate:yyyy-MM-dd}");
            
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate:yyyy-MM-dd}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<MemberBookingHistoryDto>($"api/member-booking/{memberId}/bookings/history{queryString}");
    }

    public async Task<ApiResponse<FacilityBookingDto>?> CreateMemberBookingAsync(Guid memberId, CreateMemberBookingRequest request)
    {
        return await _apiService.PostAsync<FacilityBookingDto>($"api/member-booking/{memberId}/bookings", request);
    }

    public async Task<ApiResponse<BookingCancellationResult>?> CancelMemberBookingAsync(Guid memberId, Guid bookingId, string? reason = null)
    {
        var endpoint = $"api/member-booking/{memberId}/bookings/{bookingId}";
        if (!string.IsNullOrEmpty(reason))
        {
            endpoint += $"?reason={Uri.EscapeDataString(reason)}";
        }
        return await _apiService.DeleteAsync<BookingCancellationResult>(endpoint);
    }

    public async Task<ApiResponse<FacilityBookingDto>?> ModifyMemberBookingAsync(Guid memberId, Guid bookingId, ModifyBookingRequest request)
    {
        return await _apiService.PutAsync<FacilityBookingDto>($"api/member-booking/{memberId}/bookings/{bookingId}", request);
    }

    public async Task<ApiResponse<List<RecommendedBookingSlot>>?> GetRecommendedBookingSlotsAsync(Guid memberId, Guid facilityId, DateTime? preferredDate = null)
    {
        var queryString = preferredDate.HasValue ? $"?preferredDate={preferredDate:yyyy-MM-dd}" : "";
        return await _apiService.GetAsync<List<RecommendedBookingSlot>>($"api/member-booking/{memberId}/recommendations/{facilityId}{queryString}");
    }

    public async Task<ApiResponse<MemberFacilityPreferencesDto>?> GetMemberPreferencesAsync(Guid memberId)
    {
        return await _apiService.GetAsync<MemberFacilityPreferencesDto>($"api/member-booking/{memberId}/preferences");
    }

    public async Task<ApiResponse<MemberFacilityPreferencesDto>?> UpdateMemberPreferencesAsync(Guid memberId, UpdateMemberPreferencesRequest request)
    {
        return await _apiService.PutAsync<MemberFacilityPreferencesDto>($"api/member-booking/{memberId}/preferences", request);
    }

    public async Task<ApiResponse<MemberAccessStatusDto>?> GetMemberAccessStatusAsync(Guid memberId)
    {
        return await _apiService.GetAsync<MemberAccessStatusDto>($"api/member-booking/{memberId}/access-status");
    }

    public async Task<ApiResponse<BookingAvailabilityResult>?> CheckBookingAvailabilityAsync(Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime)
    {
        var request = new { FacilityId = facilityId, StartTime = startTime, EndTime = endTime };
        return await _apiService.PostAsync<BookingAvailabilityResult>($"api/member-booking/{memberId}/check-availability", request);
    }

    public async Task<ApiResponse<List<FacilityDto>>?> GetMemberFavoriteFacilitiesAsync(Guid memberId)
    {
        return await _apiService.GetAsync<List<FacilityDto>>($"api/member-booking/{memberId}/favorites");
    }
}