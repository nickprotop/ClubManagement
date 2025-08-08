namespace ClubManagement.Shared.Models.Authorization;

public class FacilityPermissions
{
    // Basic CRUD Operations
    public bool CanView { get; set; }
    public bool CanViewDetails { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    
    // Booking Management
    public bool CanBook { get; set; }
    public bool CanCancelBookings { get; set; }
    public bool CanModifyBookings { get; set; }
    public bool CanViewAllBookings { get; set; }
    public bool CanOverrideBookingLimits { get; set; }
    
    // Member-specific Booking Permissions
    public bool CanBookForSelf { get; set; }
    public bool CanBookForOthers { get; set; }
    public bool CanCancelOwnBookings { get; set; }
    public bool CanModifyOwnBookings { get; set; }
    public bool CanViewOwnBookings { get; set; }
    
    // Facility Operations
    public bool CanCheckInMembers { get; set; }
    public bool CanCheckOutMembers { get; set; }
    public bool CanMarkMaintenance { get; set; }
    public bool CanMarkOutOfOrder { get; set; }
    public bool CanRetire { get; set; }
    public bool CanReactivate { get; set; }
    public bool CanChangeLocation { get; set; }
    
    // Type Management
    public bool CanCreateTypes { get; set; }
    public bool CanEditTypes { get; set; }
    public bool CanDeleteTypes { get; set; }
    public bool CanManagePropertySchemas { get; set; }
    
    // Financial Operations
    public bool CanViewFinancials { get; set; }
    public bool CanSetRates { get; set; }
    public bool CanProcessPayments { get; set; }
    public bool CanWaiveFees { get; set; }
    
    // Calendar & Scheduling
    public bool CanViewCalendar { get; set; }
    public bool CanManageSchedule { get; set; }
    public bool CanBlockTime { get; set; }
    public bool CanOverrideCapacity { get; set; }
    
    // Reporting
    public bool CanViewReports { get; set; }
    public bool CanGenerateReports { get; set; }
    public bool CanExportData { get; set; }
    public bool CanViewAnalytics { get; set; }
    
    // Event Integration
    public bool CanAssignToEvents { get; set; }
    public bool CanViewEventBookings { get; set; }
    public bool CanManageEventFacilities { get; set; }
    
    // Member Integration & Access Control
    public bool CanAccessMemberPortal { get; set; }
    public bool CanGrantTierAccess { get; set; }
    public bool CanRequireCertification { get; set; }
    public bool CanViewMemberUsage { get; set; }
    public bool CanBypassCertificationRequirements { get; set; }
    
    // Access Scope
    public bool CanViewAll { get; set; }
    public bool CanViewOwn { get; set; }
    
    // Member-specific Access Control
    public List<Guid> AccessibleFacilityTypes { get; set; } = new();
    public int MaxConcurrentBookings { get; set; }
    public int MaxBookingDurationMinutes { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public TimeSpan? BookingTimeWindowStart { get; set; }
    public TimeSpan? BookingTimeWindowEnd { get; set; }
    
    // Certification Requirements
    public List<string> RequiredCertifications { get; set; } = new();
    public List<string> MissingCertifications { get; set; } = new();
    
    // Context Information
    public string[] Restrictions { get; set; } = Array.Empty<string>();
    public string[] ReasonsDenied { get; set; } = Array.Empty<string>();
    public bool HasActiveBooking { get; set; }
    public int CurrentBookingCount { get; set; }
    public MembershipTier? MembershipTier { get; set; }
    public MembershipTier? RequiredTierForAction { get; set; }
}