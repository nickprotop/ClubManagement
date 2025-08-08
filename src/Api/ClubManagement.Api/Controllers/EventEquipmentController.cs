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
[Route("api/events/{eventId}/equipment")]
[Authorize]
public class EventEquipmentController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IEventAuthorizationService _eventAuthService;
    private readonly IHardwareAuthorizationService _hardwareAuthService;

    public EventEquipmentController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IEventAuthorizationService eventAuthService,
        IHardwareAuthorizationService hardwareAuthService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _eventAuthService = eventAuthService;
        _hardwareAuthService = hardwareAuthService;
    }

    [HttpGet("requirements")]
    public async Task<ActionResult<ApiResponse<List<EventEquipmentRequirementDto>>>> GetEventEquipmentRequirements(Guid eventId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<EventEquipmentRequirementDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _eventAuthService.CanPerformActionAsync(userId, EventAction.View, tenantContext, eventId))
                return Forbid();

            // Verify event exists
            var eventExists = await tenantContext.Events.AnyAsync(e => e.Id == eventId);
            if (!eventExists)
                return NotFound(ApiResponse<List<EventEquipmentRequirementDto>>.ErrorResult("Event not found"));

            var requirements = await tenantContext.EventEquipmentRequirements
                .Include(r => r.HardwareType)
                .Include(r => r.Assignments)
                    .ThenInclude(a => a.Hardware)
                .Where(r => r.EventId == eventId)
                .Select(r => new EventEquipmentRequirementDto
                {
                    Id = r.Id,
                    EventId = r.EventId,
                    HardwareTypeId = r.HardwareTypeId,
                    HardwareTypeName = r.HardwareType.Name,
                    HardwareTypeIcon = r.HardwareType.Icon,
                    QuantityRequired = r.Quantity,
                    IsMandatory = r.IsMandatory,
                    AutoAssign = r.AutoAssign,
                    Notes = r.Notes,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt ?? r.CreatedAt,
                    QuantityAssigned = r.Assignments.Count,
                    QuantityRemaining = r.Quantity - r.Assignments.Count,
                    IsFulfilled = r.Assignments.Count >= r.Quantity,
                    AssignedHardware = r.Assignments.Select(a => new EventEquipmentAssignmentDto
                    {
                        Id = a.Id,
                        HardwareId = a.HardwareId,
                        HardwareName = a.Hardware.Name,
                        HardwareSerialNumber = a.Hardware.SerialNumber,
                        AssignedAt = a.AssignedAt,
                        Status = a.Status,
                        Notes = a.Notes
                    }).ToList()
                })
                .ToListAsync();

            return Ok(ApiResponse<List<EventEquipmentRequirementDto>>.SuccessResult(requirements));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<EventEquipmentRequirementDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<EventEquipmentRequirementDto>>.ErrorResult($"Error retrieving event equipment requirements: {ex.Message}"));
        }
    }

    [HttpGet("requirements/{requirementId}")]
    public async Task<ActionResult<ApiResponse<EventEquipmentRequirementDto>>> GetEventEquipmentRequirement(Guid eventId, Guid requirementId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _eventAuthService.CanPerformActionAsync(userId, EventAction.View, tenantContext, eventId))
                return Forbid();

            var requirement = await tenantContext.EventEquipmentRequirements
                .Include(r => r.HardwareType)
                .Include(r => r.Assignments)
                    .ThenInclude(a => a.Hardware)
                .FirstOrDefaultAsync(r => r.Id == requirementId && r.EventId == eventId);

            if (requirement == null)
                return NotFound(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Equipment requirement not found"));

            var dto = new EventEquipmentRequirementDto
            {
                Id = requirement.Id,
                EventId = requirement.EventId,
                HardwareTypeId = requirement.HardwareTypeId,
                HardwareTypeName = requirement.HardwareType.Name,
                HardwareTypeIcon = requirement.HardwareType.Icon,
                QuantityRequired = requirement.Quantity,
                IsMandatory = requirement.IsMandatory,
                AutoAssign = requirement.AutoAssign,
                Notes = requirement.Notes,
                CreatedAt = requirement.CreatedAt,
                UpdatedAt = requirement.UpdatedAt ?? requirement.CreatedAt,
                QuantityAssigned = requirement.Assignments.Count,
                QuantityRemaining = requirement.Quantity - requirement.Assignments.Count,
                IsFulfilled = requirement.Assignments.Count >= requirement.Quantity,
                AssignedHardware = requirement.Assignments.Select(a => new EventEquipmentAssignmentDto
                {
                    Id = a.Id,
                    HardwareId = a.HardwareId,
                    HardwareName = a.Hardware.Name,
                    HardwareSerialNumber = a.Hardware.SerialNumber,
                    AssignedAt = a.AssignedAt,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToList()
            };

            return Ok(ApiResponse<EventEquipmentRequirementDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<EventEquipmentRequirementDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventEquipmentRequirementDto>.ErrorResult($"Error retrieving event equipment requirement: {ex.Message}"));
        }
    }

    [HttpPost("requirements")]
    [Authorize(Roles = "Staff,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EventEquipmentRequirementDto>>> CreateEventEquipmentRequirement(Guid eventId, [FromBody] CreateEventEquipmentRequirementRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var eventAuthResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Edit, tenantContext, eventId);
            if (!eventAuthResult.Succeeded)
                return Forbid(string.Join(", ", eventAuthResult.Reasons));

            var hardwareAuthResult = await _hardwareAuthService.CheckAuthorizationAsync(userId, HardwareAction.Assign, tenantContext);
            if (!hardwareAuthResult.Succeeded)
                return Forbid(string.Join(", ", hardwareAuthResult.Reasons));

            // Verify event exists
            var eventEntity = await tenantContext.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
                return NotFound(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Event not found"));

            // Verify hardware type exists
            var hardwareType = await tenantContext.HardwareTypes.FirstOrDefaultAsync(ht => ht.Id == request.HardwareTypeId);
            if (hardwareType == null)
                return BadRequest(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Invalid hardware type"));

            // Check if requirement already exists for this hardware type
            var existingRequirement = await tenantContext.EventEquipmentRequirements
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.HardwareTypeId == request.HardwareTypeId);
            if (existingRequirement != null)
                return BadRequest(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Equipment requirement for this hardware type already exists"));

            var requirement = new EventEquipmentRequirement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventId = eventId,
                HardwareTypeId = request.HardwareTypeId,
                Quantity = request.QuantityRequired,
                IsMandatory = request.IsMandatory,
                AutoAssign = request.AutoAssign,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.EventEquipmentRequirements.Add(requirement);
            await tenantContext.SaveChangesAsync();

            // Auto-assign hardware if requested
            if (request.AutoAssign)
            {
                await AutoAssignHardware(requirement, tenantId, tenantContext);
            }

            // Return the created requirement with type info
            var createdRequirement = await tenantContext.EventEquipmentRequirements
                .Include(r => r.HardwareType)
                .Include(r => r.Assignments)
                    .ThenInclude(a => a.Hardware)
                .FirstOrDefaultAsync(r => r.Id == requirement.Id);

            var dto = new EventEquipmentRequirementDto
            {
                Id = createdRequirement!.Id,
                EventId = createdRequirement.EventId,
                HardwareTypeId = createdRequirement.HardwareTypeId,
                HardwareTypeName = createdRequirement.HardwareType.Name,
                HardwareTypeIcon = createdRequirement.HardwareType.Icon,
                QuantityRequired = createdRequirement.Quantity,
                IsMandatory = createdRequirement.IsMandatory,
                AutoAssign = createdRequirement.AutoAssign,
                Notes = createdRequirement.Notes,
                CreatedAt = createdRequirement.CreatedAt,
                UpdatedAt = createdRequirement.UpdatedAt ?? createdRequirement.CreatedAt,
                QuantityAssigned = createdRequirement.Assignments.Count,
                QuantityRemaining = createdRequirement.Quantity - createdRequirement.Assignments.Count,
                IsFulfilled = createdRequirement.Assignments.Count >= createdRequirement.Quantity,
                AssignedHardware = createdRequirement.Assignments.Select(a => new EventEquipmentAssignmentDto
                {
                    Id = a.Id,
                    HardwareId = a.HardwareId,
                    HardwareName = a.Hardware.Name,
                    HardwareSerialNumber = a.Hardware.SerialNumber,
                    AssignedAt = a.AssignedAt,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToList()
            };

            return CreatedAtAction(nameof(GetEventEquipmentRequirement), new { eventId, requirementId = requirement.Id }, ApiResponse<EventEquipmentRequirementDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<EventEquipmentRequirementDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventEquipmentRequirementDto>.ErrorResult($"Error creating event equipment requirement: {ex.Message}"));
        }
    }

    [HttpPut("requirements/{requirementId}")]
    [Authorize(Roles = "Staff,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EventEquipmentRequirementDto>>> UpdateEventEquipmentRequirement(Guid eventId, Guid requirementId, [FromBody] UpdateEventEquipmentRequirementRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var eventAuthResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Edit, tenantContext, eventId);
            if (!eventAuthResult.Succeeded)
                return Forbid(string.Join(", ", eventAuthResult.Reasons));

            var requirement = await tenantContext.EventEquipmentRequirements
                .Include(r => r.HardwareType)
                .Include(r => r.Assignments)
                    .ThenInclude(a => a.Hardware)
                .FirstOrDefaultAsync(r => r.Id == requirementId && r.EventId == eventId);

            if (requirement == null)
                return NotFound(ApiResponse<EventEquipmentRequirementDto>.ErrorResult("Equipment requirement not found"));

            // Update requirement properties
            requirement.Quantity = request.QuantityRequired;
            requirement.IsMandatory = request.IsMandatory;
            requirement.AutoAssign = request.AutoAssign;
            requirement.Notes = request.Notes;
            requirement.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            var dto = new EventEquipmentRequirementDto
            {
                Id = requirement.Id,
                EventId = requirement.EventId,
                HardwareTypeId = requirement.HardwareTypeId,
                HardwareTypeName = requirement.HardwareType.Name,
                HardwareTypeIcon = requirement.HardwareType.Icon,
                QuantityRequired = requirement.Quantity,
                IsMandatory = requirement.IsMandatory,
                AutoAssign = requirement.AutoAssign,
                Notes = requirement.Notes,
                CreatedAt = requirement.CreatedAt,
                UpdatedAt = requirement.UpdatedAt ?? requirement.CreatedAt,
                QuantityAssigned = requirement.Assignments.Count,
                QuantityRemaining = requirement.Quantity - requirement.Assignments.Count,
                IsFulfilled = requirement.Assignments.Count >= requirement.Quantity,
                AssignedHardware = requirement.Assignments.Select(a => new EventEquipmentAssignmentDto
                {
                    Id = a.Id,
                    HardwareId = a.HardwareId,
                    HardwareName = a.Hardware.Name,
                    HardwareSerialNumber = a.Hardware.SerialNumber,
                    AssignedAt = a.AssignedAt,
                    Status = a.Status,
                    Notes = a.Notes
                }).ToList()
            };

            return Ok(ApiResponse<EventEquipmentRequirementDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<EventEquipmentRequirementDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventEquipmentRequirementDto>.ErrorResult($"Error updating event equipment requirement: {ex.Message}"));
        }
    }

    [HttpDelete("requirements/{requirementId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteEventEquipmentRequirement(Guid eventId, Guid requirementId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var eventAuthResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Delete, tenantContext, eventId);
            if (!eventAuthResult.Succeeded)
                return Forbid(string.Join(", ", eventAuthResult.Reasons));

            var requirement = await tenantContext.EventEquipmentRequirements
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == requirementId && r.EventId == eventId);

            if (requirement == null)
                return NotFound(ApiResponse<object>.ErrorResult("Equipment requirement not found"));

            // Remove all assignments first
            tenantContext.EventEquipmentAssignments.RemoveRange(requirement.Assignments);
            tenantContext.EventEquipmentRequirements.Remove(requirement);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Equipment requirement deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deleting event equipment requirement: {ex.Message}"));
        }
    }

    [HttpPost("requirements/{requirementId}/assign/{hardwareId}")]
    [Authorize(Roles = "Staff,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EventEquipmentAssignmentDto>>> AssignHardware(Guid eventId, Guid requirementId, Guid hardwareId, [FromBody] AssignEventEquipmentRequest? request = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var hardwareAuthResult = await _hardwareAuthService.CheckAuthorizationAsync(userId, HardwareAction.Assign, tenantContext, hardwareId);
            if (!hardwareAuthResult.Succeeded)
                return Forbid(string.Join(", ", hardwareAuthResult.Reasons));

            var requirement = await tenantContext.EventEquipmentRequirements
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == requirementId && r.EventId == eventId);

            if (requirement == null)
                return NotFound(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult("Equipment requirement not found"));

            var hardware = await tenantContext.Hardware.FirstOrDefaultAsync(h => h.Id == hardwareId);
            if (hardware == null)
                return NotFound(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult("Hardware not found"));

            // Check if hardware is available
            if (hardware.Status != HardwareStatus.Available)
                return BadRequest(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult("Hardware is not available"));

            // Check if already assigned to this requirement
            if (requirement.Assignments.Any(a => a.HardwareId == hardwareId))
                return BadRequest(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult("Hardware already assigned to this requirement"));

            // Check if requirement is already fulfilled
            if (requirement.Assignments.Count >= requirement.Quantity)
                return BadRequest(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult("Equipment requirement is already fulfilled"));

            var assignment = new EventEquipmentAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventId = eventId,
                RequirementId = requirementId,
                HardwareId = hardwareId,
                AssignedAt = DateTime.UtcNow,
                Status = ClubManagement.Shared.Models.EventEquipmentAssignmentStatus.Reserved,
                Notes = request?.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            // Update hardware status
            // Note: Hardware status remains unchanged - staff controls it manually
            hardware.UpdatedAt = DateTime.UtcNow;

            tenantContext.EventEquipmentAssignments.Add(assignment);
            await tenantContext.SaveChangesAsync();

            var dto = new EventEquipmentAssignmentDto
            {
                Id = assignment.Id,
                HardwareId = assignment.HardwareId,
                HardwareName = hardware.Name,
                HardwareSerialNumber = hardware.SerialNumber,
                AssignedAt = assignment.AssignedAt,
                Status = assignment.Status,
                Notes = assignment.Notes
            };

            return Ok(ApiResponse<EventEquipmentAssignmentDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<EventEquipmentAssignmentDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EventEquipmentAssignmentDto>.ErrorResult($"Error assigning hardware: {ex.Message}"));
        }
    }

    [HttpDelete("assignments/{assignmentId}")]
    [Authorize(Roles = "Staff,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> UnassignHardware(Guid eventId, Guid assignmentId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var hardwareAuthResult = await _hardwareAuthService.CheckAuthorizationAsync(userId, HardwareAction.Assign, tenantContext);
            if (!hardwareAuthResult.Succeeded)
                return Forbid(string.Join(", ", hardwareAuthResult.Reasons));

            var assignment = await tenantContext.EventEquipmentAssignments
                .Include(a => a.Hardware)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.EventId == eventId);

            if (assignment == null)
                return NotFound(ApiResponse<object>.ErrorResult("Equipment assignment not found"));

            // Update hardware status back to available
            assignment.Hardware.Status = HardwareStatus.Available;
            assignment.Hardware.UpdatedAt = DateTime.UtcNow;

            tenantContext.EventEquipmentAssignments.Remove(assignment);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Hardware unassigned successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error unassigning hardware: {ex.Message}"));
        }
    }

    [HttpGet("availability")]
    public async Task<ActionResult<ApiResponse<List<HardwareAvailabilityDto>>>> CheckHardwareAvailability(Guid eventId, [FromQuery] Guid? hardwareTypeId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<HardwareAvailabilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _hardwareAuthService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid();

            var query = tenantContext.Hardware
                .Include(h => h.HardwareType)
                .AsQueryable();

            if (hardwareTypeId.HasValue)
                query = query.Where(h => h.HardwareTypeId == hardwareTypeId.Value);

            var hardware = await query.ToListAsync();

            var availability = new List<HardwareAvailabilityDto>();

            foreach (var item in hardware)
            {
                var conflicts = new List<ConflictingEquipmentEventDto>();

                // Check for conflicts with other events in the date range
                if (startDate.HasValue && endDate.HasValue)
                {
                    var conflictingAssignments = await tenantContext.EventEquipmentAssignments
                        .Include(a => a.Event)
                        .Where(a => a.HardwareId == item.Id && 
                               a.Event.StartDateTime < endDate.Value && 
                               a.Event.EndDateTime > startDate.Value &&
                               a.EventId != eventId)
                        .ToListAsync();

                    conflicts = conflictingAssignments.Select(a => new ConflictingEquipmentEventDto
                    {
                        EventId = a.EventId,
                        EventName = a.Event.Title,
                        EventStartDate = a.Event.StartDateTime,
                        EventEndDate = a.Event.EndDateTime,
                        AssignmentId = a.Id
                    }).ToList();
                }

                availability.Add(new HardwareAvailabilityDto
                {
                    HardwareId = item.Id,
                    HardwareName = item.Name,
                    HardwareSerialNumber = item.SerialNumber,
                    HardwareTypeId = item.HardwareTypeId,
                    HardwareTypeName = item.HardwareType.Name,
                    Status = item.Status,
                    IsAvailable = item.Status == HardwareStatus.Available && conflicts.Count == 0,
                    ConflictingEvents = conflicts
                });
            }

            return Ok(ApiResponse<List<HardwareAvailabilityDto>>.SuccessResult(availability));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<HardwareAvailabilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<HardwareAvailabilityDto>>.ErrorResult($"Error checking hardware availability: {ex.Message}"));
        }
    }

    private async Task AutoAssignHardware(EventEquipmentRequirement requirement, Guid tenantId, ClubManagementDbContext tenantContext)
    {
        var availableHardware = await tenantContext.Hardware
            .Where(h => h.HardwareTypeId == requirement.HardwareTypeId && 
                       h.Status == HardwareStatus.Available)
            .Take(requirement.Quantity)
            .ToListAsync();

        foreach (var hardware in availableHardware)
        {
            var assignment = new EventEquipmentAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventId = requirement.EventId,
                RequirementId = requirement.Id,
                HardwareId = hardware.Id,
                AssignedAt = DateTime.UtcNow,
                Status = ClubManagement.Shared.Models.EventEquipmentAssignmentStatus.Reserved,
                Notes = "Auto-assigned",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            // Note: Hardware status remains unchanged - staff controls it manually
            hardware.UpdatedAt = DateTime.UtcNow;

            tenantContext.EventEquipmentAssignments.Add(assignment);
        }

        await tenantContext.SaveChangesAsync();
    }
}