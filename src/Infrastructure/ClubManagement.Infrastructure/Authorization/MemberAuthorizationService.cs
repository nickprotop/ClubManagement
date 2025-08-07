using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Authorization;

public class MemberAuthorizationService : IMemberAuthorizationService
{
    private readonly IMemberAuditService _auditService;

    public MemberAuthorizationService(IMemberAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<MemberPermissions> GetMemberPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? memberId = null)
    {
        var user = await GetUserWithRoleAsync(userId, tenantContext);
        if (user == null)
            return new MemberPermissions();

        var targetMember = memberId.HasValue ? await GetMemberAsync(memberId.Value, tenantContext) : null;
        var currentUserMember = await tenantContext.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        
        return user.Role switch
        {
            UserRole.Member => await GetMemberPermissionsForMemberAsync(user, currentUserMember, targetMember),
            UserRole.Staff => await GetMemberPermissionsForStaffAsync(user, targetMember),
            UserRole.Instructor => await GetMemberPermissionsForInstructorAsync(user, targetMember),
            UserRole.Coach => await GetMemberPermissionsForCoachAsync(user, targetMember),
            UserRole.Admin => await GetMemberPermissionsForAdminAsync(user, targetMember),
            UserRole.SuperAdmin => await GetMemberPermissionsForSuperAdminAsync(user, targetMember),
            _ => new MemberPermissions()
        };
    }

    public async Task<bool> CanPerformActionAsync(Guid userId, MemberAction action, ClubManagementDbContext tenantContext, Guid? memberId = null)
    {
        var permissions = await GetMemberPermissionsAsync(userId, tenantContext, memberId);
        
        return action switch
        {
            MemberAction.View => permissions.CanView,
            MemberAction.ViewDetails => permissions.CanViewDetails,
            MemberAction.Create => permissions.CanCreate,
            MemberAction.Edit => permissions.CanEdit,
            MemberAction.Delete => permissions.CanDelete,
            MemberAction.ChangeStatus => permissions.CanChangeStatus,
            MemberAction.ExtendMembership => permissions.CanExtendMembership,
            MemberAction.ChangeTier => permissions.CanChangeTier,
            MemberAction.ViewOwnProfile => permissions.CanViewOwnProfile,
            MemberAction.EditOwnProfile => permissions.CanEditOwnProfile,
            MemberAction.ViewOtherProfiles => permissions.CanViewOtherProfiles,
            MemberAction.EditOtherProfiles => permissions.CanEditOtherProfiles,
            MemberAction.ViewMedicalInfo => permissions.CanViewMedicalInfo,
            MemberAction.EditMedicalInfo => permissions.CanEditMedicalInfo,
            MemberAction.ViewEmergencyContact => permissions.CanViewEmergencyContact,
            MemberAction.EditEmergencyContact => permissions.CanEditEmergencyContact,
            MemberAction.ViewFinancialInfo => permissions.CanViewFinancialInfo,
            MemberAction.EditBalance => permissions.CanEditBalance,
            MemberAction.ExportMemberData => permissions.CanExportMemberData,
            MemberAction.ViewMemberActivity => permissions.CanViewMemberActivity,
            MemberAction.ViewAllMembers => permissions.CanViewAllMembers,
            MemberAction.ImpersonateMember => permissions.CanImpersonateMember,
            MemberAction.ViewContactInfo => permissions.CanViewContactInfo,
            MemberAction.SendNotifications => permissions.CanSendNotifications,
            MemberAction.AccessMemberDirectory => permissions.CanAccessMemberDirectory,
            _ => false
        };
    }

    public async Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, MemberAction action, ClubManagementDbContext tenantContext, Guid? memberId = null)
    {
        var user = await GetUserWithRoleAsync(userId, tenantContext);
        if (user == null)
            return AuthorizationResult.Failed("User not found");

        var targetMember = memberId.HasValue ? await GetMemberAsync(memberId.Value, tenantContext) : null;
        
        // Check basic permissions
        var canPerform = await CanPerformActionAsync(userId, action, tenantContext, memberId);
        if (!canPerform)
        {
            var permissions = await GetMemberPermissionsAsync(userId, tenantContext, memberId);
            var reasons = permissions.ReasonsDenied.Any() 
                ? permissions.ReasonsDenied 
                : new[] { $"User does not have permission to {action}" };
            
            // Log permission denied
            await _auditService.LogPermissionDeniedAsync(tenantContext, userId, action, memberId, reasons);
            
            return AuthorizationResult.Failed(reasons);
        }

        // Additional context-specific checks
        if (targetMember != null)
        {
            var contextCheck = await CheckMemberContextAsync(user, targetMember, action, tenantContext);
            if (!contextCheck.Succeeded)
            {
                // Log permission denied for context-specific failure
                await _auditService.LogPermissionDeniedAsync(tenantContext, userId, action, memberId, contextCheck.Reasons);
                return contextCheck;
            }
        }

        // Log successful authorization (but only for sensitive actions)
        if (IsSensitiveAction(action))
        {
            await _auditService.LogMemberActionAsync(tenantContext, userId, memberId, action, "Permission granted");
        }

        return AuthorizationResult.Success();
    }

    private async Task<MemberPermissions> GetMemberPermissionsForMemberAsync(User user, Member? currentUserMember, Member? targetMember)
    {
        var restrictions = new List<string>();
        var reasons = new List<string>();
        var isViewingOwnData = targetMember?.UserId == user.Id;

        if (currentUserMember == null)
        {
            reasons.Add("User is not a registered member");
            return new MemberPermissions { ReasonsDenied = reasons.ToArray() };
        }

        if (currentUserMember.Status != MembershipStatus.Active)
        {
            reasons.Add("Membership is not active");
            restrictions.Add("Inactive membership");
        }

        // Members can only view basic info of others, full access to own data
        var canViewOtherProfiles = currentUserMember.Status == MembershipStatus.Active;
        var canAccessDirectory = currentUserMember.Tier >= MembershipTier.Premium;

        if (!isViewingOwnData && targetMember != null)
            restrictions.Add("Limited access to other members' data");

        return new MemberPermissions
        {
            CanView = true,
            CanViewDetails = isViewingOwnData || canViewOtherProfiles,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            
            CanChangeStatus = false,
            CanExtendMembership = false,
            CanChangeTier = false,
            
            CanViewOwnProfile = true,
            CanEditOwnProfile = isViewingOwnData,
            CanViewOtherProfiles = canViewOtherProfiles,
            CanEditOtherProfiles = false,
            
            CanViewMedicalInfo = isViewingOwnData,
            CanEditMedicalInfo = isViewingOwnData,
            CanViewEmergencyContact = isViewingOwnData,
            CanEditEmergencyContact = isViewingOwnData,
            CanViewFinancialInfo = isViewingOwnData,
            CanEditBalance = false,
            
            CanExportMemberData = false,
            CanViewMemberActivity = isViewingOwnData,
            CanViewAllMembers = false,
            CanImpersonateMember = false,
            
            CanViewContactInfo = isViewingOwnData || canAccessDirectory,
            CanSendNotifications = false,
            CanAccessMemberDirectory = canAccessDirectory,
            
            Restrictions = restrictions.ToArray(),
            ReasonsDenied = reasons.ToArray(),
            IsViewingOwnData = isViewingOwnData
        };
    }

    private async Task<MemberPermissions> GetMemberPermissionsForStaffAsync(User user, Member? targetMember)
    {
        var restrictions = new List<string>();
        var canDeleteMember = targetMember == null || 
            (targetMember.Status != MembershipStatus.Active && !HasActiveRegistrations(targetMember));

        if (targetMember != null && HasActiveRegistrations(targetMember))
            restrictions.Add("Cannot delete members with active event registrations");

        return new MemberPermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = canDeleteMember,
            
            CanChangeStatus = true,
            CanExtendMembership = true,
            CanChangeTier = true,
            
            CanViewOwnProfile = true,
            CanEditOwnProfile = true,
            CanViewOtherProfiles = true,
            CanEditOtherProfiles = true,
            
            CanViewMedicalInfo = true,
            CanEditMedicalInfo = true,
            CanViewEmergencyContact = true,
            CanEditEmergencyContact = true,
            CanViewFinancialInfo = true,
            CanEditBalance = false, // Requires admin approval
            
            CanExportMemberData = true,
            CanViewMemberActivity = true,
            CanViewAllMembers = true,
            CanImpersonateMember = false,
            
            CanViewContactInfo = true,
            CanSendNotifications = true,
            CanAccessMemberDirectory = true,
            
            Restrictions = restrictions.ToArray()
        };
    }

    private async Task<MemberPermissions> GetMemberPermissionsForInstructorAsync(User user, Member? targetMember)
    {
        var restrictions = new List<string>();
        
        // Instructors have limited member access - mainly for their events
        restrictions.Add("Limited to members in your events and basic directory access");

        return new MemberPermissions
        {
            CanView = true,
            CanViewDetails = true, // For event management purposes
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            
            CanChangeStatus = false,
            CanExtendMembership = false,
            CanChangeTier = false,
            
            CanViewOwnProfile = true,
            CanEditOwnProfile = true,
            CanViewOtherProfiles = true, // Basic info only
            CanEditOtherProfiles = false,
            
            CanViewMedicalInfo = false, // Only in emergency situations during events
            CanEditMedicalInfo = false,
            CanViewEmergencyContact = false, // Only during events they're instructing
            CanEditEmergencyContact = false,
            CanViewFinancialInfo = false,
            CanEditBalance = false,
            
            CanExportMemberData = false,
            CanViewMemberActivity = false,
            CanViewAllMembers = false,
            CanImpersonateMember = false,
            
            CanViewContactInfo = true, // For event coordination
            CanSendNotifications = false,
            CanAccessMemberDirectory = true,
            
            Restrictions = restrictions.ToArray()
        };
    }

    private async Task<MemberPermissions> GetMemberPermissionsForCoachAsync(User user, Member? targetMember)
    {
        var restrictions = new List<string>();
        
        // Coaches have similar permissions to instructors but slightly more access
        restrictions.Add("Limited to members in your events and coaching activities");

        return new MemberPermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            
            CanChangeStatus = false,
            CanExtendMembership = false,
            CanChangeTier = false,
            
            CanViewOwnProfile = true,
            CanEditOwnProfile = true,
            CanViewOtherProfiles = true,
            CanEditOtherProfiles = false,
            
            CanViewMedicalInfo = false, // Only during events they're coaching
            CanEditMedicalInfo = false,
            CanViewEmergencyContact = false, // Only during events they're coaching  
            CanEditEmergencyContact = false,
            CanViewFinancialInfo = false,
            CanEditBalance = false,
            
            CanExportMemberData = false,
            CanViewMemberActivity = false,
            CanViewAllMembers = false,
            CanImpersonateMember = false,
            
            CanViewContactInfo = true,
            CanSendNotifications = false,
            CanAccessMemberDirectory = true,
            
            Restrictions = restrictions.ToArray()
        };
    }

    private async Task<MemberPermissions> GetMemberPermissionsForAdminAsync(User user, Member? targetMember)
    {
        return new MemberPermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            
            CanChangeStatus = true,
            CanExtendMembership = true,
            CanChangeTier = true,
            
            CanViewOwnProfile = true,
            CanEditOwnProfile = true,
            CanViewOtherProfiles = true,
            CanEditOtherProfiles = true,
            
            CanViewMedicalInfo = true,
            CanEditMedicalInfo = true,
            CanViewEmergencyContact = true,
            CanEditEmergencyContact = true,
            CanViewFinancialInfo = true,
            CanEditBalance = true,
            
            CanExportMemberData = true,
            CanViewMemberActivity = true,
            CanViewAllMembers = true,
            CanImpersonateMember = true, // Admins can impersonate
            
            CanViewContactInfo = true,
            CanSendNotifications = true,
            CanAccessMemberDirectory = true
        };
    }

    private async Task<MemberPermissions> GetMemberPermissionsForSuperAdminAsync(User user, Member? targetMember)
    {
        return new MemberPermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            
            CanChangeStatus = true,
            CanExtendMembership = true,
            CanChangeTier = true,
            
            CanViewOwnProfile = true,
            CanEditOwnProfile = true,
            CanViewOtherProfiles = true,
            CanEditOtherProfiles = true,
            
            CanViewMedicalInfo = true,
            CanEditMedicalInfo = true,
            CanViewEmergencyContact = true,
            CanEditEmergencyContact = true,
            CanViewFinancialInfo = true,
            CanEditBalance = true,
            
            CanExportMemberData = true,
            CanViewMemberActivity = true,
            CanViewAllMembers = true,
            CanImpersonateMember = true, // SuperAdmins can impersonate
            
            CanViewContactInfo = true,
            CanSendNotifications = true,
            CanAccessMemberDirectory = true
        };
    }

    private async Task<AuthorizationResult> CheckMemberContextAsync(User user, Member targetMember, MemberAction action, ClubManagementDbContext tenantContext)
    {
        var reasons = new List<string>();

        // Check if trying to modify/delete own admin account
        if (action is MemberAction.Delete or MemberAction.ChangeStatus)
        {
            if (targetMember.UserId == user.Id)
                reasons.Add("Cannot modify your own account status");
        }

        // Check if trying to impersonate higher privilege user
        if (action == MemberAction.ImpersonateMember)
        {
            var targetUser = await tenantContext.Users.FindAsync(targetMember.UserId);
            if (targetUser != null && IsHigherPrivilege(targetUser.Role, user.Role))
                reasons.Add("Cannot impersonate users with higher privileges");
        }

        // Check sensitive data access during business hours (example business rule)
        if (action is MemberAction.ViewMedicalInfo or MemberAction.ViewFinancialInfo)
        {
            var currentHour = DateTime.UtcNow.Hour;
            if (currentHour < 8 || currentHour > 18) // Outside business hours
                reasons.Add("Sensitive data access is restricted outside business hours");
        }

        return reasons.Any() ? AuthorizationResult.Failed(reasons.ToArray()) : AuthorizationResult.Success();
    }

    private async Task<User?> GetUserWithRoleAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<Member?> GetMemberAsync(Guid memberId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);
    }

    private static bool HasActiveRegistrations(Member member)
    {
        // This would typically check for active event registrations
        // For now, return false as a placeholder - this should be implemented based on business rules
        return false;
    }

    private static bool IsHigherPrivilege(UserRole targetRole, UserRole currentRole)
    {
        var roleHierarchy = new Dictionary<UserRole, int>
        {
            { UserRole.Member, 1 },
            { UserRole.Instructor, 2 },
            { UserRole.Coach, 3 },
            { UserRole.Staff, 4 },
            { UserRole.Admin, 5 },
            { UserRole.SuperAdmin, 6 }
        };

        return roleHierarchy.GetValueOrDefault(targetRole, 0) >= roleHierarchy.GetValueOrDefault(currentRole, 0);
    }

    private static bool IsSensitiveAction(MemberAction action)
    {
        return action switch
        {
            MemberAction.Delete => true,
            MemberAction.ChangeStatus => true,
            MemberAction.ViewMedicalInfo => true,
            MemberAction.EditMedicalInfo => true,
            MemberAction.ViewFinancialInfo => true,
            MemberAction.EditBalance => true,
            MemberAction.ImpersonateMember => true,
            MemberAction.ExportMemberData => true,
            MemberAction.EditOtherProfiles => true,
            _ => false
        };
    }
}