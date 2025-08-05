using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Authorization;

public interface IEventAuthorizationService
{
    Task<EventPermissions> GetEventPermissionsAsync(Guid userId, Guid? eventId = null);
    Task<bool> CanPerformActionAsync(Guid userId, EventAction action, Guid? eventId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, EventAction action, Guid? eventId = null);
}