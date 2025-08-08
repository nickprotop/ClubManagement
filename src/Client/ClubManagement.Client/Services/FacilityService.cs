using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using System.Text;
using System.Text.Json;

namespace ClubManagement.Client.Services;

public class FacilityService : IFacilityService
{
    private readonly IApiService _apiService;

    public FacilityService(IApiService apiService)
    {
        _apiService = apiService;
    }

    // Facilities
    public async Task<ApiResponse<List<FacilityDto>>> GetFacilitiesAsync()
    {
        var response = await _apiService.GetAsync<PagedResult<FacilityDto>>("api/facilities");
        if (response.Success && response.Data != null)
        {
            return ApiResponse<List<FacilityDto>>.SuccessResult(response.Data.Items);
        }
        return ApiResponse<List<FacilityDto>>.ErrorResult(response.Message ?? "Failed to retrieve facilities");
    }

    public async Task<ApiResponse<FacilityDto>> GetFacilityByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<FacilityDto>($"api/facilities/{id}");
    }

    public async Task<ApiResponse<FacilityDto>> CreateFacilityAsync(CreateFacilityRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityDto>("api/facilities", content);
    }

    public async Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(Guid id, UpdateFacilityRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PutAsync<FacilityDto>($"api/facilities/{id}", content);
    }

    public async Task<ApiResponse<object>> DeleteFacilityAsync(Guid id)
    {
        return await _apiService.DeleteAsync<object>($"api/facilities/{id}");
    }

    public async Task<ApiResponse<FacilityPermissions>> GetFacilityPermissionsAsync(Guid? id = null)
    {
        var url = id.HasValue ? $"api/facilities/{id}/permissions" : "api/facilities/permissions";
        return await _apiService.GetAsync<FacilityPermissions>(url);
    }

    public async Task<ApiResponse<List<FacilityDto>>> SearchFacilitiesAsync(string? searchTerm = null, Guid? facilityTypeId = null, FacilityStatus? status = null)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(searchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            
        if (facilityTypeId.HasValue)
            queryParams.Add($"facilityTypeId={facilityTypeId}");
            
        if (status.HasValue)
            queryParams.Add($"status={status}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<List<FacilityDto>>($"api/facilities/search{queryString}");
    }

    public async Task<ApiResponse<List<FacilityDto>>> GetFacilitiesByTypeAsync(Guid facilityTypeId)
    {
        return await _apiService.GetAsync<List<FacilityDto>>($"api/facilities/type/{facilityTypeId}");
    }

    public async Task<ApiResponse<List<FacilityUsageHistoryDto>>> GetFacilityUsageHistoryAsync(Guid facilityId)
    {
        return await _apiService.GetAsync<List<FacilityUsageHistoryDto>>($"api/facilities/{facilityId}/usage-history");
    }

    public async Task<ApiResponse<List<FacilityDto>>> GetAvailableFacilitiesForMemberAsync(Guid memberId, DateTime? startDateTime = null, DateTime? endDateTime = null)
    {
        var queryParams = new List<string>();
        
        if (startDateTime.HasValue)
            queryParams.Add($"startDateTime={startDateTime.Value:O}");
            
        if (endDateTime.HasValue)
            queryParams.Add($"endDateTime={endDateTime.Value:O}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<List<FacilityDto>>($"api/facilities/available-for-member/{memberId}{queryString}");
    }

    public async Task<ApiResponse<MemberFacilityAccessDto>> CheckMemberAccessAsync(Guid facilityId, Guid memberId)
    {
        return await _apiService.GetAsync<MemberFacilityAccessDto>($"api/facilities/{facilityId}/check-member-access/{memberId}");
    }

    // Facility Types
    public async Task<ApiResponse<List<FacilityTypeDto>>> GetFacilityTypesAsync()
    {
        return await _apiService.GetAsync<List<FacilityTypeDto>>("api/facility-types");
    }

    public async Task<ApiResponse<FacilityTypeDto>> GetFacilityTypeByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<FacilityTypeDto>($"api/facility-types/{id}");
    }

    public async Task<ApiResponse<FacilityTypeDto>> CreateFacilityTypeAsync(CreateFacilityTypeRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityTypeDto>("api/facility-types", content);
    }

    public async Task<ApiResponse<FacilityTypeDto>> UpdateFacilityTypeAsync(Guid id, UpdateFacilityTypeRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PutAsync<FacilityTypeDto>($"api/facility-types/{id}", content);
    }

    public async Task<ApiResponse<object>> DeleteFacilityTypeAsync(Guid id)
    {
        return await _apiService.DeleteAsync<object>($"api/facility-types/{id}");
    }

    public async Task<ApiResponse<object>> ActivateFacilityTypeAsync(Guid id)
    {
        return await _apiService.PostAsync<object>($"api/facility-types/{id}/activate", null);
    }

    public async Task<ApiResponse<object>> DeactivateFacilityTypeAsync(Guid id)
    {
        return await _apiService.PostAsync<object>($"api/facility-types/{id}/deactivate", null);
    }

    // Facility Bookings
    public async Task<ApiResponse<List<FacilityBookingDto>>> GetFacilityBookingsAsync(Guid? facilityId = null, Guid? memberId = null, DateTime? startDate = null, DateTime? endDate = null, BookingStatus? status = null)
    {
        var queryParams = new List<string>();
        
        if (facilityId.HasValue)
            queryParams.Add($"facilityId={facilityId}");
            
        if (memberId.HasValue)
            queryParams.Add($"memberId={memberId}");
            
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            
        if (status.HasValue)
            queryParams.Add($"status={status}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<List<FacilityBookingDto>>($"api/facility-bookings{queryString}");
    }

    public async Task<ApiResponse<FacilityBookingDto>> GetFacilityBookingByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<FacilityBookingDto>($"api/facility-bookings/{id}");
    }

    public async Task<ApiResponse<FacilityBookingDto>> CreateFacilityBookingAsync(CreateFacilityBookingRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityBookingDto>("api/facility-bookings", content);
    }

    public async Task<ApiResponse<FacilityBookingDto>> UpdateFacilityBookingAsync(Guid id, UpdateFacilityBookingRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PutAsync<FacilityBookingDto>($"api/facility-bookings/{id}", content);
    }

    public async Task<ApiResponse<object>> CancelFacilityBookingAsync(Guid id)
    {
        return await _apiService.PostAsync<object>($"api/facility-bookings/{id}/cancel", null);
    }

    public async Task<ApiResponse<FacilityBookingDto>> CheckInFacilityBookingAsync(Guid id, CheckInBookingRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityBookingDto>($"api/facility-bookings/{id}/checkin", content);
    }

    public async Task<ApiResponse<FacilityBookingDto>> CheckOutFacilityBookingAsync(Guid id, CheckOutBookingRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityBookingDto>($"api/facility-bookings/{id}/checkout", content);
    }

    public async Task<ApiResponse<List<FacilityAvailabilityDto>>> CheckFacilityAvailabilityAsync(Guid facilityId, DateTime startDateTime, DateTime endDateTime)
    {
        var queryParams = $"startDateTime={startDateTime:O}&endDateTime={endDateTime:O}";
        return await _apiService.GetAsync<List<FacilityAvailabilityDto>>($"api/facility-bookings/{facilityId}/availability?{queryParams}");
    }

    public async Task<ApiResponse<List<FacilityBookingConflictDto>>> CheckBookingConflictsAsync(Guid facilityId, DateTime startDateTime, DateTime endDateTime, Guid? excludeBookingId = null)
    {
        var queryParams = new List<string>
        {
            $"startDateTime={startDateTime:O}",
            $"endDateTime={endDateTime:O}"
        };
        
        if (excludeBookingId.HasValue)
            queryParams.Add($"excludeBookingId={excludeBookingId}");

        var queryString = string.Join("&", queryParams);
        return await _apiService.GetAsync<List<FacilityBookingConflictDto>>($"api/facility-bookings/{facilityId}/check-conflicts?{queryString}");
    }

    public async Task<ApiResponse<List<FacilityBookingDto>>> GetMemberBookingsAsync(Guid memberId, DateTime? startDate = null, DateTime? endDate = null, BookingStatus? status = null)
    {
        var queryParams = new List<string>();
        
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            
        if (status.HasValue)
            queryParams.Add($"status={status}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<List<FacilityBookingDto>>($"api/facility-bookings/member/{memberId}{queryString}");
    }

    public async Task<ApiResponse<FacilityBookingDto>> CreateMemberBookingAsync(CreateMemberFacilityBookingRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityBookingDto>("api/facility-bookings/member", content);
    }

    public async Task<ApiResponse<FacilityBookingStatsDto>> GetBookingStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var queryParams = new List<string>();
        
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<FacilityBookingStatsDto>($"api/facility-bookings/stats{queryString}");
    }

    public async Task<ApiResponse<List<FacilityUsageReportDto>>> GetFacilityUsageReportAsync(DateTime startDate, DateTime endDate)
    {
        var queryParams = $"startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        return await _apiService.GetAsync<List<FacilityUsageReportDto>>($"api/facility-bookings/usage-report?{queryParams}");
    }

    // Certification Management
    public async Task<ApiResponse<PagedResult<FacilityCertificationDto>>> GetCertificationsAsync(CertificationListFilter filter)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(filter.Search))
            queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");
            
        if (filter.MemberId.HasValue)
            queryParams.Add($"memberId={filter.MemberId}");
            
        if (!string.IsNullOrEmpty(filter.CertificationType))
            queryParams.Add($"certificationType={Uri.EscapeDataString(filter.CertificationType)}");
            
        if (filter.IsActive.HasValue)
            queryParams.Add($"isActive={filter.IsActive}");
            
        if (filter.ExpiringWithinDays.HasValue)
            queryParams.Add($"expiringWithinDays={filter.ExpiringWithinDays}");
            
        if (filter.ExpiredOnly.HasValue)
            queryParams.Add($"expiredOnly={filter.ExpiredOnly}");
            
        queryParams.Add($"page={filter.Page}");
        queryParams.Add($"pageSize={filter.PageSize}");
        
        if (!string.IsNullOrEmpty(filter.SortBy))
            queryParams.Add($"sortBy={filter.SortBy}");
            
        queryParams.Add($"sortDescending={filter.SortDescending}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<PagedResult<FacilityCertificationDto>>($"api/facility-certifications{queryString}");
    }

    public async Task<ApiResponse<FacilityCertificationDto>> GetCertificationByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<FacilityCertificationDto>($"api/facility-certifications/{id}");
    }

    public async Task<ApiResponse<List<FacilityCertificationDto>>> GetMemberCertificationsAsync(Guid memberId)
    {
        return await _apiService.GetAsync<List<FacilityCertificationDto>>($"api/facility-certifications/member/{memberId}");
    }

    public async Task<ApiResponse<FacilityCertificationDto>> CreateCertificationAsync(CreateCertificationRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<FacilityCertificationDto>("api/facility-certifications", content);
    }

    public async Task<ApiResponse<FacilityCertificationDto>> DeactivateCertificationAsync(Guid id, DeactivateCertificationRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PutAsync<FacilityCertificationDto>($"api/facility-certifications/{id}/deactivate", content);
    }

    public async Task<ApiResponse<List<FacilityCertificationDto>>> GetExpiringCertificationsAsync(int daysAhead = 30)
    {
        return await _apiService.GetAsync<List<FacilityCertificationDto>>($"api/facility-certifications/expiring?daysAhead={daysAhead}");
    }

    public async Task<ApiResponse<List<string>>> GetCertificationTypesAsync()
    {
        return await _apiService.GetAsync<List<string>>("api/facility-certifications/types");
    }

    // Member Facility Access
    public async Task<ApiResponse<List<FacilityDto>>> GetAccessibleFacilitiesForMemberAsync(Guid memberId)
    {
        return await _apiService.GetAsync<List<FacilityDto>>($"api/member-facility-access/{memberId}/accessible");
    }

    public async Task<ApiResponse<List<FacilityDto>>> GetRestrictedFacilitiesForMemberAsync(Guid memberId)
    {
        return await _apiService.GetAsync<List<FacilityDto>>($"api/member-facility-access/{memberId}/restricted");
    }

    public async Task<ApiResponse<BookingLimitValidationResult>> ValidateBookingLimitsAsync(ValidateBookingLimitsRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<BookingLimitValidationResult>("api/member-facility-access/validate-booking-limits", content);
    }
}