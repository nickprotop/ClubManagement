using ClubManagement.Shared.Models;

namespace ClubManagement.Domain.Entities;

public class ImpersonationSession : BaseEntity
{
    public Guid AdminUserId { get; set; }
    public Guid TargetMemberId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public List<string> ActionsPerformed { get; set; } = new();
    
    // Navigation properties
    public User AdminUser { get; set; } = null!;
    public Member TargetMember { get; set; } = null!;
    
    // Helper methods
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public TimeSpan Duration => (EndedAt ?? DateTime.UtcNow) - StartedAt;
    
    public void AddAction(string action)
    {
        ActionsPerformed.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {action}");
    }
    
    public void End()
    {
        EndedAt = DateTime.UtcNow;
        IsActive = false;
    }
}