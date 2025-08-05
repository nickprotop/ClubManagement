using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IMemberPermissionService
{
    Task<MemberPermissions> GetMemberPermissionsAsync(Guid? memberId = null);
    Task<MemberPermissions> GetGeneralMemberPermissionsAsync();
    void ClearPermissionsCache();
    Task<bool> CanPerformActionAsync(MemberAction action, Guid? memberId = null);
}