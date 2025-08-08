namespace ClubManagement.Shared.Models;

public class MemberFacilityCertification : BaseEntity
{
    public Guid MemberId { get; set; }
    public string CertificationType { get; set; } = string.Empty;
    public DateTime CertifiedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid CertifiedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public User CertifiedByUser { get; set; } = null!;
}