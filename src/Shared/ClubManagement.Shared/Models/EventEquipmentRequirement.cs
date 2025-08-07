namespace ClubManagement.Shared.Models;

public class EventEquipmentRequirement : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid? HardwareTypeId { get; set; }        // Link to specific hardware type
    public Guid? SpecificHardwareId { get; set; }    // Link to specific hardware item
    public string Description { get; set; } = string.Empty;  // Fallback/custom description
    public int Quantity { get; set; } = 1;           // Required quantity
    public bool IsMandatory { get; set; } = true;    // vs. recommended/optional
    public string? Notes { get; set; }               // Additional requirements/notes
    
    // Auto-assignment settings
    public bool AutoAssign { get; set; } = false;   // Automatically assign on event creation
    public HardwareCondition? MinimumCondition { get; set; }
    
    // Navigation properties
    public Event Event { get; set; } = null!;
    public HardwareType? HardwareType { get; set; }
    public Hardware? SpecificHardware { get; set; }
    public List<EventEquipmentAssignment> Assignments { get; set; } = new();
}

public class EventEquipmentAssignment : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid RequirementId { get; set; }
    public Guid HardwareId { get; set; }
    public Guid? MemberId { get; set; }              // Member responsible for this equipment
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedOutAt { get; set; }      // When equipment was physically checked out
    public DateTime? ReturnedAt { get; set; }        // When equipment was returned
    public string? Notes { get; set; }
    public EventEquipmentAssignmentStatus Status { get; set; } = EventEquipmentAssignmentStatus.Reserved;
    
    // Navigation properties
    public Event Event { get; set; } = null!;
    public EventEquipmentRequirement Requirement { get; set; } = null!;
    public Hardware Hardware { get; set; } = null!;
    public Member? Member { get; set; }
}

public enum HardwareCondition
{
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}

public enum EventEquipmentAssignmentStatus
{
    Reserved,      // Equipment reserved for event but not checked out
    CheckedOut,    // Equipment checked out to member
    InUse,         // Equipment currently being used in event
    Returned,      // Equipment returned in good condition
    Missing,       // Equipment not returned or lost
    Damaged        // Equipment returned damaged
}