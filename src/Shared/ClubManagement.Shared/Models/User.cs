namespace ClubManagement.Shared.Models;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsActive { get; set; } = true;
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool EmailVerified { get; set; } = false;
    public DateTime? PasswordChangedAt { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    // Navigation properties
    public Member? Member { get; set; }
}

public enum UserRole
{
    Member,
    Staff,
    Instructor,
    Coach,
    Admin,
    SuperAdmin
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    PendingVerification
}