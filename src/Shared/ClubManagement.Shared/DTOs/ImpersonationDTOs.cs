namespace ClubManagement.Shared.DTOs;

public class StartImpersonationRequest
{
    public Guid TargetMemberId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30; // Default 30 minutes
}

public class ImpersonationStatusDto
{
    public bool IsImpersonating { get; set; }
    public Guid? SessionId { get; set; }
    public string? TargetMemberName { get; set; }
    public Guid? TargetMemberId { get; set; }
    public string? AdminName { get; set; }
    public Guid? AdminUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
    public TimeSpan? RemainingTime => ExpiresAt?.Subtract(DateTime.UtcNow);
}

public class ImpersonationSessionDto
{
    public Guid Id { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string TargetMemberName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> ActionsPerformed { get; set; } = new();
}

public class EndImpersonationRequest
{
    public string? Reason { get; set; }
}