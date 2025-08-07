using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Authorization;

public class EventAuthorizationService : IEventAuthorizationService
{

    public async Task<EventPermissions> GetEventPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? eventId = null)
    {
        var user = await GetUserWithRoleAsync(userId, tenantContext);
        if (user == null)
            return new EventPermissions();

        var eventEntity = eventId.HasValue ? await GetEventAsync(eventId.Value, tenantContext) : null;
        
        return user.Role switch
        {
            UserRole.Member => await GetMemberPermissionsAsync(user, eventEntity, tenantContext),
            UserRole.Staff => await GetStaffPermissionsAsync(user, eventEntity, tenantContext),
            UserRole.Instructor => await GetInstructorPermissionsAsync(user, eventEntity, tenantContext),
            UserRole.Coach => await GetCoachPermissionsAsync(user, eventEntity, tenantContext),
            UserRole.Admin => await GetAdminPermissionsAsync(user, eventEntity, tenantContext),
            UserRole.SuperAdmin => await GetSuperAdminPermissionsAsync(user, eventEntity, tenantContext),
            _ => new EventPermissions()
        };
    }

    public async Task<bool> CanPerformActionAsync(Guid userId, EventAction action, ClubManagementDbContext tenantContext, Guid? eventId = null)
    {
        var permissions = await GetEventPermissionsAsync(userId, tenantContext, eventId);
        
        return action switch
        {
            EventAction.View => permissions.CanView,
            EventAction.Create => permissions.CanCreate,
            EventAction.Edit => permissions.CanEdit,
            EventAction.Delete => permissions.CanDelete,
            EventAction.RegisterSelf => permissions.CanRegisterSelf,
            EventAction.RegisterOthers => permissions.CanRegisterOthers,
            EventAction.CheckInSelf => permissions.CanCheckInSelf,
            EventAction.CheckInOthers => permissions.CanCheckInOthers,
            EventAction.ViewRegistrations => permissions.CanViewRegistrations,
            EventAction.ModifyRegistrations => permissions.CanModifyRegistrations,
            EventAction.CancelEvent => permissions.CanCancelEvent,
            EventAction.RescheduleEvent => permissions.CanRescheduleEvent,
            EventAction.ManageEventStatus => permissions.CanManageEventStatus,
            _ => false
        };
    }

    public async Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, EventAction action, ClubManagementDbContext tenantContext, Guid? eventId = null)
    {
        var user = await GetUserWithRoleAsync(userId, tenantContext);
        if (user == null)
            return AuthorizationResult.Failed("User not found");

        var eventEntity = eventId.HasValue ? await GetEventAsync(eventId.Value, tenantContext) : null;
        
        // Check basic permissions
        var canPerform = await CanPerformActionAsync(userId, action, tenantContext, eventId);
        if (!canPerform)
        {
            var permissions = await GetEventPermissionsAsync(userId, tenantContext, eventId);
            var reasons = permissions.ReasonsDenied.Any() 
                ? permissions.ReasonsDenied 
                : new[] { $"User does not have permission to {action}" };
            return AuthorizationResult.Failed(reasons);
        }

        // Additional context-specific checks
        if (eventEntity != null)
        {
            var contextCheck = await CheckEventContextAsync(user, eventEntity, action, tenantContext);
            if (!contextCheck.Succeeded)
                return contextCheck;
        }

        return AuthorizationResult.Success();
    }

    private async Task<EventPermissions> GetMemberPermissionsAsync(User user, Event? eventEntity, ClubManagementDbContext tenantContext)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
        var isRegistered = eventEntity != null && member != null ? 
            await IsUserRegisteredForEventAsync(member.Id, eventEntity.Id, tenantContext) : false;
        
        var restrictions = new List<string>();
        var reasons = new List<string>();

        if (member == null)
        {
            reasons.Add("User is not a registered member");
            return new EventPermissions { ReasonsDenied = reasons.ToArray() };
        }

        if (member.Status != MembershipStatus.Active)
        {
            reasons.Add("Membership is not active");
            restrictions.Add("Inactive membership");
        }

        var canRegisterSelf = eventEntity == null || 
            (!isRegistered && IsRegistrationOpen(eventEntity) && member.Status == MembershipStatus.Active);
        
        var canCheckInSelf = eventEntity != null && isRegistered && IsCheckInAllowed(eventEntity);

        return new EventPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanRegisterSelf = canRegisterSelf,
            CanRegisterOthers = false,
            CanCheckInSelf = canCheckInSelf,
            CanCheckInOthers = false,
            CanViewRegistrations = isRegistered,
            CanModifyRegistrations = false,
            CanCancelEvent = false,
            CanRescheduleEvent = false,
            CanManageEventStatus = false,
            CanBulkCheckIn = false,
            Restrictions = restrictions.ToArray(),
            ReasonsDenied = reasons.ToArray()
        };
    }

    private async Task<EventPermissions> GetStaffPermissionsAsync(User user, Event? eventEntity, ClubManagementDbContext tenantContext)
    {
        var restrictions = new List<string>();
        var canModifyEvent = eventEntity == null || (!IsEventStarted(eventEntity) && !IsCriticalEvent(eventEntity));
        var canDeleteEvent = eventEntity == null || (!HasConfirmedRegistrations(eventEntity) && !IsEventStarted(eventEntity));

        if (eventEntity != null && IsCriticalEvent(eventEntity))
            restrictions.Add("Critical events require admin approval for modifications");

        return new EventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = canModifyEvent,
            CanDelete = canDeleteEvent,
            CanRegisterSelf = true,
            CanRegisterOthers = true,
            CanCheckInSelf = true,
            CanCheckInOthers = true,
            CanViewRegistrations = true,
            CanModifyRegistrations = true,
            CanCancelEvent = canModifyEvent,
            CanRescheduleEvent = canModifyEvent,
            CanManageEventStatus = true,
            CanBulkCheckIn = true,
            Restrictions = restrictions.ToArray()
        };
    }

    private async Task<EventPermissions> GetInstructorPermissionsAsync(User user, Event? eventEntity, ClubManagementDbContext tenantContext)
    {
        var isOwnEvent = eventEntity?.InstructorId == user.Id;
        var canModifyEvent = eventEntity == null || (isOwnEvent && !IsEventStarted(eventEntity));
        var canDeleteEvent = eventEntity == null || (isOwnEvent && !HasConfirmedRegistrations(eventEntity) && !IsEventStarted(eventEntity));
        
        var restrictions = new List<string>();
        if (eventEntity != null && !isOwnEvent)
            restrictions.Add("Can only modify events you are instructing");

        return new EventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = canModifyEvent,
            CanDelete = canDeleteEvent,
            CanRegisterSelf = true,
            CanRegisterOthers = isOwnEvent,
            CanCheckInSelf = true,
            CanCheckInOthers = isOwnEvent,
            CanViewRegistrations = isOwnEvent,
            CanModifyRegistrations = isOwnEvent,
            CanCancelEvent = canModifyEvent,
            CanRescheduleEvent = canModifyEvent,
            CanManageEventStatus = isOwnEvent,
            CanBulkCheckIn = isOwnEvent,
            Restrictions = restrictions.ToArray()
        };
    }

    private async Task<EventPermissions> GetCoachPermissionsAsync(User user, Event? eventEntity, ClubManagementDbContext tenantContext)
    {
        var isOwnEvent = eventEntity?.InstructorId == user.Id;
        var canModifyEvent = eventEntity == null || (isOwnEvent && !IsEventStarted(eventEntity));
        var canDeleteEvent = eventEntity == null || (isOwnEvent && !HasConfirmedRegistrations(eventEntity) && !IsEventStarted(eventEntity));
        
        var restrictions = new List<string>();
        if (eventEntity != null && !isOwnEvent)
            restrictions.Add("Can only modify events you are coaching");

        return new EventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = canModifyEvent,
            CanDelete = canDeleteEvent,
            CanRegisterSelf = true,
            CanRegisterOthers = isOwnEvent,
            CanCheckInSelf = true,
            CanCheckInOthers = isOwnEvent,
            CanViewRegistrations = isOwnEvent,
            CanModifyRegistrations = isOwnEvent,
            CanCancelEvent = canModifyEvent,
            CanRescheduleEvent = canModifyEvent,
            CanManageEventStatus = isOwnEvent,
            CanBulkCheckIn = isOwnEvent,
            Restrictions = restrictions.ToArray()
        };
    }

    private async Task<EventPermissions> GetAdminPermissionsAsync(User user, Event? eventEntity, ClubManagementDbContext tenantContext)
    {
        return new EventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanRegisterSelf = true,
            CanRegisterOthers = true,
            CanCheckInSelf = true,
            CanCheckInOthers = true,
            CanViewRegistrations = true,
            CanModifyRegistrations = true,
            CanCancelEvent = true,
            CanRescheduleEvent = true,
            CanManageEventStatus = true,
            CanBulkCheckIn = true
        };
    }

    private async Task<EventPermissions> GetSuperAdminPermissionsAsync(User user, Event? eventEntity, ClubManagementDbContext tenantContext)
    {
        return new EventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanRegisterSelf = true,
            CanRegisterOthers = true,
            CanCheckInSelf = true,
            CanCheckInOthers = true,
            CanViewRegistrations = true,
            CanModifyRegistrations = true,
            CanCancelEvent = true,
            CanRescheduleEvent = true,
            CanManageEventStatus = true,
            CanBulkCheckIn = true
        };
    }

    private async Task<AuthorizationResult> CheckEventContextAsync(User user, Event eventEntity, EventAction action, ClubManagementDbContext tenantContext)
    {
        var reasons = new List<string>();

        // Check if event has started for modification actions
        if (action is EventAction.Edit or EventAction.Delete or EventAction.RescheduleEvent)
        {
            if (IsEventStarted(eventEntity))
                reasons.Add("Cannot modify events that have already started");
        }

        // Check registration timing
        if (action == EventAction.RegisterSelf || action == EventAction.RegisterOthers)
        {
            if (!IsRegistrationOpen(eventEntity))
                reasons.Add("Registration is closed for this event");
                
            if (eventEntity.Status == EventStatus.Cancelled)
                reasons.Add("Cannot register for cancelled events");
        }

        // Check check-in timing
        if (action is EventAction.CheckInSelf or EventAction.CheckInOthers)
        {
            if (!IsCheckInAllowed(eventEntity))
                reasons.Add("Check-in is not available at this time");
        }

        // Check deletion constraints
        if (action == EventAction.Delete)
        {
            if (HasConfirmedRegistrations(eventEntity))
                reasons.Add("Cannot delete events with confirmed registrations");
        }

        return reasons.Any() ? AuthorizationResult.Failed(reasons.ToArray()) : AuthorizationResult.Success();
    }

    private async Task<User?> GetUserWithRoleAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<Event?> GetEventAsync(Guid eventId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    private async Task<bool> IsUserRegisteredForEventAsync(Guid memberId, Guid eventId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.EventRegistrations
            .AnyAsync(r => r.MemberId == memberId && r.EventId == eventId && 
                          r.Status == RegistrationStatus.Confirmed);
    }

    private static bool IsEventStarted(Event eventEntity)
    {
        return eventEntity.StartDateTime <= DateTime.UtcNow;
    }

    private static bool IsRegistrationOpen(Event eventEntity)
    {
        if (eventEntity.RegistrationDeadline.HasValue)
            return DateTime.UtcNow <= eventEntity.RegistrationDeadline;
        
        // Default: allow registration until event starts
        return DateTime.UtcNow < eventEntity.StartDateTime;
    }

    private static bool IsCheckInAllowed(Event eventEntity)
    {
        var now = DateTime.UtcNow;
        var checkInStart = eventEntity.StartDateTime.AddHours(-1); // 1 hour before
        var checkInEnd = eventEntity.StartDateTime.AddMinutes(30); // 30 minutes after start
        
        return now >= checkInStart && now <= checkInEnd;
    }

    private static bool HasConfirmedRegistrations(Event eventEntity)
    {
        return eventEntity.Registrations?.Any(r => r.Status == RegistrationStatus.Confirmed) ?? false;
    }

    private static bool IsCriticalEvent(Event eventEntity)
    {
        return eventEntity.Type == EventType.Tournament || 
               eventEntity.MaxCapacity > 50 || 
               (eventEntity.Price ?? 0) > 100;
    }
}