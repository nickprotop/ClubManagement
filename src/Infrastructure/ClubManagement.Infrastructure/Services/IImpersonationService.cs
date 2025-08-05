using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Services;

public interface IImpersonationService
{
    Task<ImpersonationResult> StartImpersonationAsync(Guid adminUserId, StartImpersonationRequest request, string? ipAddress = null, string? userAgent = null);
    Task<AuthorizationResult> EndImpersonationAsync(Guid sessionId, string? reason = null);
    Task<ImpersonationStatusDto?> GetCurrentImpersonationStatusAsync(Guid userId);
    Task<bool> IsUserImpersonatingAsync(Guid userId);
    Task<Guid?> GetOriginalUserIdAsync(Guid currentUserId);
    Task<List<ImpersonationSessionDto>> GetActiveSessionsAsync();
    Task<List<ImpersonationSessionDto>> GetSessionHistoryAsync(int page = 1, int pageSize = 50);
    Task LogImpersonationActionAsync(Guid sessionId, string action);
    Task CleanupExpiredSessionsAsync();
    Task<AuthorizationResult> ValidateImpersonationPermissionAsync(Guid adminUserId, Guid targetMemberId);
}