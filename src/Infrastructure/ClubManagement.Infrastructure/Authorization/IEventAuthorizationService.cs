using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Infrastructure.Data;

namespace ClubManagement.Infrastructure.Authorization;

public interface IEventAuthorizationService
{
    Task<EventPermissions> GetEventPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? eventId = null);
    Task<bool> CanPerformActionAsync(Guid userId, EventAction action, ClubManagementDbContext tenantContext, Guid? eventId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, EventAction action, ClubManagementDbContext tenantContext, Guid? eventId = null);
}