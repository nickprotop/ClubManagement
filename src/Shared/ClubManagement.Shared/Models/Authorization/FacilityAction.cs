namespace ClubManagement.Shared.Models.Authorization;

public enum FacilityAction
{
    // Basic CRUD
    View,
    ViewDetails,
    Create,
    Edit,
    Delete,
    
    // Booking Management
    Book,
    CancelBooking,
    ModifyBooking,
    ViewAllBookings,
    
    // Member-specific Actions
    BookForSelf,
    ViewOwnBookings,
    CancelOwnBooking,
    ModifyOwnBooking,
    
    // Facility Operations
    CheckInMembers,
    CheckOutMembers,
    MarkMaintenance,
    MarkOutOfOrder,
    Retire,
    Reactivate,
    ChangeLocation,
    
    // Member Integration Actions
    OverrideBookingLimits,
    GrantTierAccess,
    RequireCertification,
    ViewMemberUsage,
    AccessMemberPortal,
    
    // Type & Configuration
    CreateType,
    EditType,
    DeleteType,
    ManagePropertySchema,
    
    // Financial Operations
    ViewFinancials,
    SetRates,
    ProcessPayments,
    WaiveFees,
    
    // Calendar & Scheduling
    ViewCalendar,
    ManageSchedule,
    BlockTime,
    OverrideCapacity,
    
    // Reporting
    ViewReports,
    GenerateReports,
    ExportData,
    ViewAnalytics,
    
    // Event Integration
    AssignToEvents,
    ViewEventBookings,
    ManageEventFacilities
}