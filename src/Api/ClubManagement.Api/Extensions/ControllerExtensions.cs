using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagement.Api.Extensions;

public static class ControllerExtensions
{
    public static Guid GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    public static string GetCurrentUserEmail(this ControllerBase controller)
    {
        return controller.User.FindFirst(ClaimTypes.Email)?.Value ?? 
               throw new UnauthorizedAccessException("User email not found in token");
    }

    public static string GetCurrentUserRole(this ControllerBase controller)
    {
        return controller.User.FindFirst(ClaimTypes.Role)?.Value ?? 
               throw new UnauthorizedAccessException("User role not found in token");
    }

    public static Guid GetCurrentTenantId(this ControllerBase controller)
    {
        var tenantIdClaim = controller.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant ID not found in token");
        }
        return tenantId;
    }

    /// <summary>
    /// Gets the original admin user ID if currently impersonating, otherwise returns current user ID
    /// </summary>
    public static Guid GetOriginalUserId(this ControllerBase controller)
    {
        var originalAdminIdClaim = controller.User.FindFirst("original_admin_id")?.Value;
        if (!string.IsNullOrEmpty(originalAdminIdClaim) && Guid.TryParse(originalAdminIdClaim, out var originalAdminId))
        {
            return originalAdminId;
        }
        return controller.GetCurrentUserId();
    }

    /// <summary>
    /// Checks if the current request is from an impersonation session
    /// </summary>
    public static bool IsImpersonating(this ControllerBase controller)
    {
        return controller.User.FindFirst("is_impersonating")?.Value == "true";
    }

    /// <summary>
    /// Gets the impersonation session ID if currently impersonating
    /// </summary>
    public static Guid? GetImpersonationSessionId(this ControllerBase controller)
    {
        var sessionIdClaim = controller.User.FindFirst("impersonation_session_id")?.Value;
        if (!string.IsNullOrEmpty(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            return sessionId;
        }
        return null;
    }

    /// <summary>
    /// Gets when the impersonation session started (Unix timestamp)
    /// </summary>
    public static DateTime? GetImpersonationStartedAt(this ControllerBase controller)
    {
        var startedAtClaim = controller.User.FindFirst("impersonation_started_at")?.Value;
        if (!string.IsNullOrEmpty(startedAtClaim) && long.TryParse(startedAtClaim, out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }
        return null;
    }
}