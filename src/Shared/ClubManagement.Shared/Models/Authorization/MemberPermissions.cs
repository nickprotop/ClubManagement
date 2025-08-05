namespace ClubManagement.Shared.Models.Authorization;

public class MemberPermissions
{
    // Basic Operations
    public bool CanView { get; set; }
    public bool CanViewDetails { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    
    // Status Management
    public bool CanChangeStatus { get; set; }
    public bool CanExtendMembership { get; set; }
    public bool CanChangeTier { get; set; }
    
    // Profile Access
    public bool CanViewOwnProfile { get; set; }
    public bool CanEditOwnProfile { get; set; }
    public bool CanViewOtherProfiles { get; set; }
    public bool CanEditOtherProfiles { get; set; }
    
    // Sensitive Data
    public bool CanViewMedicalInfo { get; set; }
    public bool CanEditMedicalInfo { get; set; }
    public bool CanViewEmergencyContact { get; set; }
    public bool CanEditEmergencyContact { get; set; }
    public bool CanViewFinancialInfo { get; set; }
    public bool CanEditBalance { get; set; }
    
    // Administrative
    public bool CanExportMemberData { get; set; }
    public bool CanViewMemberActivity { get; set; }
    public bool CanViewAllMembers { get; set; }
    public bool CanImpersonateMember { get; set; }
    
    // Communication
    public bool CanViewContactInfo { get; set; }
    public bool CanSendNotifications { get; set; }
    public bool CanAccessMemberDirectory { get; set; }
    
    // Context Information
    public string[] Restrictions { get; set; } = Array.Empty<string>();
    public string[] ReasonsDenied { get; set; } = Array.Empty<string>();
    public bool IsViewingOwnData { get; set; }
    public MembershipTier? RequiredTierForAction { get; set; }
}