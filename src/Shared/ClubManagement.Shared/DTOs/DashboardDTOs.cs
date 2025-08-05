using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class DashboardDataDto
{
    public UserRole UserRole { get; set; }
    public List<MetricCardDto> MetricCards { get; set; } = new();
    public List<QuickActionDto> QuickActions { get; set; } = new();
    public List<ActivityItemDto> RecentActivity { get; set; } = new();
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
    public List<AlertDto> Alerts { get; set; } = new();
}

public class MetricCardDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string SubText { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "Primary"; // MudBlazor color
    public string TrendText { get; set; } = string.Empty;
    public string TrendColor { get; set; } = "Default";
}

public class QuickActionDto
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Color { get; set; } = "Primary";
    public bool RequiresPermission { get; set; } = false;
    public string Permission { get; set; } = string.Empty;
}

public class ActivityItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "Primary";
    public DateTime Timestamp { get; set; }
    public string TimeAgo => GetTimeAgo(Timestamp);

    private static string GetTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;
        if (timeSpan.TotalMinutes < 1) return "Just now";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} days ago";
        return timestamp.ToString("MMM dd, yyyy");
    }
}

public class UpcomingEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public int CurrentEnrollment { get; set; }
    public int MaxCapacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "Primary";
    public bool CanRegister { get; set; } = false;
    public bool IsUserRegistered { get; set; } = false;
}

public class AlertDto
{
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Success, Warning, Error
    public string Icon { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? ActionText { get; set; }
    public string? ActionHref { get; set; }
}