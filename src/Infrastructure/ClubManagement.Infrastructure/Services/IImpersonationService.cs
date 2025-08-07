using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Infrastructure.Data;

namespace ClubManagement.Infrastructure.Services;

public interface IImpersonationService
{
    Task<ImpersonationResult> StartImpersonationAsync(ClubManagementDbContext tenantContext, Guid adminUserId, StartImpersonationRequest request, string? ipAddress = null, string? userAgent = null);
    Task<AuthorizationResult> EndImpersonationAsync(ClubManagementDbContext tenantContext, Guid sessionId, string? reason = null);
    Task<ImpersonationStatusDto?> GetCurrentImpersonationStatusAsync(ClubManagementDbContext tenantContext, Guid userId);
    Task<bool> IsUserImpersonatingAsync(ClubManagementDbContext tenantContext, Guid userId);
    Task<Guid?> GetOriginalUserIdAsync(ClubManagementDbContext tenantContext, Guid currentUserId);
    Task<List<ImpersonationSessionDto>> GetActiveSessionsAsync(ClubManagementDbContext tenantContext);
    Task<List<ImpersonationSessionDto>> GetSessionHistoryAsync(ClubManagementDbContext tenantContext, int page = 1, int pageSize = 50);
    Task LogImpersonationActionAsync(ClubManagementDbContext tenantContext, Guid sessionId, string action);
    Task CleanupExpiredSessionsAsync(ClubManagementDbContext tenantContext);
    Task<AuthorizationResult> ValidateImpersonationPermissionAsync(ClubManagementDbContext tenantContext, Guid adminUserId, Guid targetMemberId);
}