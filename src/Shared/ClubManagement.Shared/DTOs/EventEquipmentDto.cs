using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class EventEquipmentRequirementDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? HardwareTypeId { get; set; }
    public int QuantityRequired { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public string? Notes { get; set; }
    public bool AutoAssign { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Read-only computed fields
    public string? HardwareTypeName { get; set; }
    public string? HardwareTypeIcon { get; set; }
    public int QuantityAssigned { get; set; }
    public int QuantityRemaining { get; set; }
    public bool IsFulfilled { get; set; }
    public List<EventEquipmentAssignmentDto> AssignedHardware { get; set; } = new();
}

public class CreateEventEquipmentRequirementRequest
{
    public Guid HardwareTypeId { get; set; }
    public int QuantityRequired { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public string? Notes { get; set; }
    public bool AutoAssign { get; set; } = false;
}

public class EventEquipmentAvailabilityDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public List<EquipmentAvailabilityItem> Requirements { get; set; } = new();
    public bool AllRequirementsAvailable { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class EquipmentAvailabilityItem
{
    public Guid RequirementId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsMandatory { get; set; }
    public List<HardwareItemAvailability> AvailableItems { get; set; } = new();
    public List<HardwareItemAvailability> ConflictingItems { get; set; } = new();
}

public class HardwareItemAvailability
{
    public Guid HardwareId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public HardwareStatus Status { get; set; }
    public string? Location { get; set; }
    public bool IsAvailable { get; set; }
    public string? ConflictReason { get; set; }
    public List<ConflictingEquipmentEventDto> ConflictingEvents { get; set; } = new();
}

public class ConflictingEquipmentEventDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public Guid? AssignmentId { get; set; }
}

public class EventEquipmentSummaryDto
{
    public Guid EventId { get; set; }
    public int TotalRequirements { get; set; }
    public int MandatoryRequirements { get; set; }
    public int OptionalRequirements { get; set; }
    public int AvailableRequirements { get; set; }
    public int UnavailableRequirements { get; set; }
    public int AssignedRequirements { get; set; }
    public List<EventEquipmentRequirementDto> Requirements { get; set; } = new();
    public bool ReadyForEvent { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class EventEquipmentAssignmentDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid RequirementId { get; set; }
    public Guid HardwareId { get; set; }
    public string HardwareName { get; set; } = string.Empty;
    public string? HardwareSerialNumber { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? Notes { get; set; }
    public EventEquipmentAssignmentStatus Status { get; set; }
}

public class BulkEquipmentAssignmentRequest
{
    public Guid EventId { get; set; }
    public List<EquipmentAssignmentItem> Assignments { get; set; } = new();
    public bool AutoResolveConflicts { get; set; } = true;
    public string? Notes { get; set; }
}

public class EquipmentAssignmentItem
{
    public Guid RequirementId { get; set; }
    public List<Guid> HardwareIds { get; set; } = new();
    public Guid? MemberId { get; set; }
}



// Enhanced Event DTOs to include equipment
public class EventWithEquipmentDto : EventDto
{
    public List<EventEquipmentRequirementDto> EquipmentRequirements { get; set; } = new();
    public EventEquipmentSummaryDto? EquipmentSummary { get; set; }
    
    // Keep legacy support
    public new List<string> RequiredEquipment { get; set; } = new();
}

public class CreateEventWithEquipmentRequest : CreateEventRequest
{
    public List<CreateEventEquipmentRequirementRequest> EquipmentRequirements { get; set; } = new();
    
    // Keep legacy support
    public new List<string> RequiredEquipment { get; set; } = new();
}

public class UpdateEventWithEquipmentRequest : UpdateEventRequest
{
    public List<CreateEventEquipmentRequirementRequest> EquipmentRequirements { get; set; } = new();
    
    // Keep legacy support  
    public new List<string> RequiredEquipment { get; set; } = new();
}

// Equipment usage analytics
public class EventEquipmentUsageDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public List<EquipmentUsageItem> EquipmentUsage { get; set; } = new();
    public int TotalEquipmentUsed { get; set; }
    public decimal TotalEquipmentValue { get; set; }
}

public class EquipmentUsageItem
{
    public Guid HardwareId { get; set; }
    public string HardwareName { get; set; } = string.Empty;
    public string HardwareType { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
    public decimal? EquipmentValue { get; set; }
    public TimeSpan UsageDuration { get; set; }
}

// Additional DTOs needed by EventEquipmentController
public class UpdateEventEquipmentRequirementRequest
{
    public int QuantityRequired { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public bool AutoAssign { get; set; } = false;
    public string? Notes { get; set; }
}

public class AssignEventEquipmentRequest
{
    public string? Notes { get; set; }
}

public class HardwareAvailabilityDto
{
    public Guid HardwareId { get; set; }
    public string HardwareName { get; set; } = string.Empty;
    public string? HardwareSerialNumber { get; set; }
    public Guid HardwareTypeId { get; set; }
    public string HardwareTypeName { get; set; } = string.Empty;
    public HardwareStatus Status { get; set; }
    public bool IsAvailable { get; set; }
    public List<ConflictingEquipmentEventDto> ConflictingEvents { get; set; } = new();
}