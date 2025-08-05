using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClubManagement.Infrastructure.Services;

public class RecurrenceMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurrenceMaintenanceService> _logger;
    private readonly RecurrenceMaintenanceSettings _settings;

    public RecurrenceMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<RecurrenceMaintenanceService> logger,
        IOptions<RecurrenceMaintenanceSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recurrence Maintenance Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync();
                
                // Wait for the configured interval before next run
                await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during recurrence maintenance");
                
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(_settings.ErrorRetryMinutes), stoppingToken);
            }
        }

        _logger.LogInformation("Recurrence Maintenance Service stopped");
    }

    private async Task PerformMaintenanceAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var recurrenceManager = scope.ServiceProvider.GetRequiredService<IRecurrenceManager>();

        _logger.LogDebug("Starting recurrence maintenance cycle");

        try
        {
            // 1. Extend recurring events that need more future occurrences
            await ExtendRecurrences(recurrenceManager);

            // 2. Clean up old completed occurrences
            if (_settings.EnableCleanup)
            {
                await recurrenceManager.CleanupOldOccurrencesAsync();
            }

            // 3. Validate recurrence integrity
            if (_settings.EnableIntegrityCheck)
            {
                await recurrenceManager.ValidateRecurrenceIntegrityAsync();
            }

            _logger.LogDebug("Recurrence maintenance cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recurrence maintenance cycle");
            throw;
        }
    }

    private async Task ExtendRecurrences(IRecurrenceManager recurrenceManager)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClubManagement.Infrastructure.Data.ClubManagementDbContext>();

        // Get all active master events that might need extension
        var activeMasterEvents = await context.Events
            .Where(e => e.IsRecurringMaster && 
                       e.RecurrenceStatus == ClubManagement.Shared.Models.RecurrenceStatus.Active)
            .Select(e => e.Id)
            .ToListAsync();

        _logger.LogDebug("Checking {Count} active recurring events for extension", activeMasterEvents.Count);

        foreach (var masterEventId in activeMasterEvents)
        {
            try
            {
                await recurrenceManager.ExtendRecurrenceAsync(masterEventId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extend recurrence for event {EventId}", masterEventId);
                // Continue with other events even if one fails
            }
        }
    }
}

public class RecurrenceMaintenanceSettings
{
    /// <summary>
    /// How often to run maintenance (in minutes). Default: 60 minutes (1 hour)
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// How long to wait before retrying after an error (in minutes). Default: 15 minutes
    /// </summary>
    public int ErrorRetryMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to enable cleanup of old occurrences. Default: true
    /// </summary>
    public bool EnableCleanup { get; set; } = true;

    /// <summary>
    /// Whether to enable integrity checking. Default: true
    /// </summary>
    public bool EnableIntegrityCheck { get; set; } = true;
}