using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Authorization;

public class FacilityAuthorizationService : IFacilityAuthorizationService
{
    public async Task<FacilityPermissions> GetPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        var user = await GetUserWithMemberAsync(userId, tenantContext);
        if (user == null) return new FacilityPermissions();

        var facility = facilityId.HasValue ? await GetFacilityAsync(facilityId.Value, tenantContext) : null;
        
        return user.Role switch
        {
            UserRole.Member => await GetMemberPermissionsAsync(user, facility, tenantContext),
            UserRole.Staff => GetStaffPermissions(user, facility),
            UserRole.Instructor => GetInstructorPermissions(user, facility),
            UserRole.Coach => GetCoachPermissions(user, facility),
            UserRole.Admin => GetAdminPermissions(user, facility),
            UserRole.SuperAdmin => GetSuperAdminPermissions(user, facility),
            _ => new FacilityPermissions()
        };
    }

    public async Task<bool> CanPerformActionAsync(Guid userId, FacilityAction action, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        var result = await CheckAuthorizationAsync(userId, action, tenantContext, facilityId);
        return result.Succeeded;
    }

    public async Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, FacilityAction action, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        var permissions = await GetPermissionsAsync(userId, tenantContext, facilityId);
        
        var canPerform = action switch
        {
            FacilityAction.View => permissions.CanView,
            FacilityAction.ViewDetails => permissions.CanViewDetails,
            FacilityAction.Create => permissions.CanCreate,
            FacilityAction.Edit => permissions.CanEdit,
            FacilityAction.Delete => permissions.CanDelete,
            FacilityAction.Book => permissions.CanBook,
            FacilityAction.BookForSelf => permissions.CanBookForSelf,
            FacilityAction.CancelBooking => permissions.CanCancelBookings,
            FacilityAction.CancelOwnBooking => permissions.CanCancelOwnBookings,
            FacilityAction.ModifyBooking => permissions.CanModifyBookings,
            FacilityAction.ModifyOwnBooking => permissions.CanModifyOwnBookings,
            FacilityAction.ViewAllBookings => permissions.CanViewAllBookings,
            FacilityAction.ViewOwnBookings => permissions.CanViewOwnBookings,
            FacilityAction.CheckInMembers => permissions.CanCheckInMembers,
            FacilityAction.CheckOutMembers => permissions.CanCheckOutMembers,
            FacilityAction.MarkMaintenance => permissions.CanMarkMaintenance,
            FacilityAction.MarkOutOfOrder => permissions.CanMarkOutOfOrder,
            FacilityAction.Retire => permissions.CanRetire,
            FacilityAction.Reactivate => permissions.CanReactivate,
            FacilityAction.ChangeLocation => permissions.CanChangeLocation,
            FacilityAction.CreateType => permissions.CanCreateTypes,
            FacilityAction.EditType => permissions.CanEditTypes,
            FacilityAction.DeleteType => permissions.CanDeleteTypes,
            FacilityAction.ManagePropertySchema => permissions.CanManagePropertySchemas,
            FacilityAction.ViewFinancials => permissions.CanViewFinancials,
            FacilityAction.SetRates => permissions.CanSetRates,
            FacilityAction.ProcessPayments => permissions.CanProcessPayments,
            FacilityAction.WaiveFees => permissions.CanWaiveFees,
            FacilityAction.ViewCalendar => permissions.CanViewCalendar,
            FacilityAction.ManageSchedule => permissions.CanManageSchedule,
            FacilityAction.BlockTime => permissions.CanBlockTime,
            FacilityAction.OverrideCapacity => permissions.CanOverrideCapacity,
            FacilityAction.ViewReports => permissions.CanViewReports,
            FacilityAction.GenerateReports => permissions.CanGenerateReports,
            FacilityAction.ExportData => permissions.CanExportData,
            FacilityAction.ViewAnalytics => permissions.CanViewAnalytics,
            FacilityAction.AssignToEvents => permissions.CanAssignToEvents,
            FacilityAction.ViewEventBookings => permissions.CanViewEventBookings,
            FacilityAction.ManageEventFacilities => permissions.CanManageEventFacilities,
            FacilityAction.AccessMemberPortal => permissions.CanAccessMemberPortal,
            FacilityAction.GrantTierAccess => permissions.CanGrantTierAccess,
            FacilityAction.RequireCertification => permissions.CanRequireCertification,
            FacilityAction.ViewMemberUsage => permissions.CanViewMemberUsage,
            FacilityAction.OverrideBookingLimits => permissions.CanOverrideBookingLimits,
            _ => false
        };

        return canPerform 
            ? AuthorizationResult.Success() 
            : AuthorizationResult.Failed(permissions.ReasonsDenied);
    }

    public async Task<bool> CanViewFacilitiesAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        return await CanPerformActionAsync(userId, FacilityAction.View, tenantContext, facilityId);
    }

    public async Task<bool> CanManageFacilitiesAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        var permissions = await GetPermissionsAsync(userId, tenantContext, facilityId);
        return permissions.CanCreate || permissions.CanEdit || permissions.CanDelete;
    }

    public async Task<bool> CanBookFacilityAsync(Guid userId, ClubManagementDbContext tenantContext, Guid facilityId)
    {
        return await CanPerformActionAsync(userId, FacilityAction.Book, tenantContext, facilityId);
    }

    public async Task<bool> CanManageFacilityTypesAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await CanPerformActionAsync(userId, FacilityAction.CreateType, tenantContext);
    }

    public async Task<bool> CanViewFinancialsAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await CanPerformActionAsync(userId, FacilityAction.ViewFinancials, tenantContext);
    }

    public async Task<bool> CanAccessMemberPortalAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await CanPerformActionAsync(userId, FacilityAction.AccessMemberPortal, tenantContext);
    }

    public async Task<bool> CanViewBookingsAsync(Guid userId, ClubManagementDbContext tenantContext, bool ownOnly = false)
    {
        var action = ownOnly ? FacilityAction.ViewOwnBookings : FacilityAction.ViewAllBookings;
        return await CanPerformActionAsync(userId, action, tenantContext);
    }

    public async Task<bool> CanCheckInMembersAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        return await CanPerformActionAsync(userId, FacilityAction.CheckInMembers, tenantContext, facilityId);
    }

    public async Task<bool> CanManageEventFacilitiesAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? eventId = null)
    {
        return await CanPerformActionAsync(userId, FacilityAction.ManageEventFacilities, tenantContext);
    }

    public async Task<bool> CanMemberAccessFacilityAsync(Guid userId, Guid facilityId, ClubManagementDbContext tenantContext)
    {
        var user = await GetUserWithMemberAsync(userId, tenantContext);
        if (user?.Member == null) return false;

        var facility = await GetFacilityWithRequirementsAsync(facilityId, tenantContext);
        if (facility == null) return false;

        // Check membership tier access
        if (facility.AllowedMembershipTiers.Any() && !facility.AllowedMembershipTiers.Contains(user.Member.Tier))
            return false;

        // Check required certifications
        var memberCertifications = await GetActiveMemberCertificationsAsync(userId, tenantContext);
        var hasRequiredCerts = facility.RequiredCertifications
            .All(req => memberCertifications.Any(cert => cert.CertificationType == req));

        return hasRequiredCerts;
    }

    public async Task<FacilityPermissions> GetMemberFacilityPermissionsAsync(Guid userId, ClubManagementDbContext tenantContext, Guid? facilityId = null)
    {
        var user = await GetUserWithMemberAsync(userId, tenantContext);
        if (user?.Member == null) return new FacilityPermissions();

        var facility = facilityId.HasValue ? await GetFacilityAsync(facilityId.Value, tenantContext) : null;
        return await GetMemberPermissionsAsync(user, facility, tenantContext);
    }

    private async Task<User?> GetUserWithMemberAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Users
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<Facility?> GetFacilityAsync(Guid facilityId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Facilities
            .Include(f => f.FacilityType)
            .Include(f => f.Bookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
            .FirstOrDefaultAsync(f => f.Id == facilityId);
    }

    private async Task<Facility?> GetFacilityWithRequirementsAsync(Guid facilityId, ClubManagementDbContext tenantContext)
    {
        return await tenantContext.Facilities
            .FirstOrDefaultAsync(f => f.Id == facilityId);
    }

    private async Task<List<MemberFacilityCertification>> GetActiveMemberCertificationsAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Member == null) return new List<MemberFacilityCertification>();

        return await tenantContext.MemberFacilityCertifications
            .Where(c => c.MemberId == user.Member.Id && c.IsActive && 
                       (!c.ExpiryDate.HasValue || c.ExpiryDate > DateTime.UtcNow))
            .ToListAsync();
    }

    private async Task<FacilityPermissions> GetMemberPermissionsAsync(User user, Facility? facility, ClubManagementDbContext tenantContext)
    {
        if (user.Member == null) return new FacilityPermissions();

        var memberBookings = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == user.Member.Id && 
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
            .CountAsync();

        var bookingLimits = GetMemberBookingLimits(user.Member.Tier);
        var accessibleFacilityTypes = await GetAccessibleFacilityTypesAsync(user.Member.Tier, tenantContext);
        
        var canAccessFacility = facility == null || await CanMemberAccessFacilityAsync(user.Id, facility.Id, tenantContext);
        var missingCertifications = facility != null ? 
            await GetMissingCertificationsAsync(user.Id, facility.Id, tenantContext) : 
            new List<string>();

        return new FacilityPermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanBookForSelf = canAccessFacility && memberBookings < bookingLimits.MaxConcurrent,
            CanViewOwnBookings = true,
            CanCancelOwnBookings = true,
            CanModifyOwnBookings = user.Member.Tier >= MembershipTier.Premium,
            CanAccessMemberPortal = true,
            CanViewCalendar = true,

            // Member-specific restrictions
            AccessibleFacilityTypes = accessibleFacilityTypes,
            MaxConcurrentBookings = bookingLimits.MaxConcurrent,
            MaxBookingDurationMinutes = bookingLimits.MaxDuration,
            MaxAdvanceBookingDays = bookingLimits.MaxAdvanceDays,
            BookingTimeWindowStart = bookingLimits.TimeWindowStart,
            BookingTimeWindowEnd = bookingLimits.TimeWindowEnd,

            // Certification info
            RequiredCertifications = facility?.RequiredCertifications ?? new List<string>(),
            MissingCertifications = missingCertifications,

            // Context information
            MembershipTier = user.Member.Tier,
            CurrentBookingCount = memberBookings,
            HasActiveBooking = memberBookings > 0,
            Restrictions = GetMemberRestrictions(user.Member, facility, canAccessFacility, missingCertifications)
        };
    }

    private FacilityPermissions GetStaffPermissions(User user, Facility? facility)
    {
        return new FacilityPermissions
        {
            // Basic Operations
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            
            // Booking Operations
            CanBook = true,
            CanBookForOthers = true,
            CanCancelBookings = true,
            CanModifyBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            
            // Facility Operations
            CanCheckInMembers = true,
            CanCheckOutMembers = true,
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanChangeLocation = true,
            
            // Calendar & Scheduling
            CanViewCalendar = true,
            CanManageSchedule = true,
            
            // Member Portal Access
            CanAccessMemberPortal = true,
            
            // Access scope
            CanViewAll = true,
            
            Restrictions = new[] { "Cannot delete facilities", "Cannot manage types", "Limited financial access" }
        };
    }

    private FacilityPermissions GetInstructorPermissions(User user, Facility? facility)
    {
        return new FacilityPermissions
        {
            // View and basic operations
            CanView = true,
            CanViewDetails = true,
            CanCreate = false,
            CanEdit = false,
            
            // Limited booking capabilities for classes
            CanBook = true,
            CanBookForOthers = true, // For their classes
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanCancelOwnBookings = true,
            
            // Check-in operations for their classes
            CanCheckInMembers = true,
            CanCheckOutMembers = true,
            
            // Calendar access
            CanViewCalendar = true,
            
            // Member portal access
            CanAccessMemberPortal = true,
            
            CanViewAll = false,
            CanViewOwn = true,
            
            Restrictions = new[] { "Limited to facilities for own classes", "Cannot create or delete facilities" }
        };
    }

    private FacilityPermissions GetCoachPermissions(User user, Facility? facility)
    {
        return new FacilityPermissions
        {
            // Enhanced permissions for coaches
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            
            // Full booking capabilities
            CanBook = true,
            CanBookForOthers = true,
            CanCancelBookings = true,
            CanModifyBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanOverrideBookingLimits = true,
            
            // Facility operations
            CanCheckInMembers = true,
            CanCheckOutMembers = true,
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanChangeLocation = true,
            
            // Calendar & Scheduling
            CanViewCalendar = true,
            CanManageSchedule = true,
            CanBlockTime = true,
            
            // Member portal access
            CanAccessMemberPortal = true,
            
            // Event integration
            CanAssignToEvents = true,
            CanViewEventBookings = true,
            CanManageEventFacilities = true,
            
            CanViewAll = true,
            
            Restrictions = new[] { "Cannot delete facilities", "Cannot manage facility types", "Limited financial access" }
        };
    }

    private FacilityPermissions GetAdminPermissions(User user, Facility? facility)
    {
        return new FacilityPermissions
        {
            // Full CRUD permissions
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            
            // Full booking management
            CanBook = true,
            CanBookForOthers = true,
            CanCancelBookings = true,
            CanModifyBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanOverrideBookingLimits = true,
            
            // Facility operations
            CanCheckInMembers = true,
            CanCheckOutMembers = true,
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanRetire = true,
            CanReactivate = true,
            CanChangeLocation = true,
            
            // Type management
            CanCreateTypes = true,
            CanEditTypes = true,
            CanDeleteTypes = true,
            CanManagePropertySchemas = true,
            
            // Financial operations
            CanViewFinancials = true,
            CanSetRates = true,
            CanProcessPayments = true,
            CanWaiveFees = true,
            
            // Calendar & Scheduling
            CanViewCalendar = true,
            CanManageSchedule = true,
            CanBlockTime = true,
            CanOverrideCapacity = true,
            
            // Reporting
            CanViewReports = true,
            CanGenerateReports = true,
            CanExportData = true,
            CanViewAnalytics = true,
            
            // Event integration
            CanAssignToEvents = true,
            CanViewEventBookings = true,
            CanManageEventFacilities = true,
            
            // Member integration
            CanAccessMemberPortal = true,
            CanGrantTierAccess = true,
            CanRequireCertification = true,
            CanViewMemberUsage = true,
            CanBypassCertificationRequirements = true,
            
            // Access scope
            CanViewAll = true,
            CanViewOwn = true
        };
    }

    private FacilityPermissions GetSuperAdminPermissions(User user, Facility? facility)
    {
        // SuperAdmin has all permissions
        return new FacilityPermissions
        {
            CanView = true,
            CanViewDetails = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanBook = true,
            CanBookForSelf = true,
            CanBookForOthers = true,
            CanCancelBookings = true,
            CanCancelOwnBookings = true,
            CanModifyBookings = true,
            CanModifyOwnBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanOverrideBookingLimits = true,
            CanCheckInMembers = true,
            CanCheckOutMembers = true,
            CanMarkMaintenance = true,
            CanMarkOutOfOrder = true,
            CanRetire = true,
            CanReactivate = true,
            CanChangeLocation = true,
            CanCreateTypes = true,
            CanEditTypes = true,
            CanDeleteTypes = true,
            CanManagePropertySchemas = true,
            CanViewFinancials = true,
            CanSetRates = true,
            CanProcessPayments = true,
            CanWaiveFees = true,
            CanViewCalendar = true,
            CanManageSchedule = true,
            CanBlockTime = true,
            CanOverrideCapacity = true,
            CanViewReports = true,
            CanGenerateReports = true,
            CanExportData = true,
            CanViewAnalytics = true,
            CanAssignToEvents = true,
            CanViewEventBookings = true,
            CanManageEventFacilities = true,
            CanAccessMemberPortal = true,
            CanGrantTierAccess = true,
            CanRequireCertification = true,
            CanViewMemberUsage = true,
            CanBypassCertificationRequirements = true,
            CanViewAll = true,
            CanViewOwn = true
        };
    }

    private BookingLimits GetMemberBookingLimits(MembershipTier tier)
    {
        return tier switch
        {
            MembershipTier.Basic => new BookingLimits 
            { 
                MaxConcurrent = 1, 
                MaxDuration = 60, 
                MaxAdvanceDays = 3,
                TimeWindowStart = new TimeSpan(6, 0, 0),  // 6 AM
                TimeWindowEnd = new TimeSpan(22, 0, 0)    // 10 PM
            },
            MembershipTier.Premium => new BookingLimits 
            { 
                MaxConcurrent = 3, 
                MaxDuration = 120, 
                MaxAdvanceDays = 7,
                TimeWindowStart = new TimeSpan(5, 0, 0),  // 5 AM
                TimeWindowEnd = new TimeSpan(23, 0, 0)    // 11 PM
            },
            MembershipTier.VIP => new BookingLimits 
            { 
                MaxConcurrent = 5, 
                MaxDuration = 240, 
                MaxAdvanceDays = 14,
                TimeWindowStart = null,  // No time restrictions
                TimeWindowEnd = null
            },
            _ => new BookingLimits()
        };
    }

    private async Task<List<Guid>> GetAccessibleFacilityTypesAsync(MembershipTier tier, ClubManagementDbContext tenantContext)
    {
        // Get all facility types that allow this membership tier
        var facilityTypes = await tenantContext.FacilityTypes
            .Where(ft => ft.IsActive)
            .ToListAsync();

        return facilityTypes
            .Where(ft => !ft.AllowedMembershipTiers.Any() || ft.AllowedMembershipTiers.Contains(tier))
            .Select(ft => ft.Id)
            .ToList();
    }

    private async Task<List<string>> GetMissingCertificationsAsync(Guid userId, Guid facilityId, ClubManagementDbContext tenantContext)
    {
        var facility = await tenantContext.Facilities.FindAsync(facilityId);
        var memberCerts = await GetActiveMemberCertificationsAsync(userId, tenantContext);
        
        return facility?.RequiredCertifications
            .Where(req => !memberCerts.Any(cert => cert.CertificationType == req))
            .ToList() ?? new List<string>();
    }

    private string[] GetMemberRestrictions(Member member, Facility? facility, bool canAccessFacility, List<string> missingCertifications)
    {
        var restrictions = new List<string>();

        if (!canAccessFacility)
        {
            if (facility != null && facility.AllowedMembershipTiers.Any() && !facility.AllowedMembershipTiers.Contains(member.Tier))
            {
                var requiredTier = facility.AllowedMembershipTiers.Min();
                restrictions.Add($"Requires {requiredTier} membership or higher");
            }
        }

        if (missingCertifications.Any())
        {
            restrictions.Add($"Missing certifications: {string.Join(", ", missingCertifications)}");
        }

        switch (member.Tier)
        {
            case MembershipTier.Basic:
                restrictions.Add("Limited booking hours: 6 AM - 10 PM");
                restrictions.Add("Maximum 1 concurrent booking");
                restrictions.Add("60-minute booking limit");
                break;
            case MembershipTier.Premium:
                restrictions.Add("Extended hours: 5 AM - 11 PM");
                restrictions.Add("Maximum 3 concurrent bookings");
                break;
            case MembershipTier.VIP:
                // No additional restrictions
                break;
        }

        return restrictions.ToArray();
    }
}

public class BookingLimits
{
    public int MaxConcurrent { get; set; }
    public int MaxDuration { get; set; }
    public int MaxAdvanceDays { get; set; }
    public TimeSpan? TimeWindowStart { get; set; }
    public TimeSpan? TimeWindowEnd { get; set; }
}