using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IHardwareAssignmentService
{
    Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetAssignmentsAsync(HardwareAssignmentFilter filter);
    Task<ApiResponse<HardwareAssignmentDto>> GetAssignmentAsync(Guid id);
    Task<ApiResponse<HardwareAssignmentDto>> AssignHardwareAsync(AssignHardwareRequest request);
    Task<ApiResponse<HardwareAssignmentDto>> ReturnHardwareAsync(Guid assignmentId, ReturnHardwareRequest request);
    Task<ApiResponse<HardwareAssignmentDashboardDto>> GetDashboardAsync();
    
    // Convenience methods
    Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetActiveAssignmentsAsync(int page = 1, int pageSize = 50);
    Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetOverdueAssignmentsAsync(int page = 1, int pageSize = 50);
    Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetAssignmentsByMemberAsync(Guid memberId, int page = 1, int pageSize = 50);
    Task<ApiResponse<PagedResult<HardwareAssignmentDto>>> GetAssignmentsByHardwareAsync(Guid hardwareId, int page = 1, int pageSize = 50);
}