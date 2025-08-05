using ClubManagement.Shared.Models;

namespace ClubManagement.Domain.Entities;

public class MemberAuditLog
{
    public Guid Id { get; set; }
    public Guid PerformedBy { get; set; }
    public Guid? TargetMemberId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation properties
    public User PerformedByUser { get; set; } = null!;
    public Member? TargetMember { get; set; }
}