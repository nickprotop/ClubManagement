namespace ClubManagement.Shared.Models;

public class FacilityType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public PropertySchema PropertySchema { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    
    // Member Integration Properties
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    public List<string> RequiredCertifications { get; set; } = new();
    public bool RequiresSupervision { get; set; } = false;
    
    // Navigation properties
    public List<Facility> Facilities { get; set; } = new();
}