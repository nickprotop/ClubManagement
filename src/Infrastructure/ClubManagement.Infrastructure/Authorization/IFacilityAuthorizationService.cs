using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Infrastructure.Data;

namespace ClubManagement.Infrastructure.Authorization;

public interface IFacilityAuthorizationService
{
    Task<FacilityPermissions> GetPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null);
    Task<bool> CanPerformActionAsync(Guid userId, FacilityAction action, ClubManagementDbContext tenantContext, Guid? facilityId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, FacilityAction action, ClubManagementDbContext tenantContext, Guid? facilityId = null);
    
    // Specific permission checks
    Task<bool> CanViewFacilitiesAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null);
    Task<bool> CanManageFacilitiesAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null);
    Task<bool> CanBookFacilityAsync(Guid userId, ClubManagementDbContext tenantContext, Guid facilityId);
    Task<bool> CanManageFacilityTypesAsync(Guid userId, ClubManagementDbContext tenantContext);
    Task<bool> CanViewFinancialsAsync(Guid userId, ClubManagementDbContext tenantContext);
    Task<bool> CanAccessMemberPortalAsync(Guid userId, ClubManagementDbContext tenantContext);
    Task<bool> CanViewBookingsAsync(Guid userId, ClubManagementDbContext tenantContext, bool ownOnly = false);
    Task<bool> CanCheckInMembersAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null);
    Task<bool> CanManageEventFacilitiesAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? eventId = null);
    
    // Member-specific authorization
    Task<bool> CanMemberAccessFacilityAsync(Guid userId, Guid facilityId, ClubManagementDbContext tenantContext);
    Task<FacilityPermissions> GetMemberFacilityPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null);
}