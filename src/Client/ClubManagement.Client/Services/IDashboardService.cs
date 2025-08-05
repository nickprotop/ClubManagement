using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IDashboardService
{
    Task<ApiResponse<DashboardDataDto>> GetDashboardDataAsync();
}