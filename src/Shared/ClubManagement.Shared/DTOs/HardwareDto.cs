using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class HardwareDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public Guid HardwareTypeId { get; set; }
    public string HardwareTypeName { get; set; } = string.Empty;
    public string HardwareTypeIcon { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public HardwareStatus Status { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Assignment information
    public bool IsCurrentlyAssigned { get; set; }
    public HardwareAssignmentDto? CurrentAssignment { get; set; }
    
    // Computed fields
    public bool IsAvailableForAssignment { get; set; }
    public bool IsMaintenanceDue { get; set; }
    public int DaysUntilMaintenance { get; set; }
}

public class CreateHardwareRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public Guid HardwareTypeId { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class UpdateHardwareRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid HardwareTypeId { get; set; }
    public string? SerialNumber { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public HardwareStatus Status { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class HardwareTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public PropertySchema PropertySchema { get; set; } = new();
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public bool RequiresAssignment { get; set; }
    public bool AllowMultipleAssignments { get; set; }
    public int? MaxAssignmentDurationHours { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Computed fields
    public int HardwareCount { get; set; }
    public int AvailableCount { get; set; }
    public int AssignedCount { get; set; }
    public int MaintenanceCount { get; set; }
}

public class CreateHardwareTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public PropertySchema PropertySchema { get; set; } = new();
    public bool RequiresAssignment { get; set; } = true;
    public bool AllowMultipleAssignments { get; set; } = false;
    public int? MaxAssignmentDurationHours { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateHardwareTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public PropertySchema PropertySchema { get; set; } = new();
    public bool IsActive { get; set; }
    public bool RequiresAssignment { get; set; }
    public bool AllowMultipleAssignments { get; set; }
    public int? MaxAssignmentDurationHours { get; set; }
    public int SortOrder { get; set; }
}

public class HardwareAssignmentDto
{
    public Guid Id { get; set; }
    public Guid HardwareId { get; set; }
    public string HardwareName { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public Guid AssignedByUserId { get; set; }
    public string AssignedByUserName { get; set; } = string.Empty;
    public string HardwareSerialNumber { get; set; } = string.Empty;
    public string HardwareTypeName { get; set; } = string.Empty;
    public string AssignedByName { get; set; } = string.Empty;
    public string? ReturnedByName { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public Guid? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? ReturnNotes { get; set; }
    public decimal? LateFee { get; set; }
    public decimal? DamageFee { get; set; }
    
    // Computed fields
    public bool IsOverdue { get; set; }
    public int DaysAssigned { get; set; }
    public DateTime? DueDate { get; set; }
}

public class CreateHardwareAssignmentRequest
{
    public Guid HardwareId { get; set; }
    public Guid MemberId { get; set; }
    public string? Notes { get; set; }
    public DateTime? DueDate { get; set; }
}

public class ReturnHardwareRequest
{
    public Guid AssignmentId { get; set; }
    public AssignmentStatus? Status { get; set; }
    public string? ReturnNotes { get; set; }
    public decimal? LateFee { get; set; }
    public decimal? DamageFee { get; set; }
}

public class HardwareListFilter
{
    public string? Search { get; set; }
    public Guid? HardwareTypeId { get; set; }
    public HardwareStatus? Status { get; set; }
    public string? Location { get; set; }
    public bool? MaintenanceDue { get; set; }
    public bool? Available { get; set; }
    public DateTime? PurchasedAfter { get; set; }
    public DateTime? PurchasedBefore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

public class HardwareUsageHistoryDto
{
    public Guid Id { get; set; }
    public Guid HardwareId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan Duration { get; set; }
    public EventEquipmentAssignmentStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? Notes { get; set; }
}

public class HardwareDashboardDto
{
    public int TotalHardware { get; set; }
    public int AvailableHardware { get; set; }
    public int AssignedHardware { get; set; }
    public int MaintenanceHardware { get; set; }
    public int OutOfOrderHardware { get; set; }
    public decimal TotalAssetValue { get; set; }
    public int MaintenanceDueCount { get; set; }
    public int OverdueAssignments { get; set; }
    public List<HardwareTypeDto> PopularTypes { get; set; } = new();
    public List<HardwareDto> RecentlyAdded { get; set; } = new();
    public List<HardwareAssignmentDto> RecentAssignments { get; set; } = new();
}

public class HardwareAssignmentFilter
{
    public string? Search { get; set; }
    public Guid? HardwareId { get; set; }
    public Guid? MemberId { get; set; }
    public AssignmentStatus? Status { get; set; }
    public DateTime? AssignedAfter { get; set; }
    public DateTime? AssignedBefore { get; set; }
    public DateTime? ReturnedAfter { get; set; }
    public DateTime? ReturnedBefore { get; set; }
    public bool? IsOverdue { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "AssignedDate";
    public bool SortDescending { get; set; } = true;
}

public class AssignHardwareRequest
{
    public Guid HardwareId { get; set; }
    public Guid MemberId { get; set; }
    public string? Notes { get; set; }
}


public class HardwareAssignmentDashboardDto
{
    public int TotalAssignments { get; set; }
    public int ActiveAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int ReturnedThisMonth { get; set; }
    public int DamagedReturns { get; set; }
    public int LostItems { get; set; }
    public decimal TotalFeesCollected { get; set; }
}