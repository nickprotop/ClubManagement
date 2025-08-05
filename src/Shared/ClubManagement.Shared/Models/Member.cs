namespace ClubManagement.Shared.Models;

public class Member : BaseEntity
{
    public Guid UserId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public MembershipTier Tier { get; set; } = MembershipTier.Basic;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? MembershipExpiresAt { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public decimal Balance { get; set; } = 0;
    public EmergencyContact? EmergencyContact { get; set; }
    public MedicalInfo? MedicalInfo { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    // Navigation properties
    public User User { get; set; } = null!;
}

public enum MembershipTier
{
    Basic,
    Premium,
    VIP,
    Family
}

public enum MembershipStatus
{
    Active,
    Expired,
    Suspended,
    Pending,
    Cancelled
}

public class EmergencyContact
{
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}

public class MedicalInfo
{
    public string Allergies { get; set; } = string.Empty;
    public string MedicalConditions { get; set; } = string.Empty;
    public string Medications { get; set; } = string.Empty;
    public string? Notes { get; set; }
}