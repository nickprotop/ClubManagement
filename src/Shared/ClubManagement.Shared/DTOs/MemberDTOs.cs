using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class MemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    
    // Personal Information (flattened from User)
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    // Membership Information
    public MembershipTier Tier { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? MembershipExpiresAt { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public decimal Balance { get; set; }
    
    // Related Information
    public EmergencyContact? EmergencyContact { get; set; }
    public MedicalInfo? MedicalInfo { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    // Original User object for backward compatibility
    public UserProfileDto User { get; set; } = null!;
}

public class CreateMemberRequest
{
    // User Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    
    // Membership Information
    public MembershipTier Tier { get; set; } = MembershipTier.Basic;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime? MembershipExpiresAt { get; set; }
    
    // Emergency Contact
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    
    // Medical Information (optional)
    public string? Allergies { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Medications { get; set; }
    
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

public class UpdateMemberRequest
{
    // User Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    
    // Membership Information
    public MembershipTier Tier { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime? MembershipExpiresAt { get; set; }
    
    // Emergency Contact
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    
    // Medical Information (optional)
    public string? Allergies { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Medications { get; set; }
    
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

public class MemberListDto
{
    public Guid Id { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public MembershipTier Tier { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class MemberSearchRequest
{
    public string? SearchTerm { get; set; }
    public MembershipTier? Tier { get; set; }
    public MembershipStatus? Status { get; set; }
    public DateTime? JoinedAfter { get; set; }
    public DateTime? JoinedBefore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class MemberQuickSearchRequest
{
    public string SearchTerm { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 20;
    public bool IncludeInactive { get; set; } = false;
}

public class MemberSearchDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MembershipNumber { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayText => $"{FullName} ({MembershipNumber})";
    public MembershipStatus Status { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}