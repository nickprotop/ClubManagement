namespace ClubManagement.Shared.Models;

public class Hardware : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public Guid HardwareTypeId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public HardwareStatus Status { get; set; } = HardwareStatus.Available;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal? PurchasePrice { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public HardwareType HardwareType { get; set; } = null!;
    public List<HardwareAssignment> Assignments { get; set; } = new();
}

public enum HardwareStatus
{
    Available,
    Assigned,
    Maintenance,
    OutOfOrder,
    Lost,
    Retired
}