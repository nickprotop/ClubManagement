using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Infrastructure.Data;

namespace ClubManagement.Infrastructure.Authorization;

public interface IMemberAuthorizationService
{
    Task<MemberPermissions> GetMemberPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? memberId = null);
    Task<bool> CanPerformActionAsync(Guid userId, MemberAction action, ClubManagementDbContext tenantContext, Guid? memberId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, MemberAction action, ClubManagementDbContext tenantContext, Guid? memberId = null);
}