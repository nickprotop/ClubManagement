namespace ClubManagement.Shared.Models;

public class HardwareAssignment : BaseEntity
{
    public Guid HardwareId { get; set; }
    public Guid MemberId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }
    public Guid? ReturnedByUserId { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? Notes { get; set; }
    public string? ReturnNotes { get; set; }
    public decimal? LateFee { get; set; }
    public decimal? DamageFee { get; set; }
    
    // Navigation properties
    public Hardware Hardware { get; set; } = null!;
    public Member Member { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
    public User? ReturnedByUser { get; set; }
}

public enum AssignmentStatus
{
    Active,
    Returned,
    Overdue,
    Lost,
    Damaged
}