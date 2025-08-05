using MudBlazor;

namespace ClubManagement.Client.Services;

public interface INotificationService
{
    /// <summary>
    /// Shows a registration success notification with details
    /// </summary>
    void ShowRegistrationSuccess(int successCount, int failCount, List<string>? warnings = null);
    
    /// <summary>
    /// Shows a registration failure notification
    /// </summary>
    void ShowRegistrationFailure(string message, string? details = null);
    
    /// <summary>
    /// Shows a recurring registration notification with action buttons
    /// </summary>
    void ShowRecurringRegistrationSuccess(string eventName, int sessionsRegistered, int totalSessions);
    
    /// <summary>
    /// Shows an event starting soon notification
    /// </summary>
    void ShowEventStartingSoon(string eventName, DateTime startTime, string? location = null);
    
    /// <summary>
    /// Shows a waitlist notification
    /// </summary>
    void ShowWaitlistNotification(string eventName, int position);
    
    /// <summary>
    /// Shows a custom notification with enhanced formatting
    /// </summary>
    void ShowEnhancedNotification(string title, string message, Severity severity, string? actionText = null, Action? action = null);
}