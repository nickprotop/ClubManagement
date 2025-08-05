using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Authorization;

public interface IMemberAuthorizationService
{
    Task<MemberPermissions> GetMemberPermissionsAsync(Guid userId, Guid? memberId = null);
    Task<bool> CanPerformActionAsync(Guid userId, MemberAction action, Guid? memberId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, MemberAction action, Guid? memberId = null);
}