using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.DTOs;

namespace ClubManagement.Infrastructure.Services;

public interface IRecurrenceUpdateService
{
    Task<RecurrenceUpdateResult> UpdateRecurrencePatternAsync(
        ClubManagementDbContext tenantContext,
        Guid masterEventId, 
        RecurrencePattern newPattern, 
        RecurrenceUpdateStrategy strategy = RecurrenceUpdateStrategy.PreserveRegistrations);
    
    Task<RecurrenceUpdateResult> PreviewRecurrenceUpdateAsync(
        ClubManagementDbContext tenantContext,
        Guid masterEventId, 
        RecurrencePattern newPattern);
    
    Task<RecurrenceUpdateResult> UpdateSingleOccurrenceAsync(
        ClubManagementDbContext tenantContext,
        Guid occurrenceId, 
        Event updatedEvent);
}

public class RecurrenceUpdateService : IRecurrenceUpdateService
{
    private readonly IRecurrenceManager _recurrenceManager;
    private readonly ILogger<RecurrenceUpdateService> _logger;

    public RecurrenceUpdateService(
        IRecurrenceManager recurrenceManager,
        ILogger<RecurrenceUpdateService> logger)
    {
        _recurrenceManager = recurrenceManager;
        _logger = logger;
    }

    public async Task<RecurrenceUpdateResult> PreviewRecurrenceUpdateAsync(
        ClubManagementDbContext tenantContext,
        Guid masterEventId, 
        RecurrencePattern newPattern)
    {
        var result = new RecurrenceUpdateResult { Success = true };

        try
        {
            var masterEvent = await tenantContext.Events
                .FirstOrDefaultAsync(e => e.Id == masterEventId && e.IsRecurringMaster);

            if (masterEvent == null)
            {
                result.Success = false;
                result.Message = "Master event not found";
                return result;
            }

            // Get current future occurrences
            var currentOccurrences = await tenantContext.Events
                .Include(e => e.Registrations)
                .Where(e => e.MasterEventId == masterEventId && e.StartDateTime > DateTime.UtcNow)
                .ToListAsync();

            // Generate new occurrences based on new pattern
            var tempMasterEvent = new Event
            {
                Id = masterEvent.Id,
                StartDateTime = masterEvent.StartDateTime,
                EndDateTime = masterEvent.EndDateTime,
                Recurrence = newPattern,
                Title = masterEvent.Title,
                Description = masterEvent.Description,
                Type = masterEvent.Type,
                FacilityId = masterEvent.FacilityId,
                InstructorId = masterEvent.InstructorId,
                MaxCapacity = masterEvent.MaxCapacity,
                Price = masterEvent.Price,
                AllowWaitlist = masterEvent.AllowWaitlist,
                SpecialInstructions = masterEvent.SpecialInstructions,
                RequiredEquipment = masterEvent.RequiredEquipment,
                TenantId = masterEvent.TenantId,
                CreatedBy = masterEvent.CreatedBy
            };

            var newOccurrences = await _recurrenceManager.GenerateOccurrencesAsync(
                tempMasterEvent, 
                DateTime.UtcNow.Date, 
                DateTime.UtcNow.AddMonths(6));

            // Analyze impact
            var occurrencesWithRegistrations = currentOccurrences
                .Where(e => e.Registrations.Any())
                .ToList();

            result.OccurrencesDeleted = currentOccurrences.Count;
            result.OccurrencesCreated = newOccurrences.Count;
            result.RegistrationsAffected = occurrencesWithRegistrations
                .Sum(e => e.Registrations.Count);
            result.ConflictingEvents = occurrencesWithRegistrations.Select(e => new ConflictingEventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartDateTime = e.StartDateTime,
                RegistrationCount = e.Registrations.Count,
                MemberNames = e.Registrations.Select(r => $"{r.Member?.User?.FirstName} {r.Member?.User?.LastName}".Trim()).ToList()
            }).ToList();

            if (result.RegistrationsAffected > 0)
            {
                result.Warnings.Add($"{result.RegistrationsAffected} registrations will be affected");
                result.Warnings.Add($"{occurrencesWithRegistrations.Count} events with registrations need handling");
            }

            result.Message = $"Preview: {result.OccurrencesDeleted} occurrences to delete, " +
                           $"{result.OccurrencesCreated} to create, " +
                           $"{result.RegistrationsAffected} registrations affected";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error previewing recurrence update: {ex.Message}";
            _logger.LogError(ex, "Error previewing recurrence update for master event {MasterEventId}", masterEventId);
        }

        return result;
    }

    public async Task<RecurrenceUpdateResult> UpdateRecurrencePatternAsync(
        ClubManagementDbContext tenantContext,
        Guid masterEventId, 
        RecurrencePattern newPattern, 
        RecurrenceUpdateStrategy strategy = RecurrenceUpdateStrategy.PreserveRegistrations)
    {
        var result = new RecurrenceUpdateResult { Success = true };

        using var transaction = await tenantContext.Database.BeginTransactionAsync();
        
        try
        {
            var masterEvent = await tenantContext.Events
                .FirstOrDefaultAsync(e => e.Id == masterEventId && e.IsRecurringMaster);

            if (masterEvent == null)
            {
                result.Success = false;
                result.Message = "Master event not found";
                return result;
            }

            // Get current future occurrences
            var currentOccurrences = await tenantContext.Events
                .Include(e => e.Registrations)
                .Where(e => e.MasterEventId == masterEventId && e.StartDateTime > DateTime.UtcNow)
                .ToListAsync();

            // Separate occurrences with and without registrations
            var occurrencesWithRegistrations = currentOccurrences
                .Where(e => e.Registrations.Any())
                .ToList();
            
            var occurrencesWithoutRegistrations = currentOccurrences
                .Where(e => !e.Registrations.Any())
                .ToList();

            // Handle different strategies
            switch (strategy)
            {
                case RecurrenceUpdateStrategy.PreserveRegistrations:
                    await HandlePreserveRegistrationsStrategy(tenantContext, masterEvent, newPattern, 
                        occurrencesWithRegistrations, occurrencesWithoutRegistrations, result);
                    break;

                case RecurrenceUpdateStrategy.ForceUpdate:
                    await HandleForceUpdateStrategy(tenantContext, masterEvent, newPattern, 
                        currentOccurrences, result);
                    break;

                case RecurrenceUpdateStrategy.CancelConflicts:
                    await HandleCancelConflictsStrategy(tenantContext, masterEvent, newPattern, 
                        occurrencesWithRegistrations, occurrencesWithoutRegistrations, result);
                    break;
            }

            // Update master event pattern
            masterEvent.Recurrence = newPattern;
            masterEvent.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Message = $"Recurrence pattern updated successfully. " +
                           $"Deleted: {result.OccurrencesDeleted}, " +
                           $"Created: {result.OccurrencesCreated}, " +
                           $"Preserved: {result.OccurrencesPreserved}";

            _logger.LogInformation("Updated recurrence pattern for master event {MasterEventId}. " +
                                 "Strategy: {Strategy}, Deleted: {Deleted}, Created: {Created}, Preserved: {Preserved}",
                masterEventId, strategy, result.OccurrencesDeleted, result.OccurrencesCreated, result.OccurrencesPreserved);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Message = $"Error updating recurrence pattern: {ex.Message}";
            _logger.LogError(ex, "Error updating recurrence pattern for master event {MasterEventId}", masterEventId);
        }

        return result;
    }

    public async Task<RecurrenceUpdateResult> UpdateSingleOccurrenceAsync(
        ClubManagementDbContext tenantContext,
        Guid occurrenceId, 
        Event updatedEvent)
    {
        var result = new RecurrenceUpdateResult { Success = true };

        try
        {
            var occurrence = await tenantContext.Events.FindAsync(occurrenceId);
            if (occurrence == null)
            {
                result.Success = false;
                result.Message = "Event occurrence not found";
                return result;
            }

            // Update only this occurrence
            occurrence.Title = updatedEvent.Title;
            occurrence.Description = updatedEvent.Description;
            occurrence.Type = updatedEvent.Type;
            occurrence.StartDateTime = updatedEvent.StartDateTime;
            occurrence.EndDateTime = updatedEvent.EndDateTime;
            occurrence.FacilityId = updatedEvent.FacilityId;
            occurrence.InstructorId = updatedEvent.InstructorId;
            occurrence.MaxCapacity = updatedEvent.MaxCapacity;
            occurrence.Price = updatedEvent.Price;
            occurrence.AllowWaitlist = updatedEvent.AllowWaitlist;
            occurrence.SpecialInstructions = updatedEvent.SpecialInstructions;
            occurrence.RequiredEquipment = updatedEvent.RequiredEquipment;
            occurrence.UpdatedAt = DateTime.UtcNow;

            // Mark as modified from series (optional flag)
            // occurrence.IsModifiedFromSeries = true; // Could add this field later

            await tenantContext.SaveChangesAsync();

            result.Message = "Single occurrence updated successfully";
            
            _logger.LogInformation("Updated single occurrence {OccurrenceId}", occurrenceId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error updating single occurrence: {ex.Message}";
            _logger.LogError(ex, "Error updating single occurrence {OccurrenceId}", occurrenceId);
        }

        return result;
    }

    private async Task HandlePreserveRegistrationsStrategy(
        ClubManagementDbContext tenantContext,
        Event masterEvent,
        RecurrencePattern newPattern,
        List<Event> occurrencesWithRegistrations,
        List<Event> occurrencesWithoutRegistrations,
        RecurrenceUpdateResult result)
    {
        // Delete occurrences without registrations
        tenantContext.Events.RemoveRange(occurrencesWithoutRegistrations);
        result.OccurrencesDeleted = occurrencesWithoutRegistrations.Count;

        // Preserve occurrences with registrations
        result.OccurrencesPreserved = occurrencesWithRegistrations.Count;
        result.RegistrationsAffected = 0; // No registrations affected

        // Generate new occurrences
        var newOccurrences = await _recurrenceManager.GenerateOccurrencesAsync(
            masterEvent, DateTime.UtcNow.Date, DateTime.UtcNow.AddMonths(6));
        
        // Filter out dates that already have preserved occurrences
        var preservedDates = occurrencesWithRegistrations
            .Select(e => e.StartDateTime.Date)
            .ToHashSet();
        
        var filteredNewOccurrences = newOccurrences
            .Where(e => !preservedDates.Contains(e.StartDateTime.Date))
            .ToList();

        await tenantContext.Events.AddRangeAsync(filteredNewOccurrences);
        result.OccurrencesCreated = filteredNewOccurrences.Count;

        if (result.OccurrencesPreserved > 0)
        {
            result.Warnings.Add($"{result.OccurrencesPreserved} events with registrations were preserved and may no longer match the series pattern");
        }
    }

    private async Task HandleForceUpdateStrategy(
        ClubManagementDbContext tenantContext,
        Event masterEvent,
        RecurrencePattern newPattern,
        List<Event> allOccurrences,
        RecurrenceUpdateResult result)
    {
        // Delete all future occurrences
        result.RegistrationsAffected = allOccurrences.Sum(e => e.Registrations.Count);
        tenantContext.Events.RemoveRange(allOccurrences);
        result.OccurrencesDeleted = allOccurrences.Count;

        // Generate new occurrences
        var newOccurrences = await _recurrenceManager.GenerateOccurrencesAsync(
            masterEvent, DateTime.UtcNow.Date, DateTime.UtcNow.AddMonths(6));
        
        await tenantContext.Events.AddRangeAsync(newOccurrences);
        result.OccurrencesCreated = newOccurrences.Count;

        if (result.RegistrationsAffected > 0)
        {
            result.Warnings.Add($"All {result.RegistrationsAffected} registrations were cancelled due to force update");
        }
    }

    private async Task HandleCancelConflictsStrategy(
        ClubManagementDbContext tenantContext,
        Event masterEvent,
        RecurrencePattern newPattern,
        List<Event> occurrencesWithRegistrations,
        List<Event> occurrencesWithoutRegistrations,
        RecurrenceUpdateResult result)
    {
        // Cancel occurrences with registrations
        foreach (var occurrence in occurrencesWithRegistrations)
        {
            occurrence.Status = EventStatus.Cancelled;
            occurrence.UpdatedAt = DateTime.UtcNow;
        }

        // Delete occurrences without registrations
        tenantContext.Events.RemoveRange(occurrencesWithoutRegistrations);
        result.OccurrencesDeleted = occurrencesWithoutRegistrations.Count;
        result.RegistrationsAffected = occurrencesWithRegistrations.Sum(e => e.Registrations.Count);

        // Generate new occurrences
        var newOccurrences = await _recurrenceManager.GenerateOccurrencesAsync(
            masterEvent, DateTime.UtcNow.Date, DateTime.UtcNow.AddMonths(6));
        
        await tenantContext.Events.AddRangeAsync(newOccurrences);
        result.OccurrencesCreated = newOccurrences.Count;

        result.Warnings.Add($"{occurrencesWithRegistrations.Count} events with registrations were cancelled");
        result.Warnings.Add($"{result.RegistrationsAffected} members will need to re-register");
    }
}