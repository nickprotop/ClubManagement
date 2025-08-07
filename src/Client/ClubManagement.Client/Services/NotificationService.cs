using MudBlazor;

namespace ClubManagement.Client.Services;

public class NotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;

    public NotificationService(ISnackbar snackbar, IDialogService dialogService)
    {
        _snackbar = snackbar;
        _dialogService = dialogService;
    }

    public void ShowRegistrationSuccess(int successCount, int failCount, List<string>? warnings = null)
    {
        var severity = failCount > 0 ? Severity.Warning : Severity.Success;
        var message = $"✅ {successCount} registration(s) successful";
        
        if (failCount > 0)
        {
            message += $", ❌ {failCount} failed";
        }
        
        _snackbar.Add(message, severity, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = failCount > 0 ? 8000 : 5000;
            config.HideTransitionDuration = 500;
        });

        // Show warnings as separate notifications
        if (warnings?.Any() == true)
        {
            foreach (var warning in warnings.Take(3)) // Limit to avoid spam
            {
                _snackbar.Add($"⚠️ {warning}", Severity.Warning, config =>
                {
                    config.ShowCloseIcon = true;
                    config.VisibleStateDuration = 6000;
                });
            }
        }
    }

    public void ShowRegistrationFailure(string message, string? details = null)
    {
        var fullMessage = $"❌ Registration Failed: {message}";
        if (!string.IsNullOrEmpty(details))
        {
            fullMessage += $"\n{details}";
        }

        _snackbar.Add(fullMessage, Severity.Error, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 10000;
            config.HideTransitionDuration = 500;
        });
    }

    public void ShowRecurringRegistrationSuccess(string eventName, int sessionsRegistered, int totalSessions)
    {
        var message = $"🔄 Registered for {sessionsRegistered}/{totalSessions} sessions of '{eventName}'";
        
        _snackbar.Add(message, Severity.Success, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 7000;
            config.HideTransitionDuration = 500;
        });
    }

    public void ShowEventStartingSoon(string eventName, DateTime startTime, string? location = null)
    {
        var timeUntil = startTime - DateTime.Now;
        var timeText = timeUntil.TotalHours < 1 
            ? $"{(int)timeUntil.TotalMinutes} minutes"
            : $"{(int)timeUntil.TotalHours} hours";
            
        var message = $"⏰ '{eventName}' starts in {timeText}";
        if (!string.IsNullOrEmpty(location))
        {
            message += $" at {location}";
        }

        _snackbar.Add(message, Severity.Info, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 10000;
            config.HideTransitionDuration = 500;
        });
    }

    public void ShowWaitlistNotification(string eventName, int position)
    {
        var message = $"📋 Added to waitlist for '{eventName}' (Position #{position})";
        
        _snackbar.Add(message, Severity.Warning, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 8000;
            config.HideTransitionDuration = 500;
        });
    }

    public void ShowEnhancedNotification(string title, string message, Severity severity, string? actionText = null, Action? action = null)
    {
        var icon = severity switch
        {
            Severity.Success => "✅",
            Severity.Warning => "⚠️",
            Severity.Error => "❌",
            Severity.Info => "ℹ️",
            _ => "📢"
        };

        var fullMessage = $"{icon} {title}";
        if (!string.IsNullOrEmpty(message))
        {
            fullMessage += $": {message}";
        }

        _snackbar.Add(fullMessage, severity, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = severity == Severity.Error ? 10000 : 6000;
            config.HideTransitionDuration = 500;
            
            if (!string.IsNullOrEmpty(actionText) && action != null)
            {
                config.Action = actionText;
                config.ActionColor = Color.Inherit;
                config.OnClick = _ => 
                {
                    action.Invoke();
                    return Task.CompletedTask;
                };
            }
        });
    }

    public Task ShowSuccessAsync(string message)
    {
        _snackbar.Add($"✅ {message}", Severity.Success, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 5000;
        });
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string message)
    {
        _snackbar.Add($"❌ {message}", Severity.Error, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 10000;
        });
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string message)
    {
        _snackbar.Add($"ℹ️ {message}", Severity.Info, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 6000;
        });
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string message)
    {
        _snackbar.Add($"⚠️ {message}", Severity.Warning, config =>
        {
            config.ShowCloseIcon = true;
            config.VisibleStateDuration = 8000;
        });
        return Task.CompletedTask;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = await _dialogService.ShowMessageBox(
            title,
            message,
            yesText: "Yes",
            cancelText: "Cancel");
            
        return result == true;
    }
}