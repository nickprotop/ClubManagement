using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Authorization;

public class HardwareAuthorizationService : IHardwareAuthorizationService
{

    public async Task<HardwarePermissions> GetPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null)
    {
        var user = await GetUserWithRoleAsync(userId, tenantContext);
        if (user == null) return new HardwarePermissions();

        var hardware = hardwareId.HasValue ? await GetHardwareAsync(hardwareId.Value, tenantContext) : null;
        
        return user.Role switch
        {
            UserRole.Member => GetMemberPermissions(user, hardware),
            UserRole.Staff => GetStaffPermissions(user, hardware),
            UserRole.Instructor => GetInstructorPermissions(user, hardware),
            UserRole.Coach => GetCoachPermissions(user, hardware),
            UserRole.Admin => GetAdminPermissions(user, hardware),
            UserRole.SuperAdmin => GetSuperAdminPermissions(user, hardware),
            _ => new HardwarePermissions()
        };
    }

    public async Task<bool> CanPerformActionAsync(Guid userId, HardwareAction action, ClubManagementDbContext tenantContext, Guid? hardwareId = null)
    {
        var result = await CheckAuthorizationAsync(userId, action, tenantContext, hardwareId);
        return result.Succeeded;
    }

    public async Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, HardwareAction action, ClubManagementDbContext tenantContext, Guid? hardwareId = null)
    {
        var permissions = await GetPermissionsAsync(userId, tenantContext, hardwareId);
        
        var canPerform = action switch
        {
            HardwareAction.View => permissions.CanView,
            HardwareAction.ViewDetails => permissions.CanViewDetails,
            HardwareAction.Create => permissions.CanCreate,
            HardwareAction.Edit => permissions.CanEdit,
            HardwareAction.Delete => permissions.CanDelete,
            HardwareAction.Assign => permissions.CanAssign,
            HardwareAction.Return => permissions.CanReturn,
            HardwareAction.BulkAssign => permissions.CanBulkAssign,
            HardwareAction.MarkMaintenance => permissions.CanMarkMaintenance,
            HardwareAction.MarkOutOfOrder => permissions.CanMarkOutOfOrder,
            HardwareAction.Retire => permissions.CanRetire,
            HardwareAction.CreateType => permissions.CanCreateTypes,
            HardwareAction.EditType => permissions.CanEditTypes,
            HardwareAction.DeleteType => permissions.CanDeleteTypes,
            HardwareAction.ViewFinancials => permissions.CanViewFinancials,
            HardwareAction.ProcessFees => permissions.CanProcessFees,
            HardwareAction.ScheduleMaintenance => permissions.CanScheduleMaintenance,
            HardwareAction.ManageInventory => permissions.CanManageInventory,
            HardwareAction.ManageEventEquipment => permissions.CanManageEventEquipment,
            _ => false
        };

        return canPerform 
            ? AuthorizationResult.Success() 
            : AuthorizationResult.Failed(permissions.ReasonsDenied);
    }

    public async Task<bool> CanViewHardwareAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null)
    {
        return await CanPerformActionAsync(userId, HardwareAction.View, tenantContext, hardwareId);
    }

    public async Task<bool> CanManageHardwareAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null)
    {
        var permissions = await GetPermissionsAsync(userId, tenantContext, hardwareId);
        return permissions.CanCreate || permissions.CanEdit || permissions.CanDelete;
    }

    public async Task<bool> CanAssignHardwareAsync(Guid userId, ClubManagementDbContext tenantContext, Guid hardwareId)
    {
        return await CanPerformActionAsync(userId, HardwareAction.Assign, tenantContext, hardwareId);
    }

    public async Task<bool> CanManageHardwareTypesAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await CanPerformActionAsync(userId, HardwareAction.CreateType, tenantContext);
    }

    public async Task<bool> CanViewFinancialsAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await CanPerformActionAsync(userId, HardwareAction.ViewFinancials, tenantContext);
    }

    public async Task<bool> CanScheduleMaintenanceAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? hardwareId = null)
    {
        return await CanPerformActionAsync(userId, HardwareAction.ScheduleMaintenance, tenantContext, hardwareId);
    }

    public async Task<bool> CanManageEventEquipmentAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? eventId = null)
    {
        return await CanPerformActionAsync(userId, HardwareAction.ManageEventEquipment, tenantContext);
    }

    private async Task<User?> GetUserWithRoleAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<Hardware?> GetHardwareAsync(Guid hardwareId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Hardware
            .Include(h => h.Assignments.Where(a => a.Status == AssignmentStatus.Active))
            .FirstOrDefaultAsync(h => h.Id == hardwareId);
    }

    private HardwarePermissions GetMemberPermissions(User user, Hardware? hardware)
    {
        var isAssignedToUser = hardware?.Assignments?.Any(a => a.MemberId.ToString() == user.Id.ToString() && a.Status == AssignmentStatus.Active) ?? false;
        
        return new HardwarePermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanViewAssignments = isAssignedToUser, // Only own assignments
            CanViewOwn = true,
            IsAssignedToUser = isAssignedToUser,
            
            // Members can only return their own equipment
            CanReturn = isAssignedToUser,
            
            Restrictions = isAssignedToUser ? Array.Empty<string>() : new[] { "Limited to assigned equipment" }
        };
    }

    private HardwarePermissions GetStaffPermissions(User user, Hardware? hardware)
    {
        return new HardwarePermissions
        {
            // Basic Operations
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            
            // Assignment Operations
            CanAssign = true,
            CanReturn = true,
            CanViewAssignments = true,
            CanModifyAssignments = true,
            
            // Status Management
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanChangeLocation = true,
            
            // Limited access to advanced features
            CanViewAll = true,
            CanScheduleMaintenance = true,
            
            // Event Integration
            CanManageEventEquipment = true,
            CanAssignEventEquipment = true,
            CanViewEventEquipmentRequirements = true,
            
            Restrictions = new[] { "Cannot delete hardware", "Cannot manage types", "Limited financial access" }
        };
    }

    private HardwarePermissions GetInstructorPermissions(User user, Hardware? hardware)
    {
        return new HardwarePermissions
        {
            // View and basic operations
            CanView = true,
            CanViewDetails = true,
            CanCreate = false,
            CanEdit = false,
            
            // Limited assignment capabilities
            CanAssign = true, // For their classes
            CanReturn = true,
            CanViewAssignments = true,
            
            // Event Integration - Limited to their events
            CanManageEventEquipment = true,
            CanAssignEventEquipment = true,
            CanViewEventEquipmentRequirements = true,
            
            CanViewAll = false,
            CanViewOwn = true,
            
            Restrictions = new[] { "Limited to equipment for own classes", "Cannot create or delete hardware" }
        };
    }

    private HardwarePermissions GetCoachPermissions(User user, Hardware? hardware)
    {
        return new HardwarePermissions
        {
            // Enhanced permissions for coaches
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            
            // Full assignment capabilities
            CanAssign = true,
            CanReturn = true,
            CanViewAssignments = true,
            CanModifyAssignments = true,
            CanBulkAssign = true,
            
            // Status management
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanChangeLocation = true,
            
            // Maintenance
            CanScheduleMaintenance = true,
            CanViewMaintenanceHistory = true,
            
            // Event Integration
            CanManageEventEquipment = true,
            CanAssignEventEquipment = true,
            CanViewEventEquipmentRequirements = true,
            
            CanViewAll = true,
            
            Restrictions = new[] { "Cannot delete hardware", "Cannot manage hardware types", "Limited financial access" }
        };
    }

    private HardwarePermissions GetAdminPermissions(User user, Hardware? hardware)
    {
        return new HardwarePermissions
        {
            // Full CRUD permissions
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            
            // Full hardware management
            CanAssign = true,
            CanReturn = true,
            CanViewAssignments = true,
            CanModifyAssignments = true,
            CanBulkAssign = true,
            
            // Status management
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanRetire = true,
            CanReactivate = true,
            CanChangeLocation = true,
            
            // Hardware Types
            CanCreateTypes = true,
            CanEditTypes = true,
            CanDeleteTypes = true,
            CanManagePropertySchemas = true,
            
            // Financial
            CanViewFinancials = true,
            CanEditFinancials = true,
            CanProcessFees = true,
            CanManagePurchases = true,
            CanViewAssetValue = true,
            
            // Maintenance
            CanScheduleMaintenance = true,
            CanPerformMaintenance = true,
            CanViewMaintenanceHistory = true,
            CanCreateMaintenanceTemplates = true,
            
            // Inventory
            CanManageInventory = true,
            CanAdjustStock = true,
            CanCreatePurchaseOrders = true,
            CanReceiveShipments = true,
            
            // Reports
            CanViewReports = true,
            CanGenerateReports = true,
            CanExportData = true,
            CanViewAnalytics = true,
            
            // Event Integration
            CanManageEventEquipment = true,
            CanAssignEventEquipment = true,
            CanViewEventEquipmentRequirements = true,
            
            // Access scope
            CanViewAll = true,
            CanViewOwn = true,
            CanViewAssigned = true
        };
    }

    private HardwarePermissions GetSuperAdminPermissions(User user, Hardware? hardware)
    {
        // SuperAdmin has all permissions
        return new HardwarePermissions
        {
            // All permissions set to true
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanAssign = true,
            CanReturn = true,
            CanViewAssignments = true,
            CanModifyAssignments = true,
            CanBulkAssign = true,
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanRetire = true,
            CanReactivate = true,
            CanChangeLocation = true,
            CanCreateTypes = true,
            CanEditTypes = true,
            CanDeleteTypes = true,
            CanManagePropertySchemas = true,
            CanViewFinancials = true,
            CanEditFinancials = true,
            CanProcessFees = true,
            CanManagePurchases = true,
            CanViewAssetValue = true,
            CanScheduleMaintenance = true,
            CanPerformMaintenance = true,
            CanViewMaintenanceHistory = true,
            CanCreateMaintenanceTemplates = true,
            CanManageInventory = true,
            CanAdjustStock = true,
            CanCreatePurchaseOrders = true,
            CanReceiveShipments = true,
            CanViewReports = true,
            CanGenerateReports = true,
            CanExportData = true,
            CanViewAnalytics = true,
            CanManageEventEquipment = true,
            CanAssignEventEquipment = true,
            CanViewEventEquipmentRequirements = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanViewAssigned = true
        };
    }
}