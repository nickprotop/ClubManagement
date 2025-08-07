using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using System.Text.Json;
using System.Web;

namespace ClubManagement.Client.Services;

public class HardwareAssignmentService : IHardwareAssignmentService
{
    private readonly IApiService _apiService;
    private readonly JsonSerializerOptions _jsonOptions;

    public HardwareAssignmentService(IApiService apiService)
    {
        _apiService = apiService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetAssignmentsAsync(HardwareAssignmentFilter filter)
    {
        var queryParams = BuildQueryString(filter);
        return await _apiService.GetAsync<PagedResult<HardwareAssignmentDto>>($"/api/hardwareassignment{queryParams}");
    }

    public async Task<ApiResponse<HardwareAssignmentDto>> GetAssignmentAsync(Guid id)
    {
        return await _apiService.GetAsync<HardwareAssignmentDto>($"/api/hardwareassignment/{id}");
    }

    public async Task<ApiResponse<HardwareAssignmentDto>> AssignHardwareAsync(AssignHardwareRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        return await _apiService.PostAsync<HardwareAssignmentDto>("/api/hardwareassignment/assign", json);
    }

    public async Task<ApiResponse<HardwareAssignmentDto>> ReturnHardwareAsync(Guid assignmentId, ReturnHardwareRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        return await _apiService.PostAsync<HardwareAssignmentDto>($"/api/hardwareassignment/{assignmentId}/return", json);
    }

    public async Task<ApiResponse<HardwareAssignmentDashboardDto>> GetDashboardAsync()
    {
        return await _apiService.GetAsync<HardwareAssignmentDashboardDto>("/api/hardwareassignment/dashboard");
    }

    public async Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetActiveAssignmentsAsync(int page = 1, int pageSize = 50)
    {
        var filter = new HardwareAssignmentFilter
        {
            IsActive = true,
            Page = page,
            PageSize = pageSize,
            SortBy = "AssignedDate",
            SortDescending = true
        };
        return await GetAssignmentsAsync(filter);
    }

    public async Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetOverdueAssignmentsAsync(int page = 1, int pageSize = 50)
    {
        var filter = new HardwareAssignmentFilter
        {
            IsOverdue = true,
            Page = page,
            PageSize = pageSize,
            SortBy = "AssignedDate",
            SortDescending = false // Oldest first for overdue
        };
        return await GetAssignmentsAsync(filter);
    }

    public async Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetAssignmentsByMemberAsync(Guid memberId, int page = 1, int pageSize = 50)
    {
        var filter = new HardwareAssignmentFilter
        {
            MemberId = memberId,
            Page = page,
            PageSize = pageSize,
            SortBy = "AssignedDate",
            SortDescending = true
        };
        return await GetAssignmentsAsync(filter);
    }

    public async Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetAssignmentsByHardwareAsync(Guid hardwareId, int page = 1, int pageSize = 50)
    {
        var filter = new HardwareAssignmentFilter
        {
            HardwareId = hardwareId,
            Page = page,
            PageSize = pageSize,
            SortBy = "AssignedDate",
            SortDescending = true
        };
        return await GetAssignmentsAsync(filter);
    }

    private string BuildQueryString(HardwareAssignmentFilter filter)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(filter.Search))
            queryParams.Add($"search={HttpUtility.UrlEncode(filter.Search)}");
            
        if (filter.HardwareId.HasValue)
            queryParams.Add($"hardwareId={filter.HardwareId}");
            
        if (filter.MemberId.HasValue)
            queryParams.Add($"memberId={filter.MemberId}");
            
        if (filter.Status.HasValue)
            queryParams.Add($"status={filter.Status}");
            
        if (filter.AssignedAfter.HasValue)
            queryParams.Add($"assignedAfter={filter.AssignedAfter:yyyy-MM-ddTHH:mm:ss}");
            
        if (filter.AssignedBefore.HasValue)
            queryParams.Add($"assignedBefore={filter.AssignedBefore:yyyy-MM-ddTHH:mm:ss}");
            
        if (filter.ReturnedAfter.HasValue)
            queryParams.Add($"returnedAfter={filter.ReturnedAfter:yyyy-MM-ddTHH:mm:ss}");
            
        if (filter.ReturnedBefore.HasValue)
            queryParams.Add($"returnedBefore={filter.ReturnedBefore:yyyy-MM-ddTHH:mm:ss}");
            
        if (filter.IsOverdue.HasValue)
            queryParams.Add($"isOverdue={filter.IsOverdue}");
            
        if (filter.IsActive.HasValue)
            queryParams.Add($"isActive={filter.IsActive}");

        queryParams.Add($"page={filter.Page}");
        queryParams.Add($"pageSize={filter.PageSize}");
        queryParams.Add($"sortBy={filter.SortBy}");
        queryParams.Add($"sortDescending={filter.SortDescending}");

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
    }
}