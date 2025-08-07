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
public class DashboardController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly CatalogDbContext _catalogContext;

    public DashboardController(ITenantDbContextFactory tenantDbContextFactory, ITenantService tenantService, CatalogDbContext catalogContext)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _catalogContext = catalogContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDataDto>>> GetDashboardData()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<DashboardDataDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Get current user with role
            var user = await tenantContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<DashboardDataDto>.ErrorResult("User not found"));

            var dashboardData = await BuildDashboardDataAsync(user, tenantContext);
            
            return Ok(ApiResponse<DashboardDataDto>.SuccessResult(dashboardData));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DashboardDataDto>.ErrorResult($"Error retrieving dashboard data: {ex.Message}"));
        }
    }

    private async Task<DashboardDataDto> BuildDashboardDataAsync(User user, ClubManagementDbContext tenantContext)
    {
        var dashboardData = new DashboardDataDto
        {
            UserRole = user.Role
        };

        switch (user.Role)
        {
            case UserRole.SuperAdmin:
                await BuildSuperAdminDashboard(dashboardData);
                break;
            case UserRole.Admin:
                await BuildAdminDashboard(dashboardData, tenantContext);
                break;
            case UserRole.Coach:
                await BuildCoachDashboard(dashboardData, user.Id, tenantContext);
                break;
            case UserRole.Staff:
                await BuildStaffDashboard(dashboardData, tenantContext);
                break;
            case UserRole.Instructor:
                await BuildInstructorDashboard(dashboardData, user.Id, tenantContext);
                break;
            case UserRole.Member:
                await BuildMemberDashboard(dashboardData, user.Id, tenantContext);
                break;
        }

        return dashboardData;
    }

    private async Task BuildSuperAdminDashboard(DashboardDataDto dashboard)
    {
        // Use catalog database for tenant data
        var tenantCount = await _catalogContext.Tenants.CountAsync();
        var activeTenants = await _catalogContext.Tenants.CountAsync(t => t.Status == TenantStatus.Active);
        
        dashboard.MetricCards.AddRange(new[]
        {
            new MetricCardDto
            {
                Title = "Total Tenants",
                Value = tenantCount.ToString(),
                SubText = $"{activeTenants} active",
                Icon = "Icons.Material.Filled.Business",
                Color = "Primary",
                TrendText = "+2 this month",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "System Health",
                Value = "98.5%",
                SubText = "Uptime",
                Icon = "Icons.Material.Filled.Health",
                Color = "Success",
                TrendText = "All systems operational",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Total Revenue",
                Value = "$127,450",
                SubText = "This month",
                Icon = "Icons.Material.Filled.AttachMoney",
                Color = "Success",
                TrendText = "+15% from last month",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Support Tickets",
                Value = "3",
                SubText = "Open tickets",
                Icon = "Icons.Material.Filled.Support",
                Color = "Warning",
                TrendText = "2 resolved today",
                TrendColor = "Success"
            }
        });

        dashboard.QuickActions.AddRange(new[]
        {
            new QuickActionDto { Title = "Create Tenant", Icon = "Icons.Material.Filled.Add", Href = "/admin/tenants/create", Color = "Primary" },
            new QuickActionDto { Title = "System Monitor", Icon = "Icons.Material.Filled.Monitor", Href = "/admin/system", Color = "Secondary" },
            new QuickActionDto { Title = "User Management", Icon = "Icons.Material.Filled.SupervisorAccount", Href = "/admin/users", Color = "Tertiary" },
            new QuickActionDto { Title = "Reports", Icon = "Icons.Material.Filled.Analytics", Href = "/admin/reports", Color = "Info" }
        });
    }

    private async Task BuildAdminDashboard(DashboardDataDto dashboard, ClubManagementDbContext tenantContext)
    {
        var memberCount = await tenantContext.Members.CountAsync();
        var activeEvents = await tenantContext.Events.CountAsync(e => e.Status == EventStatus.Scheduled && e.StartDateTime > DateTime.UtcNow);
        var thisMonthRegistrations = await tenantContext.Members.CountAsync(m => m.JoinedAt >= DateTime.UtcNow.AddDays(-30));
        
        dashboard.MetricCards.AddRange(new[]
        {
            new MetricCardDto
            {
                Title = "Total Members",
                Value = memberCount.ToString(),
                SubText = $"{thisMonthRegistrations} new this month",
                Icon = "Icons.Material.Filled.People",
                Color = "Primary",
                TrendText = "+12% from last month",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Active Events",
                Value = activeEvents.ToString(),
                SubText = "Scheduled events",
                Icon = "Icons.Material.Filled.Event",
                Color = "Secondary",
                TrendText = "3 this week",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Monthly Revenue",
                Value = "$45,230",
                SubText = "This month",
                Icon = "Icons.Material.Filled.AttachMoney",
                Color = "Success",
                TrendText = "+8% from last month",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Facility Usage",
                Value = "87%",
                SubText = "Average utilization",
                Icon = "Icons.Material.Filled.Business",
                Color = "Warning",
                TrendText = "Peak hours busy",
                TrendColor = "Warning"
            }
        });

        dashboard.QuickActions.AddRange(new[]
        {
            new QuickActionDto { Title = "Add New Member", Icon = "Icons.Material.Filled.PersonAdd", Href = "/members/create", Color = "Primary" },
            new QuickActionDto { Title = "Create Event", Icon = "Icons.Material.Filled.EventAvailable", Href = "/events/create", Color = "Secondary" },
            new QuickActionDto { Title = "View Reports", Icon = "Icons.Material.Filled.Analytics", Href = "/reports", Color = "Tertiary" },
            new QuickActionDto { Title = "Manage Staff", Icon = "Icons.Material.Filled.SupervisorAccount", Href = "/staff", Color = "Info" }
        });

        await AddRecentActivityAsync(dashboard, tenantContext);
    }

    private async Task BuildCoachDashboard(DashboardDataDto dashboard, Guid userId, ClubManagementDbContext tenantContext)
    {
        var myEvents = await tenantContext.Events
            .Where(e => e.InstructorId == userId && e.StartDateTime > DateTime.UtcNow)
            .CountAsync();
        
        var thisWeekEvents = await tenantContext.Events
            .Where(e => e.InstructorId == userId && 
                       e.StartDateTime >= DateTime.UtcNow.Date &&
                       e.StartDateTime < DateTime.UtcNow.Date.AddDays(7))
            .CountAsync();

        var totalParticipants = await tenantContext.EventRegistrations
            .Join(tenantContext.Events, r => r.EventId, e => e.Id, (r, e) => new { r, e })
            .Where(x => x.e.InstructorId == userId && x.r.Status == RegistrationStatus.Confirmed)
            .CountAsync();

        dashboard.MetricCards.AddRange(new[]
        {
            new MetricCardDto
            {
                Title = "My Upcoming Events",
                Value = myEvents.ToString(),
                SubText = $"{thisWeekEvents} this week",
                Icon = "Icons.Material.Filled.Event",
                Color = "Primary",
                TrendText = "Next session tomorrow",
                TrendColor = "Info"
            },
            new MetricCardDto
            {
                Title = "Active Participants",
                Value = totalParticipants.ToString(),
                SubText = "Registered students",
                Icon = "Icons.Material.Filled.People",
                Color = "Secondary",
                TrendText = "+5 new this week",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "This Month Revenue",
                Value = "$3,240",
                SubText = "From my classes",
                Icon = "Icons.Material.Filled.AttachMoney",
                Color = "Success",
                TrendText = "+12% from last month",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Attendance Rate",
                Value = "94%",
                SubText = "Average attendance",
                Icon = "Icons.Material.Filled.CheckCircle",
                Color = "Success",
                TrendText = "Excellent engagement",
                TrendColor = "Success"
            }
        });

        dashboard.QuickActions.AddRange(new[]
        {
            new QuickActionDto { Title = "Create Class", Icon = "Icons.Material.Filled.Add", Href = "/events/create", Color = "Primary" },
            new QuickActionDto { Title = "My Schedule", Icon = "Icons.Material.Filled.Schedule", Href = "/coach/schedule", Color = "Secondary" },
            new QuickActionDto { Title = "Student Progress", Icon = "Icons.Material.Filled.TrendingUp", Href = "/coach/progress", Color = "Tertiary" },
            new QuickActionDto { Title = "Message Participants", Icon = "Icons.Material.Filled.Message", Href = "/messages", Color = "Info" }
        });

        await AddMyEventsAsync(dashboard, userId, tenantContext);
    }

    private async Task BuildStaffDashboard(DashboardDataDto dashboard, ClubManagementDbContext tenantContext)
    {
        var todayCheckIns = await tenantContext.Members.CountAsync(m => m.LastVisitAt.HasValue && 
                                                                   m.LastVisitAt.Value.Date == DateTime.UtcNow.Date);
        var pendingRenewals = await tenantContext.Members.CountAsync(m => m.MembershipExpiresAt.HasValue && 
                                                                     m.MembershipExpiresAt.Value <= DateTime.UtcNow.AddDays(30));
        
        dashboard.MetricCards.AddRange(new[]
        {
            new MetricCardDto
            {
                Title = "Today's Check-ins",
                Value = todayCheckIns.ToString(),
                SubText = "Members visited",
                Icon = "Icons.Material.Filled.CheckCircle",
                Color = "Primary",
                TrendText = "Peak: 2-4 PM",
                TrendColor = "Info"
            },
            new MetricCardDto
            {
                Title = "Pending Renewals",
                Value = pendingRenewals.ToString(),
                SubText = "Due within 30 days",
                Icon = "Icons.Material.Filled.Refresh",
                Color = "Warning",
                TrendText = "Renewal reminders sent",
                TrendColor = "Info"
            },
            new MetricCardDto
            {
                Title = "Facility Bookings",
                Value = "18",
                SubText = "Today's bookings",
                Icon = "Icons.Material.Filled.BookOnline",
                Color = "Secondary",
                TrendText = "3 walk-ins available",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Support Requests",
                Value = "2",
                SubText = "Open requests",
                Icon = "Icons.Material.Filled.Help",
                Color = "Info",
                TrendText = "Response time: 15 min",
                TrendColor = "Success"
            }
        });

        dashboard.QuickActions.AddRange(new[]
        {
            new QuickActionDto { Title = "Check-in Member", Icon = "Icons.Material.Filled.CheckCircle", Href = "/checkin", Color = "Primary" },
            new QuickActionDto { Title = "Book Facility", Icon = "Icons.Material.Filled.BookOnline", Href = "/bookings/create", Color = "Secondary" },
            new QuickActionDto { Title = "Process Payment", Icon = "Icons.Material.Filled.Payment", Href = "/payments", Color = "Tertiary" },
            new QuickActionDto { Title = "Member Support", Icon = "Icons.Material.Filled.Support", Href = "/support", Color = "Info" }
        });
    }

    private async Task BuildInstructorDashboard(DashboardDataDto dashboard, Guid userId, ClubManagementDbContext tenantContext)
    {
        var myClasses = await tenantContext.Events
            .Where(e => e.InstructorId == userId && e.StartDateTime > DateTime.UtcNow)
            .CountAsync();

        dashboard.MetricCards.AddRange(new[]
        {
            new MetricCardDto
            {
                Title = "My Classes",
                Value = myClasses.ToString(),
                SubText = "Upcoming sessions",
                Icon = "Icons.Material.Filled.School",
                Color = "Primary",
                TrendText = "Next class in 2 hours",
                TrendColor = "Info"
            },
            new MetricCardDto
            {
                Title = "Students",
                Value = "24",
                SubText = "Active students",
                Icon = "Icons.Material.Filled.People",
                Color = "Secondary",
                TrendText = "+3 new this week",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Completion Rate",
                Value = "96%",
                SubText = "Course completion",
                Icon = "Icons.Material.Filled.CheckCircle",
                Color = "Success",
                TrendText = "Above average",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Resources",
                Value = "12",
                SubText = "Available materials",
                Icon = "Icons.Material.Filled.LibraryBooks",
                Color = "Info",
                TrendText = "2 new this week",
                TrendColor = "Success"
            }
        });

        dashboard.QuickActions.AddRange(new[]
        {
            new QuickActionDto { Title = "Plan Lesson", Icon = "Icons.Material.Filled.Create", Href = "/instructor/lessons/create", Color = "Primary" },
            new QuickActionDto { Title = "Record Attendance", Icon = "Icons.Material.Filled.CheckBox", Href = "/instructor/attendance", Color = "Secondary" },
            new QuickActionDto { Title = "Student Progress", Icon = "Icons.Material.Filled.TrendingUp", Href = "/instructor/progress", Color = "Tertiary" },
            new QuickActionDto { Title = "Resources", Icon = "Icons.Material.Filled.LibraryBooks", Href = "/instructor/resources", Color = "Info" }
        });
    }

    private async Task BuildMemberDashboard(DashboardDataDto dashboard, Guid userId, ClubManagementDbContext tenantContext)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null) return;

        var myRegistrations = await tenantContext.EventRegistrations
            .Join(tenantContext.Events, r => r.EventId, e => e.Id, (r, e) => new { r, e })
            .Where(x => x.r.MemberId == member.Id && x.e.StartDateTime > DateTime.UtcNow)
            .CountAsync();

        var thisMonthVisits = await tenantContext.Members
            .Where(m => m.Id == member.Id && m.LastVisitAt.HasValue && 
                       m.LastVisitAt.Value >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        var daysUntilRenewal = member.MembershipExpiresAt.HasValue ? 
            (member.MembershipExpiresAt.Value - DateTime.UtcNow).Days : 0;

        dashboard.MetricCards.AddRange(new[]
        {
            new MetricCardDto
            {
                Title = "My Events",
                Value = myRegistrations.ToString(),
                SubText = "Upcoming events",
                Icon = "Icons.Material.Filled.Event",
                Color = "Primary",
                TrendText = "Next event tomorrow",
                TrendColor = "Info"
            },
            new MetricCardDto
            {
                Title = "This Month Visits",
                Value = "12",
                SubText = "Facility visits",
                Icon = "Icons.Material.Filled.DirectionsRun",
                Color = "Secondary",
                TrendText = "+3 from last month",
                TrendColor = "Success"
            },
            new MetricCardDto
            {
                Title = "Membership Status",
                Value = member.Tier.ToString(),
                SubText = $"Expires in {daysUntilRenewal} days",
                Icon = "Icons.Material.Filled.CardMembership",
                Color = daysUntilRenewal < 30 ? "Warning" : "Success",
                TrendText = daysUntilRenewal < 30 ? "Renewal due soon" : "Active membership",
                TrendColor = daysUntilRenewal < 30 ? "Warning" : "Success"
            },
            new MetricCardDto
            {
                Title = "Account Balance",
                Value = $"${member.Balance:F2}",
                SubText = "Current balance",
                Icon = "Icons.Material.Filled.AccountBalance",
                Color = member.Balance < 0 ? "Error" : "Success",
                TrendText = member.Balance < 0 ? "Payment required" : "Account in good standing",
                TrendColor = member.Balance < 0 ? "Error" : "Success"
            }
        });

        dashboard.QuickActions.AddRange(new[]
        {
            new QuickActionDto { Title = "Book Facility", Icon = "Icons.Material.Filled.BookOnline", Href = "/bookings/create", Color = "Primary" },
            new QuickActionDto { Title = "Register for Event", Icon = "Icons.Material.Filled.EventAvailable", Href = "/events", Color = "Secondary" },
            new QuickActionDto { Title = "My Schedule", Icon = "Icons.Material.Filled.Schedule", Href = "/member/schedule", Color = "Tertiary" },
            new QuickActionDto { Title = "Update Profile", Icon = "Icons.Material.Filled.Person", Href = "/profile", Color = "Info" }
        });

        await AddMemberUpcomingEventsAsync(dashboard, member.Id, tenantContext);
    }

    private async Task AddRecentActivityAsync(DashboardDataDto dashboard, ClubManagementDbContext tenantContext)
    {
        var recentMembers = await tenantContext.Members
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedAt)
            .Take(3)
            .ToListAsync();

        foreach (var member in recentMembers)
        {
            dashboard.RecentActivity.Add(new ActivityItemDto
            {
                Title = "New member registered",
                Description = $"{member.User.FirstName} {member.User.LastName} joined as {member.Tier} member",
                Icon = "Icons.Material.Filled.PersonAdd",
                Color = "Success",
                Timestamp = member.CreatedAt
            });
        }

        var recentEvents = await tenantContext.Events
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(e => e.CreatedAt)
            .Take(2)
            .ToListAsync();

        foreach (var evt in recentEvents)
        {
            dashboard.RecentActivity.Add(new ActivityItemDto
            {
                Title = "Event created",
                Description = $"{evt.Title} scheduled for {evt.StartDateTime:MMM dd}",
                Icon = "Icons.Material.Filled.Event",
                Color = "Primary",
                Timestamp = evt.CreatedAt
            });
        }
    }

    private async Task AddMyEventsAsync(DashboardDataDto dashboard, Guid instructorId, ClubManagementDbContext tenantContext)
    {
        var upcomingEvents = await tenantContext.Events
            .Where(e => e.InstructorId == instructorId && e.StartDateTime > DateTime.UtcNow)
            .OrderBy(e => e.StartDateTime)
            .Take(3)
            .ToListAsync();

        foreach (var evt in upcomingEvents)
        {
            dashboard.UpcomingEvents.Add(new UpcomingEventDto
            {
                Id = evt.Id,
                Title = evt.Title,
                StartDateTime = evt.StartDateTime,
                CurrentEnrollment = evt.CurrentEnrollment,
                MaxCapacity = evt.MaxCapacity,
                Status = GetEventStatusText(evt.CurrentEnrollment, evt.MaxCapacity),
                StatusColor = GetEventStatusColor(evt.CurrentEnrollment, evt.MaxCapacity)
            });
        }
    }

    private async Task AddMemberUpcomingEventsAsync(DashboardDataDto dashboard, Guid memberId, ClubManagementDbContext tenantContext)
    {
        var upcomingEvents = await tenantContext.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.MemberId == memberId && r.Event.StartDateTime > DateTime.UtcNow && r.Status == RegistrationStatus.Confirmed)
            .OrderBy(r => r.Event.StartDateTime)
            .Take(3)
            .Select(r => r.Event)
            .ToListAsync();

        foreach (var evt in upcomingEvents)
        {
            dashboard.UpcomingEvents.Add(new UpcomingEventDto
            {
                Id = evt.Id,
                Title = evt.Title,
                StartDateTime = evt.StartDateTime,
                CurrentEnrollment = evt.CurrentEnrollment,
                MaxCapacity = evt.MaxCapacity,
                Status = "Registered",
                StatusColor = "Success",
                IsUserRegistered = true
            });
        }
    }

    private static string GetEventStatusText(int current, int max)
    {
        if (current == 0) return "No registrations";
        if (current >= max) return "Full";
        if (current >= max * 0.8) return "Almost full";
        return $"{current}/{max} spots";
    }

    private static string GetEventStatusColor(int current, int max)
    {
        if (current == 0) return "Default";
        if (current >= max) return "Error";
        if (current >= max * 0.8) return "Warning";
        return "Success";
    }
}