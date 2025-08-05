using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services;

public interface IRecurrenceManager
{
    Task<List<Event>> GenerateInitialOccurrencesAsync(Event masterEvent);
    Task ExtendRecurrenceAsync(Guid masterEventId);
    Task<List<Event>> GenerateOccurrencesAsync(Event masterEvent, DateTime startDate, DateTime endDate);
    Task CleanupOldOccurrencesAsync();
    Task ValidateRecurrenceIntegrityAsync();
    Task<List<Event>> GetUpcomingOccurrencesAsync(Guid masterEventId, int count = 10);
    Task<Event?> GetSpecificOccurrenceAsync(Guid masterEventId, DateTime targetDate);
}

public class RecurrenceManager : IRecurrenceManager
{
    private readonly ClubManagementDbContext _context;
    private readonly ILogger<RecurrenceManager> _logger;
    private readonly RecurrenceSettings _settings;

    public RecurrenceManager(
        ClubManagementDbContext context,
        ILogger<RecurrenceManager> logger,
        IOptions<RecurrenceSettings> settings)
    {
        _context = context;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<List<Event>> GenerateInitialOccurrencesAsync(Event masterEvent)
    {
        if (masterEvent.Recurrence?.Type == RecurrenceType.None)
            return new List<Event>();

        var endDate = DateTime.UtcNow.AddMonths(_settings.InitialGenerationMonths);
        var occurrences = await GenerateOccurrencesAsync(masterEvent, masterEvent.StartDateTime, endDate);

        // Update master event generation tracking
        masterEvent.LastGeneratedUntil = endDate;
        masterEvent.IsRecurringMaster = true;
        masterEvent.RecurrenceStatus = RecurrenceStatus.Active;

        await _context.Events.AddRangeAsync(occurrences);
        
        _logger.LogInformation("Generated {Count} initial occurrences for event {EventId} until {EndDate}", 
            occurrences.Count, masterEvent.Id, endDate);

        return occurrences;
    }

    public async Task<List<Event>> GenerateOccurrencesAsync(Event masterEvent, DateTime startDate, DateTime endDate)
    {
        if (masterEvent.Recurrence?.Type == RecurrenceType.None)
            return new List<Event>();

        var occurrences = new List<Event>();
        var currentDate = startDate;
        var occurrenceNumber = await GetNextOccurrenceNumber(masterEvent.Id);

        // Safety limit to prevent runaway generation
        var maxOccurrences = _settings.MaxOccurrencesPerGeneration;
        var generatedCount = 0;

        while (currentDate <= endDate && generatedCount < maxOccurrences)
        {
            // Check end conditions
            if (masterEvent.Recurrence.EndDate.HasValue && currentDate > masterEvent.Recurrence.EndDate.Value)
                break;

            if (masterEvent.Recurrence.MaxOccurrences.HasValue && 
                occurrenceNumber > masterEvent.Recurrence.MaxOccurrences.Value)
                break;

            // Create occurrence
            var occurrence = CreateOccurrence(masterEvent, currentDate, occurrenceNumber);
            occurrences.Add(occurrence);

            // Move to next occurrence date
            currentDate = CalculateNextOccurrence(currentDate, masterEvent.Recurrence);
            occurrenceNumber++;
            generatedCount++;
        }

        return occurrences;
    }

    public async Task ExtendRecurrenceAsync(Guid masterEventId)
    {
        var masterEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == masterEventId && e.IsRecurringMaster);

        if (masterEvent?.Recurrence?.Type == RecurrenceType.None || 
            masterEvent?.RecurrenceStatus != RecurrenceStatus.Active)
            return;

        var lastGenerated = masterEvent.LastGeneratedUntil ?? DateTime.UtcNow;
        var futureMonthsRemaining = (lastGenerated - DateTime.UtcNow).Days / 30.0;

        if (futureMonthsRemaining < _settings.MinimumFutureMonths)
        {
            var newEndDate = lastGenerated.AddMonths(_settings.ExtensionBatchMonths);
            var newOccurrences = await GenerateOccurrencesAsync(masterEvent, lastGenerated.AddDays(1), newEndDate);

            if (newOccurrences.Any())
            {
                await _context.Events.AddRangeAsync(newOccurrences);
                masterEvent.LastGeneratedUntil = newEndDate;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Extended recurrence for event {EventId} with {Count} new occurrences until {EndDate}",
                    masterEventId, newOccurrences.Count, newEndDate);
            }
        }
    }

    public async Task CleanupOldOccurrencesAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-_settings.HistoryRetentionMonths);

        var oldOccurrences = await _context.Events
            .Where(e => e.MasterEventId != null &&
                       e.EndDateTime < cutoffDate &&
                       e.Status == EventStatus.Completed)
            .ToListAsync();

        if (oldOccurrences.Any())
        {
            _context.Events.RemoveRange(oldOccurrences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old event occurrences before {CutoffDate}",
                oldOccurrences.Count, cutoffDate);
        }
    }

    public async Task ValidateRecurrenceIntegrityAsync()
    {
        // Find master events with missing occurrences
        var masterEvents = await _context.Events
            .Where(e => e.IsRecurringMaster && e.RecurrenceStatus == RecurrenceStatus.Active)
            .ToListAsync();

        foreach (var masterEvent in masterEvents)
        {
            var occurrenceCount = await _context.Events
                .CountAsync(e => e.MasterEventId == masterEvent.Id);

            if (occurrenceCount == 0)
            {
                _logger.LogWarning("Master event {EventId} has no occurrences - may need regeneration", masterEvent.Id);
            }
        }

        // Find orphaned occurrences
        var orphanedCount = await _context.Events
            .Where(e => e.MasterEventId != null)
            .Where(e => !_context.Events.Any(m => m.Id == e.MasterEventId))
            .CountAsync();

        if (orphanedCount > 0)
        {
            _logger.LogWarning("Found {Count} orphaned event occurrences", orphanedCount);
        }
    }

    public async Task<List<Event>> GetUpcomingOccurrencesAsync(Guid masterEventId, int count = 10)
    {
        return await _context.Events
            .Where(e => e.MasterEventId == masterEventId && e.StartDateTime >= DateTime.UtcNow)
            .OrderBy(e => e.StartDateTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Event?> GetSpecificOccurrenceAsync(Guid masterEventId, DateTime targetDate)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.MasterEventId == masterEventId && 
                                    e.StartDateTime.Date == targetDate.Date);
    }

    private Event CreateOccurrence(Event masterEvent, DateTime occurrenceDate, int occurrenceNumber)
    {
        var duration = masterEvent.EndDateTime - masterEvent.StartDateTime;
        
        // Ensure UTC dates for PostgreSQL compatibility
        var utcOccurrenceDate = occurrenceDate.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(occurrenceDate, DateTimeKind.Utc) 
            : occurrenceDate.ToUniversalTime();
            
        var utcEndDate = utcOccurrenceDate.Add(duration);
        
        return new Event
        {
            Id = Guid.NewGuid(),
            TenantId = masterEvent.TenantId,
            Title = masterEvent.Title,
            Description = masterEvent.Description,
            Type = masterEvent.Type,
            StartDateTime = utcOccurrenceDate,
            EndDateTime = utcEndDate,
            FacilityId = masterEvent.FacilityId,
            InstructorId = masterEvent.InstructorId,
            MaxCapacity = masterEvent.MaxCapacity,
            CurrentEnrollment = 0, // Start with no enrollments
            Price = masterEvent.Price,
            Status = EventStatus.Scheduled,
            RegistrationDeadline = CalculateRegistrationDeadline(utcOccurrenceDate, masterEvent.RegistrationDeadline, masterEvent.StartDateTime),
            CancellationDeadline = CalculateCancellationDeadline(utcOccurrenceDate, masterEvent.CancellationDeadline, masterEvent.StartDateTime),
            CancellationPolicy = masterEvent.CancellationPolicy,
            AllowWaitlist = masterEvent.AllowWaitlist,
            SpecialInstructions = masterEvent.SpecialInstructions,
            RequiredEquipment = masterEvent.RequiredEquipment,
            
            // Recurrence-specific properties
            MasterEventId = masterEvent.Id,
            IsRecurringMaster = false,
            OccurrenceNumber = occurrenceNumber,
            Recurrence = null, // Occurrences don't store recurrence pattern
            
            // Audit properties
            CreatedAt = DateTime.UtcNow,
            CreatedBy = masterEvent.CreatedBy ?? "RecurrenceManager"
        };
    }

    private DateTime CalculateNextOccurrence(DateTime currentDate, RecurrencePattern recurrence)
    {
        return recurrence.Type switch
        {
            RecurrenceType.Daily => currentDate.AddDays(recurrence.Interval),
            RecurrenceType.Weekly => CalculateNextWeeklyOccurrence(currentDate, recurrence),
            RecurrenceType.Monthly => currentDate.AddMonths(recurrence.Interval),
            RecurrenceType.Yearly => currentDate.AddYears(recurrence.Interval),
            _ => throw new ArgumentException($"Unsupported recurrence type: {recurrence.Type}")
        };
    }

    private DateTime CalculateNextWeeklyOccurrence(DateTime currentDate, RecurrencePattern recurrence)
    {
        if (!recurrence.DaysOfWeek.Any())
        {
            // If no specific days specified, use the original day of week
            return currentDate.AddDays(7 * recurrence.Interval);
        }

        // Find next occurrence based on specified days of week
        var nextDate = currentDate.AddDays(1);
        var weekOffset = 0;

        while (weekOffset < 4) // Safety limit - check up to 4 weeks ahead
        {
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var candidateDate = nextDate.AddDays(dayOffset + (weekOffset * 7 * recurrence.Interval));
                if (recurrence.DaysOfWeek.Contains(candidateDate.DayOfWeek))
                {
                    return candidateDate;
                }
            }
            weekOffset++;
        }

        // Fallback to weekly interval
        return currentDate.AddDays(7 * recurrence.Interval);
    }

    private DateTime? CalculateRegistrationDeadline(DateTime occurrenceDate, DateTime? masterDeadline, DateTime masterStartDate)
    {
        if (!masterDeadline.HasValue)
            return null;

        // Calculate the offset from master event start to deadline
        var deadlineOffset = masterDeadline.Value - masterStartDate;
        
        // Apply same offset to occurrence and ensure UTC
        var calculatedDeadline = occurrenceDate.Add(deadlineOffset);
        return calculatedDeadline.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(calculatedDeadline, DateTimeKind.Utc) 
            : calculatedDeadline.ToUniversalTime();
    }

    private DateTime? CalculateCancellationDeadline(DateTime occurrenceDate, DateTime? masterDeadline, DateTime masterStartDate)
    {
        if (!masterDeadline.HasValue)
            return null;

        var deadlineOffset = masterDeadline.Value - masterStartDate;
        var calculatedDeadline = occurrenceDate.Add(deadlineOffset);
        return calculatedDeadline.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(calculatedDeadline, DateTimeKind.Utc) 
            : calculatedDeadline.ToUniversalTime();
    }

    private async Task<int> GetNextOccurrenceNumber(Guid masterEventId)
    {
        var lastOccurrence = await _context.Events
            .Where(e => e.MasterEventId == masterEventId)
            .OrderByDescending(e => e.OccurrenceNumber)
            .FirstOrDefaultAsync();

        return (lastOccurrence?.OccurrenceNumber ?? 0) + 1;
    }
}

public class RecurrenceSettings
{
    public int InitialGenerationMonths { get; set; } = 6;
    public int MinimumFutureMonths { get; set; } = 3;
    public int ExtensionBatchMonths { get; set; } = 6;
    public int MaxOccurrencesPerGeneration { get; set; } = 500;
    public int HistoryRetentionMonths { get; set; } = 12;
}