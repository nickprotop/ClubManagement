namespace ClubManagement.Shared.Models.Authorization;

public class ImpersonationResult
{
    public bool Succeeded { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
    public Guid? SessionId { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    public static ImpersonationResult Success(Guid sessionId, string token, DateTime expiresAt) => 
        new() 
        { 
            Succeeded = true, 
            SessionId = sessionId, 
            Token = token, 
            ExpiresAt = expiresAt 
        };
    
    public static ImpersonationResult Failed(params string[] reasons) => 
        new() { Succeeded = false, Reasons = reasons };
}