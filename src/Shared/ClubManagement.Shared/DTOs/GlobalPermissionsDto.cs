using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class GlobalPermissionsDto
{
    public UserRole UserRole { get; set; }
    public GlobalMemberPermissions Members { get; set; } = new();
    public GlobalEventPermissions Events { get; set; } = new();
    public GlobalFacilityPermissions Facilities { get; set; } = new();
    public GlobalPaymentPermissions Payments { get; set; } = new();
    public GlobalCommunicationPermissions Communications { get; set; } = new();
    public GlobalReportPermissions Reports { get; set; } = new();
    public GlobalSystemPermissions System { get; set; } = new();
    public GlobalNavigationPermissions Navigation { get; set; } = new();
}

public class GlobalMemberPermissions
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanImpersonate { get; set; }
    public bool CanViewAll { get; set; }
    public bool CanViewOwn { get; set; }
    public bool CanManagePayments { get; set; }
    public bool CanManageMemberships { get; set; }
}

public class GlobalEventPermissions
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanViewAll { get; set; }
    public bool CanViewOwn { get; set; }
    public bool CanManageRegistrations { get; set; }
    public bool CanRegister { get; set; }
    public bool CanCancelRegistrations { get; set; }
    public bool CanViewCalendar { get; set; }
    public bool CanManageInstructors { get; set; }
}

public class GlobalFacilityPermissions
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanBook { get; set; }
    public bool CanManageBookings { get; set; }
    public bool CanViewAllBookings { get; set; }
    public bool CanViewOwnBookings { get; set; }
    public bool CanCancelBookings { get; set; }
}

public class GlobalPaymentPermissions
{
    public bool CanView { get; set; }
    public bool CanProcess { get; set; }
    public bool CanRefund { get; set; }
    public bool CanViewAll { get; set; }
    public bool CanViewOwn { get; set; }
    public bool CanManageSubscriptions { get; set; }
    public bool CanGenerateInvoices { get; set; }
}

public class GlobalCommunicationPermissions
{
    public bool CanView { get; set; }
    public bool CanSend { get; set; }
    public bool CanBroadcast { get; set; }
    public bool CanManageTemplates { get; set; }
    public bool CanViewAll { get; set; }
    public bool CanViewOwn { get; set; }
}

public class GlobalReportPermissions
{
    public bool CanView { get; set; }
    public bool CanGenerate { get; set; }
    public bool CanExport { get; set; }
    public bool CanViewFinancial { get; set; }
    public bool CanViewMembership { get; set; }
    public bool CanViewUsage { get; set; }
    public bool CanViewSystem { get; set; }
}

public class GlobalSystemPermissions
{
    public bool CanViewSettings { get; set; }
    public bool CanEditSettings { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageRoles { get; set; }
    public bool CanViewLogs { get; set; }
    public bool CanManageTenants { get; set; }
    public bool CanViewSystem { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
}

public class GlobalNavigationPermissions
{
    public bool ShowDashboard { get; set; } = true;
    public bool ShowMembers { get; set; }
    public bool ShowEvents { get; set; }
    public bool ShowFacilities { get; set; }
    public bool ShowPayments { get; set; }
    public bool ShowCommunications { get; set; }
    public bool ShowReports { get; set; }
    public bool ShowSettings { get; set; }
    public bool ShowMemberManagement { get; set; }
    public bool ShowEventManagement { get; set; }
    public bool ShowFacilityManagement { get; set; }
    public bool ShowPaymentManagement { get; set; }
    public bool ShowUserManagement { get; set; }
}