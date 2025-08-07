using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Infrastructure.Data;

namespace ClubManagement.Infrastructure.Authorization;

public interface IHardwareAuthorizationService
{
    Task<HardwarePermissions> GetPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null);
    Task<bool> CanPerformActionAsync(Guid userId, HardwareAction action, ClubManagementDbContext tenantContext, Guid? hardwareId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, HardwareAction action, ClubManagementDbContext tenantContext, Guid? hardwareId = null);
    
    // Specific permission checks
    Task<bool> CanViewHardwareAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null);
    Task<bool> CanManageHardwareAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null);
    Task<bool> CanAssignHardwareAsync(Guid userId, ClubManagementDbContext tenantContext, Guid hardwareId);
    Task<bool> CanManageHardwareTypesAsync(Guid userId, ClubManagementDbContext tenantContext);
    Task<bool> CanViewFinancialsAsync(Guid userId, ClubManagementDbContext tenantContext);
    Task<bool> CanScheduleMaintenanceAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null);
    Task<bool> CanManageEventEquipmentAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? eventId = null);
}