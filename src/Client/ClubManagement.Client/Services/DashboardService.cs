using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class DashboardService : IDashboardService
{
    private readonly IApiService _apiService;

    public DashboardService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<DashboardDataDto>> GetDashboardDataAsync()
    {
        return await _apiService.GetAsync<DashboardDataDto>("/api/dashboard");
    }
}