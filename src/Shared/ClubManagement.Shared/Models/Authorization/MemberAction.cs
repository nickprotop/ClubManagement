namespace ClubManagement.Shared.Models.Authorization;

public enum MemberAction
{
    // Basic CRUD
    View,               // View member list/search
    ViewDetails,        // View individual member details  
    Create,             // Create new members
    Edit,               // Edit member information
    Delete,             // Delete members
    
    // Status Management
    ChangeStatus,       // Activate/suspend/cancel memberships
    ExtendMembership,   // Extend membership dates
    ChangeTier,         // Upgrade/downgrade membership tiers
    
    // Profile Management  
    ViewOwnProfile,     // View own member profile
    EditOwnProfile,     // Edit own member profile
    ViewOtherProfiles,  // View other members' profiles
    EditOtherProfiles,  // Edit other members' profiles
    
    // Sensitive Data Access
    ViewMedicalInfo,    // Access medical information
    EditMedicalInfo,    // Modify medical information
    ViewEmergencyContact, // Access emergency contacts
    EditEmergencyContact, // Modify emergency contacts
    ViewFinancialInfo,  // View balances, payment history
    EditBalance,        // Modify member balances
    
    // Administrative
    ExportMemberData,   // Export member lists/reports
    ViewMemberActivity, // View member activity logs
    ViewAllMembers,     // View all members regardless of privacy settings
    ImpersonateMember,  // Login as member (Admin/SuperAdmin only)
    
    // Communication
    ViewContactInfo,    // View phone/email of other members
    SendNotifications,  // Send system notifications to members
    AccessMemberDirectory // View member directory with contact info
}