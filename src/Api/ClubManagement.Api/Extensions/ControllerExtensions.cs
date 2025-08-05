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
}