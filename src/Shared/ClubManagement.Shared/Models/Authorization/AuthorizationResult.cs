namespace ClubManagement.Shared.Models.Authorization;

public class AuthorizationResult
{
    public bool Succeeded { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
    
    public static AuthorizationResult Success() => new() { Succeeded = true };
    
    public static AuthorizationResult Failed(params string[] reasons) => 
        new() { Succeeded = false, Reasons = reasons };
}