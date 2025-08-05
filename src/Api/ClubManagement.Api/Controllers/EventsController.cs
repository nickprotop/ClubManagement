using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Infrastructure.Authorization;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Api.Extensions;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly ClubManagementDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IEventAuthorizationService _authService;
    private readonly IRecurrenceManager _recurrenceManager;
    private readonly IRecurrenceUpdateService _recurrenceUpdateService;

    public EventsController(ClubManagementDbContext context, ITenantService tenantService, IEventAuthorizationService authService, IRecurrenceManager recurrenceManager, IRecurrenceUpdateService recurrenceUpdateService)
    {
        _context = context;
        _tenantService = tenantService;
        _authService = authService;
        _recurrenceManager = recurrenceManager;
        _recurrenceUpdateService = recurrenceUpdateService;
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<EventPermissions>>> GetEventPermissions(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<EventPermissions>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var permissions = await _authService.GetEventPermissionsAsync(userId, id);
            return Ok(ApiResponse<EventPermissions>.SuccessResult(permissions));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventPermissions>.ErrorResult($"Error retrieving event permissions: {ex.Message}"));
        }
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<EventPermissions>>> GetGeneralEventPermissions()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<EventPermissions>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var permissions = await _authService.GetEventPermissionsAsync(userId);
            return Ok(ApiResponse<EventPermissions>.SuccessResult(permissions));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<EventPermissions>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventPermissions>.ErrorResult($"Error retrieving event permissions: {ex.Message}"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EventListDto>>>> GetEvents([FromQuery] EventSearchRequest request)
    {
        try
        {
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<EventListDto>>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var query = _context.Events
                .Include(e => e.Facility)
                .Include(e => e.Instructor)
                .Where(e => !e.IsRecurringMaster) // Exclude master events from list, only show actual occurrences
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(e => 
                    e.Title.ToLower().Contains(searchLower) ||
                    e.Description.ToLower().Contains(searchLower));
            }

            if (request.Type.HasValue)
                query = query.Where(e => e.Type == request.Type.Value);

            if (request.Status.HasValue)
                query = query.Where(e => e.Status == request.Status.Value);

            if (request.FacilityId.HasValue)
                query = query.Where(e => e.FacilityId == request.FacilityId.Value);

            if (request.InstructorId.HasValue)
                query = query.Where(e => e.InstructorId == request.InstructorId.Value);

            if (request.StartDate.HasValue)
                query = query.Where(e => e.StartDateTime >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(e => e.StartDateTime <= request.EndDate.Value);

            var totalCount = await query.CountAsync();
            
            var events = await query
                .OrderBy(e => e.StartDateTime)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new EventListDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Type = e.Type,
                    StartDateTime = e.StartDateTime,
                    EndDateTime = e.EndDateTime,
                    FacilityName = e.Facility != null ? e.Facility.Name : null,
                    InstructorName = e.Instructor != null ? $"{e.Instructor.FirstName} {e.Instructor.LastName}" : null,
                    MaxCapacity = e.MaxCapacity,
                    CurrentEnrollment = e.CurrentEnrollment,
                    Price = e.Price,
                    Status = e.Status,
                    IsRecurringMaster = e.IsRecurringMaster,
                    MasterEventId = e.MasterEventId,
                    OccurrenceNumber = e.OccurrenceNumber,
                    RecurrenceType = e.Recurrence != null ? e.Recurrence.Type : null
                })
                .ToListAsync();

            var result = new PagedResult<EventListDto>
            {
                Items = events,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Ok(ApiResponse<PagedResult<EventListDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<EventListDto>>.ErrorResult($"Error retrieving events: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EventDto>>> GetEvent(Guid id)
    {
        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Facility)
                .Include(e => e.Instructor)
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
                return NotFound(ApiResponse<EventDto>.ErrorResult("Event not found"));

            var eventDto = new EventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Type = eventEntity.Type,
                StartDateTime = eventEntity.StartDateTime,
                EndDateTime = eventEntity.EndDateTime,
                FacilityId = eventEntity.FacilityId,
                FacilityName = eventEntity.Facility?.Name,
                InstructorId = eventEntity.InstructorId,
                InstructorName = eventEntity.Instructor != null ? $"{eventEntity.Instructor.FirstName} {eventEntity.Instructor.LastName}" : null,
                MaxCapacity = eventEntity.MaxCapacity,
                CurrentEnrollment = eventEntity.CurrentEnrollment,
                Price = eventEntity.Price,
                Status = eventEntity.Status,
                Recurrence = eventEntity.Recurrence,
                RegistrationDeadline = eventEntity.RegistrationDeadline,
                CancellationDeadline = eventEntity.CancellationDeadline,
                CancellationPolicy = eventEntity.CancellationPolicy,
                AllowWaitlist = eventEntity.AllowWaitlist,
                SpecialInstructions = eventEntity.SpecialInstructions,
                RequiredEquipment = eventEntity.RequiredEquipment,
                IsRecurringMaster = eventEntity.IsRecurringMaster,
                MasterEventId = eventEntity.MasterEventId,
                OccurrenceNumber = eventEntity.OccurrenceNumber,
                Registrations = eventEntity.Registrations.Select(r => new EventRegistrationDto
                {
                    Id = r.Id,
                    MemberId = r.MemberId,
                    MemberName = $"{r.Member.User.FirstName} {r.Member.User.LastName}",
                    Status = r.Status,
                    RegisteredAt = r.RegisteredAt,
                    Notes = r.Notes
                }).ToList()
            };

            return Ok(ApiResponse<EventDto>.SuccessResult(eventDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventDto>.ErrorResult($"Error retrieving event: {ex.Message}"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EventDto>>> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.Create);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            // Validate facility if specified
            if (request.FacilityId.HasValue)
            {
                var facility = await _context.Facilities.FindAsync(request.FacilityId.Value);
                if (facility == null)
                    return BadRequest(ApiResponse<EventDto>.ErrorResult("Facility not found"));
            }

            // Validate instructor if specified
            if (request.InstructorId.HasValue)
            {
                var instructor = await _context.Users.FindAsync(request.InstructorId.Value);
                if (instructor == null)
                    return BadRequest(ApiResponse<EventDto>.ErrorResult("Instructor not found"));
            }

            // Build recurrence pattern from UI helper properties
            RecurrencePattern? recurrencePattern = null;
            if (request.RecurrenceType != RecurrenceType.None)
            {
                recurrencePattern = new RecurrencePattern
                {
                    Type = request.RecurrenceType,
                    Interval = request.RecurrenceInterval,
                    DaysOfWeek = request.DaysOfWeek,
                    EndDate = request.RecurrenceEndDate,
                    MaxOccurrences = request.MaxOccurrences
                };
            }

            var eventEntity = new Event
            {
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                FacilityId = request.FacilityId,
                InstructorId = request.InstructorId,
                MaxCapacity = request.MaxCapacity,
                Price = request.Price,
                Status = EventStatus.Scheduled,
                Recurrence = recurrencePattern,
                RegistrationDeadline = request.RegistrationDeadline,
                CancellationDeadline = request.CancellationDeadline,
                CancellationPolicy = request.CancellationPolicy,
                AllowWaitlist = request.AllowWaitlist,
                SpecialInstructions = request.SpecialInstructions,
                RequiredEquipment = request.RequiredEquipment ?? new List<string>(),
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            // Handle recurrence creation
            if (recurrencePattern?.Type != RecurrenceType.None)
            {
                var occurrences = await _recurrenceManager.GenerateInitialOccurrencesAsync(eventEntity);
                await _context.SaveChangesAsync();
            }

            // Return the created event
            var createdEvent = await GetEvent(eventEntity.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = eventEntity.Id }, createdEvent.Value);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventDto>.ErrorResult($"Error creating event: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EventDto>>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.Edit, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
                
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<EventDto>.ErrorResult("Event not found"));

            // Validate facility if specified
            if (request.FacilityId.HasValue)
            {
                var facility = await _context.Facilities.FindAsync(request.FacilityId.Value);
                if (facility == null)
                    return BadRequest(ApiResponse<EventDto>.ErrorResult("Facility not found"));
            }

            // Validate instructor if specified
            if (request.InstructorId.HasValue)
            {
                var instructor = await _context.Users.FindAsync(request.InstructorId.Value);
                if (instructor == null)
                    return BadRequest(ApiResponse<EventDto>.ErrorResult("Instructor not found"));
            }

            // Handle different update scenarios
            if (eventEntity.IsRecurringMaster)
            {
                // This is a master event - recurrence changes affect the entire series
                // This should not happen from the UI directly - user should use the series update endpoint
                return BadRequest(ApiResponse<EventDto>.ErrorResult(
                    "Cannot update master event directly. Use the series update endpoint instead."));
            }
            else if (eventEntity.MasterEventId.HasValue)
            {
                // This is an occurrence - update only this occurrence
                var updatedEvent = new Event
                {
                    Title = request.Title,
                    Description = request.Description,
                    Type = request.Type,
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    FacilityId = request.FacilityId,
                    InstructorId = request.InstructorId,
                    MaxCapacity = request.MaxCapacity,
                    Price = request.Price,
                    AllowWaitlist = request.AllowWaitlist,
                    SpecialInstructions = request.SpecialInstructions,
                    RequiredEquipment = request.RequiredEquipment ?? new List<string>()
                };

                var updateResult = await _recurrenceUpdateService.UpdateSingleOccurrenceAsync(id, updatedEvent);
                
                if (!updateResult.Success)
                    return BadRequest(ApiResponse<EventDto>.ErrorResult(updateResult.Message));

                var result = await GetEvent(id);
                return result;
            }
            else
            {
                // This is a regular (non-recurring) event - update normally
                eventEntity.Title = request.Title;
                eventEntity.Description = request.Description;
                eventEntity.Type = request.Type;
                eventEntity.StartDateTime = request.StartDateTime;
                eventEntity.EndDateTime = request.EndDateTime;
                eventEntity.FacilityId = request.FacilityId;
                eventEntity.InstructorId = request.InstructorId;
                eventEntity.MaxCapacity = request.MaxCapacity;
                eventEntity.Price = request.Price;
                eventEntity.RegistrationDeadline = request.RegistrationDeadline;
                eventEntity.CancellationDeadline = request.CancellationDeadline;
                eventEntity.CancellationPolicy = request.CancellationPolicy;
                eventEntity.AllowWaitlist = request.AllowWaitlist;
                eventEntity.SpecialInstructions = request.SpecialInstructions;
                eventEntity.RequiredEquipment = request.RequiredEquipment ?? new List<string>();
                eventEntity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedEvent = await GetEvent(id);
                return updatedEvent;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventDto>.ErrorResult($"Error updating event: {ex.Message}"));
        }
    }

    // Recurrence Series Management Endpoints
    
    [HttpGet("{id}/master")]
    public async Task<ActionResult<ApiResponse<EventDto>>> GetMasterEvent(Guid id)
    {
        try
        {
            // Get the master event for this occurrence
            var occurrence = await _context.Events.FindAsync(id);
            if (occurrence == null)
                return NotFound(ApiResponse<EventDto>.ErrorResult("Event not found"));

            Guid masterEventId = occurrence.IsRecurringMaster ? occurrence.Id : occurrence.MasterEventId ?? occurrence.Id;
            return await GetEvent(masterEventId);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventDto>.ErrorResult($"Error retrieving master event: {ex.Message}"));
        }
    }

    [HttpPost("{id}/recurrence/preview")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<RecurrenceUpdateResult>>> PreviewRecurrenceUpdate(
        Guid id, [FromBody] RecurrencePattern newPattern)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.Edit, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Get master event ID
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<RecurrenceUpdateResult>.ErrorResult("Event not found"));

            var masterEventId = eventEntity.IsRecurringMaster ? eventEntity.Id : eventEntity.MasterEventId;
            if (!masterEventId.HasValue)
                return BadRequest(ApiResponse<RecurrenceUpdateResult>.ErrorResult("Event is not part of a recurring series"));

            var result = await _recurrenceUpdateService.PreviewRecurrenceUpdateAsync(masterEventId.Value, newPattern);
            return Ok(ApiResponse<RecurrenceUpdateResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RecurrenceUpdateResult>.ErrorResult($"Error previewing recurrence update: {ex.Message}"));
        }
    }

    [HttpPut("{id}/recurrence")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<RecurrenceUpdateResult>>> UpdateRecurrenceSeries(
        Guid id, [FromBody] UpdateRecurrenceRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.Edit, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Get master event ID
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<RecurrenceUpdateResult>.ErrorResult("Event not found"));

            var masterEventId = eventEntity.IsRecurringMaster ? eventEntity.Id : eventEntity.MasterEventId;
            if (!masterEventId.HasValue)
                return BadRequest(ApiResponse<RecurrenceUpdateResult>.ErrorResult("Event is not part of a recurring series"));

            // Update the master event details first
            var masterEvent = await _context.Events.FindAsync(masterEventId.Value);
            if (masterEvent != null)
            {
                masterEvent.Title = request.Title;
                masterEvent.Description = request.Description;
                masterEvent.Type = request.Type;
                masterEvent.StartDateTime = request.StartDateTime;
                masterEvent.EndDateTime = request.EndDateTime;
                masterEvent.FacilityId = request.FacilityId;
                masterEvent.InstructorId = request.InstructorId;
                masterEvent.MaxCapacity = request.MaxCapacity;
                masterEvent.Price = request.Price;
                masterEvent.AllowWaitlist = request.AllowWaitlist;
                masterEvent.SpecialInstructions = request.SpecialInstructions;
                masterEvent.RequiredEquipment = request.RequiredEquipment ?? new List<string>();
                masterEvent.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }

            // Update the recurrence pattern
            var result = await _recurrenceUpdateService.UpdateRecurrencePatternAsync(
                masterEventId.Value, 
                request.RecurrencePattern, 
                request.UpdateStrategy);

            return Ok(ApiResponse<RecurrenceUpdateResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RecurrenceUpdateResult>.ErrorResult($"Error updating recurrence series: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEvent(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.Delete, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Event not found"));

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Event deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error deleting event: {ex.Message}"));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEventStatus(Guid id, [FromBody] EventStatus status)
    {
        try
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Event not found"));

            eventEntity.Status = status;
            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Event status updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error updating event status: {ex.Message}"));
        }
    }

    // Event Registration Endpoints
    [HttpPost("{id}/register")]
    public async Task<ActionResult<ApiResponse<EventRegistrationDto>>> RegisterForEvent(Guid id, [FromBody] EventRegistrationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var action = request.MemberId != null ? EventAction.RegisterOthers : EventAction.RegisterSelf;
            var authResult = await _authService.CheckAuthorizationAsync(userId, action, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<EventRegistrationDto>.ErrorResult("Event not found"));

            // Determine member ID
            Guid memberId;
            if (request.MemberId.HasValue)
            {
                memberId = request.MemberId.Value;
            }
            else
            {
                var userMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
                if (userMember == null)
                    return BadRequest(ApiResponse<EventRegistrationDto>.ErrorResult("User is not a registered member"));
                memberId = userMember.Id;
            }

            var member = await _context.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == memberId);
            if (member == null)
                return BadRequest(ApiResponse<EventRegistrationDto>.ErrorResult("Member not found"));

            // Check if already registered
            var existingRegistration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.MemberId == memberId);
            
            if (existingRegistration != null)
                return BadRequest(ApiResponse<EventRegistrationDto>.ErrorResult("Member is already registered for this event"));

            // Check capacity
            var currentRegistrations = await _context.EventRegistrations
                .CountAsync(r => r.EventId == id && r.Status == RegistrationStatus.Confirmed);

            var status = currentRegistrations < eventEntity.MaxCapacity 
                ? RegistrationStatus.Confirmed 
                : (eventEntity.AllowWaitlist ? RegistrationStatus.Waitlisted : RegistrationStatus.Declined);

            if (status == RegistrationStatus.Declined)
                return BadRequest(ApiResponse<EventRegistrationDto>.ErrorResult("Event is full and waitlist is not allowed"));

            var registration = new EventRegistration
            {
                EventId = id,
                MemberId = memberId,
                RegisteredByUserId = userId,
                Status = status,
                RegisteredAt = DateTime.UtcNow,
                Notes = request.Notes,
                IsWaitlisted = status == RegistrationStatus.Waitlisted
            };

            if (status == RegistrationStatus.Waitlisted)
            {
                var waitlistCount = await _context.EventRegistrations
                    .CountAsync(r => r.EventId == id && r.Status == RegistrationStatus.Waitlisted);
                registration.WaitlistPosition = waitlistCount + 1;
            }

            _context.EventRegistrations.Add(registration);

            // Update enrollment count
            if (status == RegistrationStatus.Confirmed)
            {
                eventEntity.CurrentEnrollment++;
            }

            await _context.SaveChangesAsync();

            var registrationDto = new EventRegistrationDto
            {
                Id = registration.Id,
                MemberId = registration.MemberId,
                MemberName = $"{member.User.FirstName} {member.User.LastName}",
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                Notes = registration.Notes,
                IsWaitlisted = registration.IsWaitlisted,
                WaitlistPosition = registration.WaitlistPosition
            };

            var message = status == RegistrationStatus.Confirmed 
                ? "Successfully registered for event" 
                : $"Added to waitlist (position {registration.WaitlistPosition})";

            return Ok(ApiResponse<EventRegistrationDto>.SuccessResult(registrationDto, message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventRegistrationDto>.ErrorResult($"Error registering for event: {ex.Message}"));
        }
    }

    [HttpGet("{id}/registrations")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<List<EventRegistrationDto>>>> GetEventRegistrations(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.ViewRegistrations, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var registrations = await _context.EventRegistrations
                .Include(r => r.Member)
                    .ThenInclude(m => m.User)
                .Include(r => r.RegisteredByUser)
                .Include(r => r.CheckedInByUser)
                .Where(r => r.EventId == id)
                .OrderBy(r => r.RegisteredAt)
                .Select(r => new EventRegistrationDto
                {
                    Id = r.Id,
                    MemberId = r.MemberId,
                    MemberName = $"{r.Member.User.FirstName} {r.Member.User.LastName}",
                    Status = r.Status,
                    RegisteredAt = r.RegisteredAt,
                    RegisteredByName = r.RegisteredByUser != null ? $"{r.RegisteredByUser.FirstName} {r.RegisteredByUser.LastName}" : null,
                    Notes = r.Notes,
                    IsWaitlisted = r.IsWaitlisted,
                    WaitlistPosition = r.WaitlistPosition,
                    CheckedInAt = r.CheckedInAt,
                    CheckedInByName = r.CheckedInByUser != null ? $"{r.CheckedInByUser.FirstName} {r.CheckedInByUser.LastName}" : null,
                    NoShow = r.NoShow
                })
                .ToListAsync();

            return Ok(ApiResponse<List<EventRegistrationDto>>.SuccessResult(registrations));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<EventRegistrationDto>>.ErrorResult($"Error retrieving registrations: {ex.Message}"));
        }
    }

    [HttpGet("{id}/registrations/my-status")]
    public async Task<ActionResult<ApiResponse<EventRegistrationDto>>> GetUserRegistrationStatus(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var userMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            
            // If user doesn't have a member profile (e.g., staff users), return a meaningful response
            if (userMember == null)
                return NotFound(ApiResponse<EventRegistrationDto>.ErrorResult("User is not a member and cannot have event registrations"));

            var registration = await _context.EventRegistrations
                .Include(r => r.Member)
                    .ThenInclude(m => m.User)
                .Include(r => r.RegisteredByUser)
                .Include(r => r.CheckedInByUser)
                .FirstOrDefaultAsync(r => r.EventId == id && r.MemberId == userMember.Id);

            if (registration == null)
                return NotFound(ApiResponse<EventRegistrationDto>.ErrorResult("User is not registered for this event"));

            var registrationDto = new EventRegistrationDto
            {
                Id = registration.Id,
                MemberId = registration.MemberId,
                MemberName = $"{registration.Member.User.FirstName} {registration.Member.User.LastName}",
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                RegisteredByName = registration.RegisteredByUser?.Email ?? "System",
                CheckedInAt = registration.CheckedInAt,
                CheckedInByName = registration.CheckedInByUser?.Email,
                NoShow = registration.NoShow,
                IsWaitlisted = registration.Status == RegistrationStatus.Waitlisted,
                WaitlistPosition = registration.WaitlistPosition,
                Notes = registration.Notes
            };

            return Ok(ApiResponse<EventRegistrationDto>.SuccessResult(registrationDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventRegistrationDto>.ErrorResult($"Error retrieving user registration status: {ex.Message}"));
        }
    }

    [HttpPut("{id}/registrations/{registrationId}")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EventRegistrationDto>>> UpdateRegistration(Guid id, Guid registrationId, [FromBody] UpdateRegistrationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.ModifyRegistrations, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var registration = await _context.EventRegistrations
                .Include(r => r.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.EventId == id);

            if (registration == null)
                return NotFound(ApiResponse<EventRegistrationDto>.ErrorResult("Registration not found"));

            registration.Status = request.Status;
            registration.Notes = request.Notes;
            registration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var registrationDto = new EventRegistrationDto
            {
                Id = registration.Id,
                MemberId = registration.MemberId,
                MemberName = $"{registration.Member.User.FirstName} {registration.Member.User.LastName}",
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                Notes = registration.Notes,
                IsWaitlisted = registration.IsWaitlisted,
                WaitlistPosition = registration.WaitlistPosition,
                CheckedInAt = registration.CheckedInAt,
                NoShow = registration.NoShow
            };

            return Ok(ApiResponse<EventRegistrationDto>.SuccessResult(registrationDto, "Registration updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventRegistrationDto>.ErrorResult($"Error updating registration: {ex.Message}"));
        }
    }

    [HttpDelete("{id}/registrations/{registrationId}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelRegistration(Guid id, Guid registrationId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            
            // Get registration to check ownership
            var registration = await _context.EventRegistrations
                .Include(r => r.Member)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.EventId == id);

            if (registration == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Registration not found"));

            // Check if user can cancel this registration
            var isSelfCancellation = registration.Member.UserId == userId;
            var action = isSelfCancellation ? EventAction.RegisterSelf : EventAction.ModifyRegistrations;
            
            var authResult = await _authService.CheckAuthorizationAsync(userId, action, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Event not found"));

            // Check cancellation deadline
            if (eventEntity.CancellationDeadline.HasValue && DateTime.UtcNow > eventEntity.CancellationDeadline)
                return BadRequest(ApiResponse<bool>.ErrorResult("Cancellation deadline has passed"));

            registration.Status = RegistrationStatus.Cancelled;
            registration.UpdatedAt = DateTime.UtcNow;

            // Update enrollment count if was confirmed
            if (registration.Status == RegistrationStatus.Confirmed)
            {
                eventEntity.CurrentEnrollment = Math.Max(0, eventEntity.CurrentEnrollment - 1);
            }

            await _context.SaveChangesAsync();

            // TODO: Handle waitlist promotion logic
            await PromoteFromWaitlistAsync(id);

            return Ok(ApiResponse<bool>.SuccessResult(true, "Registration cancelled successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error cancelling registration: {ex.Message}"));
        }
    }

    // Event Check-in Endpoints
    [HttpPost("{id}/checkin/{memberId}")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckInMember(Guid id, Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.CheckInOthers, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.MemberId == memberId && r.Status == RegistrationStatus.Confirmed);

            if (registration == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Registration not found or not confirmed"));

            if (registration.CheckedInAt.HasValue)
                return BadRequest(ApiResponse<bool>.ErrorResult("Member is already checked in"));

            registration.CheckedInAt = DateTime.UtcNow;
            registration.CheckedInByUserId = userId;
            registration.NoShow = false;
            registration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Member checked in successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error checking in member: {ex.Message}"));
        }
    }

    [HttpPost("{id}/checkin/self")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckInSelf(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.CheckInSelf, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("User is not a registered member"));

            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.MemberId == member.Id && r.Status == RegistrationStatus.Confirmed);

            if (registration == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Registration not found or not confirmed"));

            if (registration.CheckedInAt.HasValue)
                return BadRequest(ApiResponse<bool>.ErrorResult("You are already checked in"));

            registration.CheckedInAt = DateTime.UtcNow;
            registration.CheckedInByUserId = userId;
            registration.NoShow = false;
            registration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Checked in successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error checking in: {ex.Message}"));
        }
    }

    [HttpDelete("{id}/checkin/{memberId}")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UndoCheckIn(Guid id, Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.CheckInOthers, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.MemberId == memberId);

            if (registration == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Registration not found"));

            if (!registration.CheckedInAt.HasValue)
                return BadRequest(ApiResponse<bool>.ErrorResult("Member is not checked in"));

            registration.CheckedInAt = null;
            registration.CheckedInByUserId = null;
            registration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Check-in undone successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error undoing check-in: {ex.Message}"));
        }
    }

    [HttpPost("{id}/bulk-checkin")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<BulkCheckInResult>>> BulkCheckIn(Guid id, [FromBody] BulkCheckInRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.CheckInOthers, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var registrations = await _context.EventRegistrations
                .Where(r => r.EventId == id && request.MemberIds.Contains(r.MemberId) && r.Status == RegistrationStatus.Confirmed)
                .ToListAsync();

            var result = new BulkCheckInResult
            {
                TotalRequested = request.MemberIds.Count,
                SuccessfulCheckIns = 0,
                AlreadyCheckedIn = 0,
                NotFound = 0,
                Errors = new List<string>()
            };

            foreach (var memberId in request.MemberIds)
            {
                var registration = registrations.FirstOrDefault(r => r.MemberId == memberId);
                
                if (registration == null)
                {
                    result.NotFound++;
                    result.Errors.Add($"Registration not found for member {memberId}");
                    continue;
                }

                if (registration.CheckedInAt.HasValue)
                {
                    result.AlreadyCheckedIn++;
                    continue;
                }

                registration.CheckedInAt = DateTime.UtcNow;
                registration.CheckedInByUserId = userId;
                registration.NoShow = false;
                registration.UpdatedAt = DateTime.UtcNow;
                result.SuccessfulCheckIns++;
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<BulkCheckInResult>.SuccessResult(result, $"Bulk check-in completed: {result.SuccessfulCheckIns} successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<BulkCheckInResult>.ErrorResult($"Error performing bulk check-in: {ex.Message}"));
        }
    }

    [HttpGet("{id}/checkin-status")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<CheckInStatusDto>>> GetCheckInStatus(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.ViewRegistrations, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<CheckInStatusDto>.ErrorResult("Event not found"));

            var registrations = await _context.EventRegistrations
                .Where(r => r.EventId == id && r.Status == RegistrationStatus.Confirmed)
                .ToListAsync();

            var checkedInCount = registrations.Count(r => r.CheckedInAt.HasValue);
            var noShowCount = registrations.Count(r => !r.CheckedInAt.HasValue && DateTime.UtcNow > eventEntity.StartDateTime.AddMinutes(30));

            var status = new CheckInStatusDto
            {
                EventId = id,
                TotalRegistered = registrations.Count,
                CheckedIn = checkedInCount,
                NotCheckedIn = registrations.Count - checkedInCount,
                NoShows = noShowCount,
                CheckInStartTime = eventEntity.StartDateTime.AddHours(-1),
                CheckInEndTime = eventEntity.StartDateTime.AddMinutes(30)
            };

            return Ok(ApiResponse<CheckInStatusDto>.SuccessResult(status));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<CheckInStatusDto>.ErrorResult($"Error retrieving check-in status: {ex.Message}"));
        }
    }

    // Event Status Management
    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelEvent(Guid id, [FromBody] CancelEventRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.CancelEvent, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Event not found"));

            if (eventEntity.Status == EventStatus.Cancelled)
                return BadRequest(ApiResponse<bool>.ErrorResult("Event is already cancelled"));

            if (eventEntity.Status == EventStatus.Completed)
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot cancel completed events"));

            eventEntity.Status = EventStatus.Cancelled;
            eventEntity.UpdatedAt = DateTime.UtcNow;

            // Cancel all confirmed registrations
            var registrations = await _context.EventRegistrations
                .Where(r => r.EventId == id && r.Status != RegistrationStatus.Cancelled)
                .ToListAsync();

            foreach (var registration in registrations)
            {
                registration.Status = RegistrationStatus.Cancelled;
                registration.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // TODO: Send cancellation notifications to registered users

            return Ok(ApiResponse<bool>.SuccessResult(true, $"Event cancelled successfully. {registrations.Count} registrations were cancelled."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error cancelling event: {ex.Message}"));
        }
    }

    [HttpPatch("{id}/reschedule")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> RescheduleEvent(Guid id, [FromBody] RescheduleEventRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var authResult = await _authService.CheckAuthorizationAsync(userId, EventAction.RescheduleEvent, id);
            
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Event not found"));

            if (eventEntity.Status == EventStatus.Cancelled)
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot reschedule cancelled events"));

            if (eventEntity.Status == EventStatus.Completed)
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot reschedule completed events"));

            eventEntity.StartDateTime = request.NewStartDateTime;
            eventEntity.EndDateTime = request.NewEndDateTime;
            eventEntity.Status = EventStatus.Rescheduled;
            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // TODO: Send reschedule notifications to registered users

            return Ok(ApiResponse<bool>.SuccessResult(true, "Event rescheduled successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error rescheduling event: {ex.Message}"));
        }
    }

    private async Task PromoteFromWaitlistAsync(Guid eventId)
    {
        var eventEntity = await _context.Events.FindAsync(eventId);
        if (eventEntity == null) return;

        var confirmedCount = await _context.EventRegistrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed);

        if (confirmedCount >= eventEntity.MaxCapacity) return;

        var spotsAvailable = eventEntity.MaxCapacity - confirmedCount;
        
        var waitlistRegistrations = await _context.EventRegistrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.RegisteredAt)
            .Take(spotsAvailable)
            .ToListAsync();

        foreach (var registration in waitlistRegistrations)
        {
            registration.Status = RegistrationStatus.Confirmed;
            registration.IsWaitlisted = false;
            registration.WaitlistPosition = null;
            registration.UpdatedAt = DateTime.UtcNow;
            eventEntity.CurrentEnrollment++;
        }

        // Update waitlist positions for remaining waitlisted members
        var remainingWaitlist = await _context.EventRegistrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.RegisteredAt)
            .ToListAsync();

        for (int i = 0; i < remainingWaitlist.Count; i++)
        {
            remainingWaitlist[i].WaitlistPosition = i + 1;
        }

        await _context.SaveChangesAsync();

        // TODO: Send promotion notifications to confirmed members
    }

    // Bulk Registration Endpoints for Recurring Events
    [HttpPost("bulk-register")]
    public async Task<ActionResult<ApiResponse<RecurringRegistrationResponse>>> BulkRegisterForEvents([FromBody] BulkEventRegistrationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var response = new RecurringRegistrationResponse();

            // Validate input
            if (!request.EventIds.Any())
                return BadRequest(ApiResponse<RecurringRegistrationResponse>.ErrorResult("No events specified for registration"));

            if (!request.MemberIds.Any())
                return BadRequest(ApiResponse<RecurringRegistrationResponse>.ErrorResult("No members specified for registration"));

            // Process each event-member combination
            foreach (var eventId in request.EventIds)
            {
                foreach (var memberId in request.MemberIds)
                {
                    try
                    {
                        var result = await RegisterMemberForEventInternal(eventId, memberId, userId, request.Notes);
                        response.Results.Add(result);
                        
                        if (result.Success)
                        {
                            response.SuccessfulRegistrations++;
                        }
                        else
                        {
                            response.FailedRegistrations++;
                        }
                    }
                    catch (Exception ex)
                    {
                        response.FailedRegistrations++;
                        response.Results.Add(new EventRegistrationResult
                        {
                            EventId = eventId,
                            MemberId = memberId,
                            Success = false,
                            ErrorMessage = ex.Message,
                            Status = RegistrationStatus.Declined
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            var message = $"Bulk registration completed: {response.SuccessfulRegistrations} successful, {response.FailedRegistrations} failed";
            return Ok(ApiResponse<RecurringRegistrationResponse>.SuccessResult(response, message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RecurringRegistrationResponse>.ErrorResult($"Error processing bulk registration: {ex.Message}"));
        }
    }

    [HttpGet("{masterEventId}/recurring-options")]
    public async Task<ActionResult<ApiResponse<RecurringEventOptionsDto>>> GetRecurringEventOptions(Guid masterEventId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            
            // Check if this is a recurring master event
            var masterEvent = await _context.Events
                .Include(e => e.Facility)
                .Include(e => e.Instructor)
                .FirstOrDefaultAsync(e => e.Id == masterEventId && e.IsRecurringMaster);
                
            if (masterEvent == null)
                return NotFound(ApiResponse<RecurringEventOptionsDto>.ErrorResult("Recurring event series not found"));

            // Get all future occurrences
            var upcomingOccurrences = await _context.Events
                .Where(e => e.MasterEventId == masterEventId && e.StartDateTime > DateTime.UtcNow)
                .OrderBy(e => e.StartDateTime)
                .Select(e => new EventOccurrenceDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartDateTime = e.StartDateTime,
                    EndDateTime = e.EndDateTime,
                    CurrentEnrollment = e.CurrentEnrollment,
                    MaxCapacity = e.MaxCapacity,
                    IsFullyBooked = e.CurrentEnrollment >= e.MaxCapacity,
                    AllowWaitlist = e.AllowWaitlist
                })
                .ToListAsync();

            // Check if user is registered for any occurrences
            var userMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (userMember != null)
            {
                var registeredEventIds = await _context.EventRegistrations
                    .Where(r => r.MemberId == userMember.Id && r.Status != RegistrationStatus.Cancelled)
                    .Select(r => r.EventId)
                    .ToListAsync();

                foreach (var occurrence in upcomingOccurrences)
                {
                    occurrence.IsUserRegistered = registeredEventIds.Contains(occurrence.Id);
                }
            }

            var options = new RecurringEventOptionsDto
            {
                MasterEventId = masterEventId,
                SeriesTitle = masterEvent.Title,
                UpcomingOccurrences = upcomingOccurrences,
                TotalOccurrences = upcomingOccurrences.Count,
                RecurrencePattern = masterEvent.Recurrence
            };

            return Ok(ApiResponse<RecurringEventOptionsDto>.SuccessResult(options));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RecurringEventOptionsDto>.ErrorResult($"Error retrieving recurring event options: {ex.Message}"));
        }
    }

    [HttpGet("user/recurring-registrations")]
    public async Task<ActionResult<ApiResponse<List<RecurringRegistrationSummary>>>> GetUserRecurringRegistrations()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var userMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            
            if (userMember == null)
                return Ok(ApiResponse<List<RecurringRegistrationSummary>>.SuccessResult(new List<RecurringRegistrationSummary>()));

            // Get all recurring event series the user is registered for
            var recurringRegistrations = await _context.EventRegistrations
                .Include(r => r.Event)
                .ThenInclude(e => e.Facility)
                .Where(r => r.MemberId == userMember.Id && 
                           r.Status != RegistrationStatus.Cancelled &&
                           r.Event.MasterEventId != null)
                .GroupBy(r => r.Event.MasterEventId)
                .Select(g => new
                {
                    MasterEventId = g.Key,
                    Registrations = g.ToList()
                })
                .ToListAsync();

            var summaries = new List<RecurringRegistrationSummary>();

            foreach (var group in recurringRegistrations)
            {
                if (!group.MasterEventId.HasValue) continue;

                var masterEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.Id == group.MasterEventId.Value && e.IsRecurringMaster);

                if (masterEvent == null) continue;

                var registeredEvents = group.Registrations
                    .Where(r => r.Event.StartDateTime > DateTime.UtcNow)
                    .OrderBy(r => r.Event.StartDateTime)
                    .Select(r => new EventOccurrenceDto
                    {
                        Id = r.Event.Id,
                        Title = r.Event.Title,
                        StartDateTime = r.Event.StartDateTime,
                        EndDateTime = r.Event.EndDateTime,
                        CurrentEnrollment = r.Event.CurrentEnrollment,
                        MaxCapacity = r.Event.MaxCapacity,
                        IsUserRegistered = true,
                        IsFullyBooked = r.Event.CurrentEnrollment >= r.Event.MaxCapacity,
                        AllowWaitlist = r.Event.AllowWaitlist
                    })
                    .ToList();

                var summary = new RecurringRegistrationSummary
                {
                    MasterEventId = group.MasterEventId.Value,
                    EventSeriesName = masterEvent.Title,
                    NextOccurrence = registeredEvents.FirstOrDefault()?.StartDateTime,
                    TotalRegistered = registeredEvents.Count,
                    TotalOccurrences = await _context.Events.CountAsync(e => e.MasterEventId == group.MasterEventId),
                    RegistrationType = DetermineRegistrationType(registeredEvents.Count, await _context.Events.CountAsync(e => e.MasterEventId == group.MasterEventId && e.StartDateTime > DateTime.UtcNow)),
                    RegisteredEvents = registeredEvents
                };

                summaries.Add(summary);
            }

            return Ok(ApiResponse<List<RecurringRegistrationSummary>>.SuccessResult(summaries));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<RecurringRegistrationSummary>>.ErrorResult($"Error retrieving recurring registrations: {ex.Message}"));
        }
    }

    private async Task<EventRegistrationResult> RegisterMemberForEventInternal(Guid eventId, Guid memberId, Guid registeredByUserId, string? notes)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Facility)
            .FirstOrDefaultAsync(e => e.Id == eventId);
            
        if (eventEntity == null)
        {
            return new EventRegistrationResult
            {
                EventId = eventId,
                MemberId = memberId,
                Success = false,
                ErrorMessage = "Event not found",
                Status = RegistrationStatus.Declined
            };
        }

        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);
            
        if (member == null)
        {
            return new EventRegistrationResult
            {
                EventId = eventId,
                MemberId = memberId,
                Success = false,
                ErrorMessage = "Member not found",
                Status = RegistrationStatus.Declined,
                EventTitle = eventEntity.Title,
                EventDateTime = eventEntity.StartDateTime
            };
        }

        // Check if already registered
        var existingRegistration = await _context.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.MemberId == memberId);
            
        if (existingRegistration != null)
        {
            return new EventRegistrationResult
            {
                EventId = eventId,
                MemberId = memberId,
                Success = false,
                ErrorMessage = "Already registered for this event",
                Status = existingRegistration.Status,
                EventTitle = eventEntity.Title,
                MemberName = $"{member.User.FirstName} {member.User.LastName}",
                EventDateTime = eventEntity.StartDateTime
            };
        }

        // Check capacity
        var currentRegistrations = await _context.EventRegistrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed);

        var status = currentRegistrations < eventEntity.MaxCapacity 
            ? RegistrationStatus.Confirmed 
            : (eventEntity.AllowWaitlist ? RegistrationStatus.Waitlisted : RegistrationStatus.Declined);

        if (status == RegistrationStatus.Declined)
        {
            return new EventRegistrationResult
            {
                EventId = eventId,
                MemberId = memberId,
                Success = false,
                ErrorMessage = "Event is full and waitlist is not allowed",
                Status = RegistrationStatus.Declined,
                EventTitle = eventEntity.Title,
                MemberName = $"{member.User.FirstName} {member.User.LastName}",
                EventDateTime = eventEntity.StartDateTime
            };
        }

        var registration = new EventRegistration
        {
            EventId = eventId,
            MemberId = memberId,
            RegisteredByUserId = registeredByUserId,
            Status = status,
            RegisteredAt = DateTime.UtcNow,
            Notes = notes,
            IsWaitlisted = status == RegistrationStatus.Waitlisted
        };

        if (status == RegistrationStatus.Waitlisted)
        {
            var waitlistCount = await _context.EventRegistrations
                .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted);
            registration.WaitlistPosition = waitlistCount + 1;
        }

        _context.EventRegistrations.Add(registration);

        // Update enrollment count
        if (status == RegistrationStatus.Confirmed)
        {
            eventEntity.CurrentEnrollment++;
        }

        return new EventRegistrationResult
        {
            EventId = eventId,
            MemberId = memberId,
            Success = true,
            Status = status,
            EventTitle = eventEntity.Title,
            MemberName = $"{member.User.FirstName} {member.User.LastName}",
            EventDateTime = eventEntity.StartDateTime
        };
    }

    private static RecurringRegistrationOption DetermineRegistrationType(int registeredCount, int totalUpcoming)
    {
        if (registeredCount == totalUpcoming && totalUpcoming > 0)
            return RecurringRegistrationOption.AllFutureOccurrences;
        if (registeredCount == 1)
            return RecurringRegistrationOption.ThisOccurrenceOnly;
        return RecurringRegistrationOption.SelectSpecific;
    }
}

