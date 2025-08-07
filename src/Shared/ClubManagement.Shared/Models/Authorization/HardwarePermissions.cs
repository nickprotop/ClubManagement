namespace ClubManagement.Shared.Models.Authorization;

public class HardwarePermissions
{
    // Basic CRUD Operations
    public bool CanView { get; set; }
    public bool CanViewDetails { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    
    // Hardware Management
    public bool CanAssign { get; set; }
    public bool CanReturn { get; set; }
    public bool CanViewAssignments { get; set; }
    public bool CanModifyAssignments { get; set; }
    public bool CanBulkAssign { get; set; }
    
    // Status Management
    public bool CanMarkMaintenance { get; set; }
    public bool CanMarkOutOfOrder { get; set; }
    public bool CanRetire { get; set; }
    public bool CanReactivate { get; set; }
    public bool CanChangeLocation { get; set; }
    
    // Hardware Types
    public bool CanCreateTypes { get; set; }
    public bool CanEditTypes { get; set; }
    public bool CanDeleteTypes { get; set; }
    public bool CanManagePropertySchemas { get; set; }
    
    // Financial Operations
    public bool CanViewFinancials { get; set; }
    public bool CanEditFinancials { get; set; }
    public bool CanProcessFees { get; set; }
    public bool CanManagePurchases { get; set; }
    public bool CanViewAssetValue { get; set; }
    
    // Maintenance Operations
    public bool CanScheduleMaintenance { get; set; }
    public bool CanPerformMaintenance { get; set; }
    public bool CanViewMaintenanceHistory { get; set; }
    public bool CanCreateMaintenanceTemplates { get; set; }
    
    // Inventory Management
    public bool CanManageInventory { get; set; }
    public bool CanAdjustStock { get; set; }
    public bool CanCreatePurchaseOrders { get; set; }
    public bool CanReceiveShipments { get; set; }
    
    // Reporting and Analytics
    public bool CanViewReports { get; set; }
    public bool CanGenerateReports { get; set; }
    public bool CanExportData { get; set; }
    public bool CanViewAnalytics { get; set; }
    
    // Event Integration
    public bool CanManageEventEquipment { get; set; }
    public bool CanAssignEventEquipment { get; set; }
    public bool CanViewEventEquipmentRequirements { get; set; }
    
    // Access Scope
    public bool CanViewAll { get; set; }
    public bool CanViewOwn { get; set; }
    public bool CanViewAssigned { get; set; }
    
    // Context Information
    public string[] Restrictions { get; set; } = Array.Empty<string>();
    public string[] ReasonsDenied { get; set; } = Array.Empty<string>();
    public bool IsAssignedToUser { get; set; }
    public bool IsUserResponsible { get; set; }
    public MembershipTier? RequiredTierForAction { get; set; }
}