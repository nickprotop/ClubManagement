namespace ClubManagement.Shared.Models.Authorization;

public enum HardwareAction
{
    // Basic CRUD
    View,
    ViewDetails,
    Create,
    Edit,
    Delete,
    
    // Hardware Operations
    Assign,
    Return,
    BulkAssign,
    ChangeLocation,
    
    // Status Changes
    MarkMaintenance,
    MarkOutOfOrder,
    Retire,
    Reactivate,
    
    // Hardware Types
    CreateType,
    EditType,
    DeleteType,
    ManagePropertySchema,
    
    // Financial
    ViewFinancials,
    EditFinancials,
    ProcessFees,
    ManagePurchases,
    
    // Maintenance
    ScheduleMaintenance,
    PerformMaintenance,
    ViewMaintenanceHistory,
    
    // Inventory
    ManageInventory,
    AdjustStock,
    CreatePurchaseOrder,
    ReceiveShipment,
    
    // Reports
    ViewReports,
    GenerateReports,
    ExportData,
    
    // Event Integration
    ManageEventEquipment,
    AssignEventEquipment,
    ViewEventRequirements
}