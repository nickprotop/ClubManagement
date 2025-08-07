using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Api.Extensions;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;

    public PermissionsController(ITenantDbContextFactory tenantDbContextFactory, ITenantService tenantService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<GlobalPermissionsDto>>> GetGlobalPermissions()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<GlobalPermissionsDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Get current user with role
            var user = await tenantContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<GlobalPermissionsDto>.ErrorResult("User not found"));

            var permissions = BuildGlobalPermissions(user);
            
            return Ok(ApiResponse<GlobalPermissionsDto>.SuccessResult(permissions));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<GlobalPermissionsDto>.ErrorResult($"Error retrieving permissions: {ex.Message}"));
        }
    }

    private static GlobalPermissionsDto BuildGlobalPermissions(User user)
    {
        var permissions = new GlobalPermissionsDto
        {
            UserRole = user.Role
        };

        switch (user.Role)
        {
            case UserRole.SuperAdmin:
                BuildSuperAdminPermissions(permissions);
                break;
            case UserRole.Admin:
                BuildAdminPermissions(permissions);
                break;
            case UserRole.Coach:
                BuildCoachPermissions(permissions);
                break;
            case UserRole.Staff:
                BuildStaffPermissions(permissions);
                break;
            case UserRole.Instructor:
                BuildInstructorPermissions(permissions);
                break;
            case UserRole.Member:
                BuildMemberPermissions(permissions);
                break;
        }

        return permissions;
    }

    private static void BuildSuperAdminPermissions(GlobalPermissionsDto permissions)
    {
        // Super Admin has all permissions
        permissions.Members = new GlobalMemberPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanImpersonate = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManagePayments = true,
            CanManageMemberships = true
        };

        permissions.Events = new GlobalEventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageRegistrations = true,
            CanRegister = true,
            CanCancelRegistrations = true,
            CanViewCalendar = true,
            CanManageInstructors = true
        };

        permissions.Facilities = new GlobalFacilityPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanBook = true,
            CanManageBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanCancelBookings = true
        };

        permissions.Hardware = new GlobalHardwarePermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanAssign = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageTypes = true,
            CanManageInventory = true,
            CanProcessFees = true,
            CanViewFinancials = true,
            CanScheduleMaintenance = true,
            CanManageEventEquipment = true
        };

        permissions.Payments = new GlobalPaymentPermissions
        {
            CanView = true,
            CanProcess = true,
            CanRefund = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageSubscriptions = true,
            CanGenerateInvoices = true
        };

        permissions.Communications = new GlobalCommunicationPermissions
        {
            CanView = true,
            CanSend = true,
            CanBroadcast = true,
            CanManageTemplates = true,
            CanViewAll = true,
            CanViewOwn = true
        };

        permissions.Reports = new GlobalReportPermissions
        {
            CanView = true,
            CanGenerate = true,
            CanExport = true,
            CanViewFinancial = true,
            CanViewMembership = true,
            CanViewUsage = true,
            CanViewSystem = true
        };

        permissions.System = new GlobalSystemPermissions
        {
            CanViewSettings = true,
            CanEditSettings = true,
            CanManageUsers = true,
            CanManageRoles = true,
            CanViewLogs = true,
            CanManageTenants = true,
            CanViewSystem = true,
            IsAdmin = true,
            IsSuperAdmin = true
        };

        permissions.Navigation = new GlobalNavigationPermissions
        {
            ShowDashboard = true,
            ShowMembers = true,
            ShowEvents = true,
            ShowFacilities = true,
            ShowHardware = true,
            ShowPayments = true,
            ShowCommunications = true,
            ShowReports = true,
            ShowSettings = true,
            ShowMemberManagement = true,
            ShowEventManagement = true,
            ShowFacilityManagement = true,
            ShowHardwareManagement = true,
            ShowPaymentManagement = true,
            ShowUserManagement = true
        };
    }

    private static void BuildAdminPermissions(GlobalPermissionsDto permissions)
    {
        permissions.Members = new GlobalMemberPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanImpersonate = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManagePayments = true,
            CanManageMemberships = true
        };

        permissions.Events = new GlobalEventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageRegistrations = true,
            CanRegister = true,
            CanCancelRegistrations = true,
            CanViewCalendar = true,
            CanManageInstructors = true
        };

        permissions.Facilities = new GlobalFacilityPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanBook = true,
            CanManageBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanCancelBookings = true
        };

        permissions.Hardware = new GlobalHardwarePermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanAssign = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageTypes = true,
            CanManageInventory = true,
            CanProcessFees = true,
            CanViewFinancials = true,
            CanScheduleMaintenance = true,
            CanManageEventEquipment = true
        };

        permissions.Payments = new GlobalPaymentPermissions
        {
            CanView = true,
            CanProcess = true,
            CanRefund = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageSubscriptions = true,
            CanGenerateInvoices = true
        };

        permissions.Communications = new GlobalCommunicationPermissions
        {
            CanView = true,
            CanSend = true,
            CanBroadcast = true,
            CanManageTemplates = true,
            CanViewAll = true,
            CanViewOwn = true
        };

        permissions.Reports = new GlobalReportPermissions
        {
            CanView = true,
            CanGenerate = true,
            CanExport = true,
            CanViewFinancial = true,
            CanViewMembership = true,
            CanViewUsage = true,
            CanViewSystem = false
        };

        permissions.System = new GlobalSystemPermissions
        {
            CanViewSettings = true,
            CanEditSettings = true,
            CanManageUsers = true,
            CanManageRoles = false,
            CanViewLogs = true,
            CanManageTenants = false,
            CanViewSystem = false,
            IsAdmin = true,
            IsSuperAdmin = false
        };

        permissions.Navigation = new GlobalNavigationPermissions
        {
            ShowDashboard = true,
            ShowMembers = true,
            ShowEvents = true,
            ShowFacilities = true,
            ShowHardware = true,
            ShowPayments = true,
            ShowCommunications = true,
            ShowReports = true,
            ShowSettings = true,
            ShowMemberManagement = true,
            ShowEventManagement = true,
            ShowFacilityManagement = true,
            ShowHardwareManagement = true,
            ShowPaymentManagement = true,
            ShowUserManagement = true
        };
    }

    private static void BuildCoachPermissions(GlobalPermissionsDto permissions)
    {
        permissions.Members = new GlobalMemberPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanImpersonate = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManagePayments = false,
            CanManageMemberships = false
        };

        permissions.Events = new GlobalEventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageRegistrations = true,
            CanRegister = true,
            CanCancelRegistrations = true,
            CanViewCalendar = true,
            CanManageInstructors = false
        };

        permissions.Facilities = new GlobalFacilityPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanBook = true,
            CanManageBookings = false,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanCancelBookings = false
        };

        permissions.Hardware = new GlobalHardwarePermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanAssign = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageInventory = false,
            CanProcessFees = false,
            CanManageTypes = false,
            CanViewFinancials = false,
            CanScheduleMaintenance = false,
            CanManageEventEquipment = true
        };

        permissions.Payments = new GlobalPaymentPermissions
        {
            CanView = false,
            CanProcess = false,
            CanRefund = false,
            CanViewAll = false,
            CanViewOwn = true,
            CanManageSubscriptions = false,
            CanGenerateInvoices = false
        };

        permissions.Communications = new GlobalCommunicationPermissions
        {
            CanView = true,
            CanSend = true,
            CanBroadcast = false,
            CanManageTemplates = false,
            CanViewAll = false,
            CanViewOwn = true
        };

        permissions.Reports = new GlobalReportPermissions
        {
            CanView = true,
            CanGenerate = false,
            CanExport = false,
            CanViewFinancial = false,
            CanViewMembership = false,
            CanViewUsage = true,
            CanViewSystem = false
        };

        permissions.System = new GlobalSystemPermissions
        {
            CanViewSettings = false,
            CanEditSettings = false,
            CanManageUsers = false,
            CanManageRoles = false,
            CanViewLogs = false,
            CanManageTenants = false,
            CanViewSystem = false,
            IsAdmin = false,
            IsSuperAdmin = false
        };

        permissions.Navigation = new GlobalNavigationPermissions
        {
            ShowDashboard = true,
            ShowMembers = true,
            ShowEvents = true,
            ShowFacilities = true,
            ShowHardware = true,
            ShowPayments = false,
            ShowCommunications = true,
            ShowReports = false,
            ShowSettings = false,
            ShowMemberManagement = false,
            ShowEventManagement = true,
            ShowFacilityManagement = false,
            ShowHardwareManagement = false,
            ShowPaymentManagement = false,
            ShowUserManagement = false
        };
    }

    private static void BuildStaffPermissions(GlobalPermissionsDto permissions)
    {
        permissions.Members = new GlobalMemberPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = false,
            CanImpersonate = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManagePayments = true,
            CanManageMemberships = true
        };

        permissions.Events = new GlobalEventPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageRegistrations = true,
            CanRegister = true,
            CanCancelRegistrations = true,
            CanViewCalendar = true,
            CanManageInstructors = false
        };

        permissions.Facilities = new GlobalFacilityPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanBook = true,
            CanManageBookings = true,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanCancelBookings = true
        };

        permissions.Hardware = new GlobalHardwarePermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = false,
            CanAssign = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageInventory = true,
            CanProcessFees = true,
            CanManageTypes = false,
            CanViewFinancials = false,
            CanScheduleMaintenance = true,
            CanManageEventEquipment = false
        };

        permissions.Payments = new GlobalPaymentPermissions
        {
            CanView = true,
            CanProcess = true,
            CanRefund = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageSubscriptions = true,
            CanGenerateInvoices = true
        };

        permissions.Communications = new GlobalCommunicationPermissions
        {
            CanView = true,
            CanSend = true,
            CanBroadcast = false,
            CanManageTemplates = true,
            CanViewAll = true,
            CanViewOwn = true
        };

        permissions.Reports = new GlobalReportPermissions
        {
            CanView = true,
            CanGenerate = true,
            CanExport = true,
            CanViewFinancial = false,
            CanViewMembership = true,
            CanViewUsage = true,
            CanViewSystem = false
        };

        permissions.System = new GlobalSystemPermissions
        {
            CanViewSettings = false,
            CanEditSettings = false,
            CanManageUsers = false,
            CanManageRoles = false,
            CanViewLogs = false,
            CanManageTenants = false,
            CanViewSystem = false,
            IsAdmin = false,
            IsSuperAdmin = false
        };

        permissions.Navigation = new GlobalNavigationPermissions
        {
            ShowDashboard = true,
            ShowMembers = true,
            ShowEvents = true,
            ShowFacilities = true,
            ShowHardware = true,
            ShowPayments = true,
            ShowCommunications = true,
            ShowReports = true,
            ShowSettings = false,
            ShowMemberManagement = true,
            ShowEventManagement = false,
            ShowFacilityManagement = true,
            ShowHardwareManagement = false,
            ShowPaymentManagement = true,
            ShowUserManagement = false
        };
    }

    private static void BuildInstructorPermissions(GlobalPermissionsDto permissions)
    {
        permissions.Members = new GlobalMemberPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanImpersonate = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManagePayments = false,
            CanManageMemberships = false
        };

        permissions.Events = new GlobalEventPermissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageRegistrations = true,
            CanRegister = true,
            CanCancelRegistrations = false,
            CanViewCalendar = true,
            CanManageInstructors = false
        };

        permissions.Facilities = new GlobalFacilityPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanBook = true,
            CanManageBookings = false,
            CanViewAllBookings = true,
            CanViewOwnBookings = true,
            CanCancelBookings = false
        };

        permissions.Hardware = new GlobalHardwarePermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanAssign = true,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageTypes = false,
            CanManageInventory = false,
            CanProcessFees = false,
            CanViewFinancials = false,
            CanScheduleMaintenance = false,
            CanManageEventEquipment = true
        };

        permissions.Payments = new GlobalPaymentPermissions
        {
            CanView = false,
            CanProcess = false,
            CanRefund = false,
            CanViewAll = false,
            CanViewOwn = true,
            CanManageSubscriptions = false,
            CanGenerateInvoices = false
        };

        permissions.Communications = new GlobalCommunicationPermissions
        {
            CanView = true,
            CanSend = true,
            CanBroadcast = false,
            CanManageTemplates = false,
            CanViewAll = false,
            CanViewOwn = true
        };

        permissions.Reports = new GlobalReportPermissions
        {
            CanView = true,
            CanGenerate = false,
            CanExport = false,
            CanViewFinancial = false,
            CanViewMembership = false,
            CanViewUsage = true,
            CanViewSystem = false
        };

        permissions.System = new GlobalSystemPermissions
        {
            CanViewSettings = false,
            CanEditSettings = false,
            CanManageUsers = false,
            CanManageRoles = false,
            CanViewLogs = false,
            CanManageTenants = false,
            CanViewSystem = false,
            IsAdmin = false,
            IsSuperAdmin = false
        };

        permissions.Navigation = new GlobalNavigationPermissions
        {
            ShowDashboard = true,
            ShowMembers = true,
            ShowEvents = true,
            ShowFacilities = true,
            ShowHardware = true,
            ShowPayments = false,
            ShowCommunications = true,
            ShowReports = false,
            ShowSettings = false,
            ShowMemberManagement = false,
            ShowEventManagement = true,
            ShowFacilityManagement = false,
            ShowHardwareManagement = false,
            ShowPaymentManagement = false,
            ShowUserManagement = false
        };
    }

    private static void BuildMemberPermissions(GlobalPermissionsDto permissions)
    {
        permissions.Members = new GlobalMemberPermissions
        {
            CanView = false,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanImpersonate = false,
            CanViewAll = false,
            CanViewOwn = true,
            CanManagePayments = false,
            CanManageMemberships = false
        };

        permissions.Events = new GlobalEventPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanViewAll = true,
            CanViewOwn = true,
            CanManageRegistrations = false,
            CanRegister = true,
            CanCancelRegistrations = true,
            CanViewCalendar = true,
            CanManageInstructors = false
        };

        permissions.Facilities = new GlobalFacilityPermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanBook = true,
            CanManageBookings = false,
            CanViewAllBookings = false,
            CanViewOwnBookings = true,
            CanCancelBookings = true
        };

        permissions.Hardware = new GlobalHardwarePermissions
        {
            CanView = true,
            CanCreate = false,
            CanEdit = false,
            CanDelete = false,
            CanAssign = false,
            CanViewAll = false,
            CanViewOwn = true,
            CanManageInventory = false,
            CanProcessFees = false,
            CanManageTypes = false,
            CanViewFinancials = false,
            CanScheduleMaintenance = false,
            CanManageEventEquipment = false
        };

        permissions.Payments = new GlobalPaymentPermissions
        {
            CanView = false,
            CanProcess = false,
            CanRefund = false,
            CanViewAll = false,
            CanViewOwn = true,
            CanManageSubscriptions = false,
            CanGenerateInvoices = false
        };

        permissions.Communications = new GlobalCommunicationPermissions
        {
            CanView = false,
            CanSend = false,
            CanBroadcast = false,
            CanManageTemplates = false,
            CanViewAll = false,
            CanViewOwn = true
        };

        permissions.Reports = new GlobalReportPermissions
        {
            CanView = false,
            CanGenerate = false,
            CanExport = false,
            CanViewFinancial = false,
            CanViewMembership = false,
            CanViewUsage = false,
            CanViewSystem = false
        };

        permissions.System = new GlobalSystemPermissions
        {
            CanViewSettings = false,
            CanEditSettings = false,
            CanManageUsers = false,
            CanManageRoles = false,
            CanViewLogs = false,
            CanManageTenants = false,
            CanViewSystem = false,
            IsAdmin = false,
            IsSuperAdmin = false
        };

        permissions.Navigation = new GlobalNavigationPermissions
        {
            ShowDashboard = true,
            ShowMembers = false,
            ShowEvents = true,
            ShowFacilities = true,
            ShowHardware = true,
            ShowPayments = false,
            ShowCommunications = false,
            ShowReports = false,
            ShowSettings = false,
            ShowMemberManagement = false,
            ShowEventManagement = false,
            ShowFacilityManagement = false,
            ShowHardwareManagement = false,
            ShowPaymentManagement = false,
            ShowUserManagement = false
        };
    }
}