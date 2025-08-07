using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClubManagement.Infrastructure.Data;

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
        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var tenantContextFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();
        var recurrenceManager = scope.ServiceProvider.GetRequiredService<IRecurrenceManager>();

        _logger.LogDebug("Starting recurrence maintenance cycle");

        try
        {
            // Get all active tenants
            var activeTenants = await catalogContext.Tenants
                .Where(t => t.Status == ClubManagement.Shared.Models.TenantStatus.Active)
                .ToListAsync();

            _logger.LogDebug("Running recurrence maintenance for {TenantCount} active tenants", activeTenants.Count);

            // Run maintenance for each tenant
            foreach (var tenant in activeTenants)
            {
                try
                {
                    _logger.LogDebug("Running maintenance for tenant: {TenantDomain}", tenant.Domain);
                    
                    // 1. Extend recurring events that need more future occurrences
                    await ExtendRecurrencesForTenant(tenant.Domain, recurrenceManager, tenantContextFactory);

                    // 2. Clean up old completed occurrences for this tenant
                    if (_settings.EnableCleanup)
                    {
                        using var tenantContextForCleanup = await tenantContextFactory.CreateTenantDbContextAsync(tenant.Domain);
                        await recurrenceManager.CleanupOldOccurrencesAsync(tenantContextForCleanup);
                    }

                    // 3. Validate recurrence integrity for this tenant
                    if (_settings.EnableIntegrityCheck)
                    {
                        using var tenantContextForIntegrity = await tenantContextFactory.CreateTenantDbContextAsync(tenant.Domain);
                        await recurrenceManager.ValidateRecurrenceIntegrityAsync(tenantContextForIntegrity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to run maintenance for tenant {TenantDomain}", tenant.Domain);
                    // Continue with other tenants even if one fails
                }
            }

            _logger.LogDebug("Recurrence maintenance cycle completed successfully for all tenants");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recurrence maintenance cycle");
            throw;
        }
    }

    private async Task ExtendRecurrencesForTenant(string tenantDomain, IRecurrenceManager recurrenceManager, ITenantDbContextFactory tenantContextFactory)
    {
        using var tenantContext = await tenantContextFactory.CreateTenantDbContextAsync(tenantDomain);

        // Get all active master events that might need extension
        var activeMasterEvents = await tenantContext.Events
            .Where(e => e.IsRecurringMaster && 
                       e.RecurrenceStatus == ClubManagement.Shared.Models.RecurrenceStatus.Active)
            .Select(e => e.Id)
            .ToListAsync();

        _logger.LogDebug("Checking {Count} active recurring events for extension in tenant {TenantDomain}", activeMasterEvents.Count, tenantDomain);

        foreach (var masterEventId in activeMasterEvents)
        {
            try
            {
                await recurrenceManager.ExtendRecurrenceAsync(masterEventId, tenantContext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extend recurrence for event {EventId} in tenant {TenantDomain}", masterEventId, tenantDomain);
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