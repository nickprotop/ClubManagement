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
[Route("api/hardware-types")]
[Authorize]
public class HardwareTypesController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IHardwareAuthorizationService _authService;

    public HardwareTypesController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IHardwareAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<HardwareTypeDto>>>> GetHardwareTypes()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<HardwareTypeDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view hardware types");

            var hardwareTypes = await tenantContext.HardwareTypes
                .Where(ht => ht.IsActive)
                .OrderBy(ht => ht.SortOrder)
                .ThenBy(ht => ht.Name)
                .Select(ht => new HardwareTypeDto
                {
                    Id = ht.Id,
                    Name = ht.Name,
                    Description = ht.Description,
                    Icon = ht.Icon,
                    PropertySchema = ht.PropertySchema,
                    IsActive = ht.IsActive,
                    SortOrder = ht.SortOrder,
                    RequiresAssignment = ht.RequiresAssignment,
                    AllowMultipleAssignments = ht.AllowMultipleAssignments,
                    MaxAssignmentDurationHours = ht.MaxAssignmentDurationHours,
                    CreatedAt = ht.CreatedAt,
                    UpdatedAt = ht.UpdatedAt ?? ht.CreatedAt,
                    HardwareCount = tenantContext.Hardware.Count(h => h.HardwareTypeId == ht.Id),
                    AvailableCount = tenantContext.Hardware.Count(h => h.HardwareTypeId == ht.Id && h.Status == HardwareStatus.Available),
                    AssignedCount = tenantContext.Hardware.Count(h => h.HardwareTypeId == ht.Id && h.Status == HardwareStatus.Assigned),
                    MaintenanceCount = tenantContext.Hardware.Count(h => h.HardwareTypeId == ht.Id && h.Status == HardwareStatus.Maintenance)
                })
                .ToListAsync();

            return Ok(ApiResponse<List<HardwareTypeDto>>.SuccessResult(hardwareTypes));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<HardwareTypeDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<HardwareTypeDto>>.ErrorResult($"Error retrieving hardware types: {ex.Message}"));
        }
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<HardwarePermissions>> GetPermissions()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest("Invalid tenant");
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetPermissionsAsync(userId, tenantContext);
            return Ok(permissions);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized($"Unauthorized: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HardwareTypeDto>>> GetHardwareType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareTypeDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view hardware type");

            var hardwareType = await tenantContext.HardwareTypes
                .FirstOrDefaultAsync(ht => ht.Id == id);

            if (hardwareType == null)
                return NotFound(ApiResponse<HardwareTypeDto>.ErrorResult("Hardware type not found"));

            var dto = new HardwareTypeDto
            {
                Id = hardwareType.Id,
                Name = hardwareType.Name,
                Description = hardwareType.Description,
                Icon = hardwareType.Icon,
                PropertySchema = hardwareType.PropertySchema,
                IsActive = hardwareType.IsActive,
                SortOrder = hardwareType.SortOrder,
                RequiresAssignment = hardwareType.RequiresAssignment,
                AllowMultipleAssignments = hardwareType.AllowMultipleAssignments,
                MaxAssignmentDurationHours = hardwareType.MaxAssignmentDurationHours,
                CreatedAt = hardwareType.CreatedAt,
                UpdatedAt = hardwareType.UpdatedAt ?? hardwareType.CreatedAt,
                HardwareCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id),
                AvailableCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id && h.Status == HardwareStatus.Available),
                AssignedCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id && h.Status == HardwareStatus.Assigned),
                MaintenanceCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id && h.Status == HardwareStatus.Maintenance)
            };

            return Ok(ApiResponse<HardwareTypeDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareTypeDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareTypeDto>.ErrorResult($"Error retrieving hardware type: {ex.Message}"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<HardwareTypeDto>>> CreateHardwareType([FromBody] CreateHardwareTypeRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareTypeDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.CreateType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Check if name already exists
            var existingType = await tenantContext.HardwareTypes
                .FirstOrDefaultAsync(ht => ht.Name.ToLower() == request.Name.ToLower());

            if (existingType != null)
                return BadRequest(ApiResponse<HardwareTypeDto>.ErrorResult("Hardware type with this name already exists"));

            var hardwareType = new HardwareType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                Icon = request.Icon,
                PropertySchema = request.PropertySchema,
                IsActive = true,
                SortOrder = request.SortOrder,
                RequiresAssignment = request.RequiresAssignment,
                AllowMultipleAssignments = request.AllowMultipleAssignments,
                MaxAssignmentDurationHours = request.MaxAssignmentDurationHours,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.HardwareTypes.Add(hardwareType);
            await tenantContext.SaveChangesAsync();

            var dto = new HardwareTypeDto
            {
                Id = hardwareType.Id,
                Name = hardwareType.Name,
                Description = hardwareType.Description,
                Icon = hardwareType.Icon,
                PropertySchema = hardwareType.PropertySchema,
                IsActive = hardwareType.IsActive,
                SortOrder = hardwareType.SortOrder,
                RequiresAssignment = hardwareType.RequiresAssignment,
                AllowMultipleAssignments = hardwareType.AllowMultipleAssignments,
                MaxAssignmentDurationHours = hardwareType.MaxAssignmentDurationHours,
                CreatedAt = hardwareType.CreatedAt,
                UpdatedAt = hardwareType.UpdatedAt ?? hardwareType.CreatedAt,
                HardwareCount = 0,
                AvailableCount = 0,
                AssignedCount = 0,
                MaintenanceCount = 0
            };

            return CreatedAtAction(nameof(GetHardwareType), new { id = hardwareType.Id }, ApiResponse<HardwareTypeDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareTypeDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareTypeDto>.ErrorResult($"Error creating hardware type: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<HardwareTypeDto>>> UpdateHardwareType(Guid id, [FromBody] UpdateHardwareTypeRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareTypeDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.EditType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var hardwareType = await tenantContext.HardwareTypes.FirstOrDefaultAsync(ht => ht.Id == id);
            if (hardwareType == null)
                return NotFound(ApiResponse<HardwareTypeDto>.ErrorResult("Hardware type not found"));

            // Check if name already exists (excluding current type)
            var existingType = await tenantContext.HardwareTypes
                .FirstOrDefaultAsync(ht => ht.Id != id && ht.Name.ToLower() == request.Name.ToLower());

            if (existingType != null)
                return BadRequest(ApiResponse<HardwareTypeDto>.ErrorResult("Hardware type with this name already exists"));

            // Update hardware type properties
            hardwareType.Name = request.Name;
            hardwareType.Description = request.Description;
            hardwareType.Icon = request.Icon;
            hardwareType.PropertySchema = request.PropertySchema;
            hardwareType.IsActive = request.IsActive;
            hardwareType.SortOrder = request.SortOrder;
            hardwareType.RequiresAssignment = request.RequiresAssignment;
            hardwareType.AllowMultipleAssignments = request.AllowMultipleAssignments;
            hardwareType.MaxAssignmentDurationHours = request.MaxAssignmentDurationHours;
            hardwareType.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            var dto = new HardwareTypeDto
            {
                Id = hardwareType.Id,
                Name = hardwareType.Name,
                Description = hardwareType.Description,
                Icon = hardwareType.Icon,
                PropertySchema = hardwareType.PropertySchema,
                IsActive = hardwareType.IsActive,
                SortOrder = hardwareType.SortOrder,
                RequiresAssignment = hardwareType.RequiresAssignment,
                AllowMultipleAssignments = hardwareType.AllowMultipleAssignments,
                MaxAssignmentDurationHours = hardwareType.MaxAssignmentDurationHours,
                CreatedAt = hardwareType.CreatedAt,
                UpdatedAt = hardwareType.UpdatedAt ?? hardwareType.CreatedAt,
                HardwareCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id),
                AvailableCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id && h.Status == HardwareStatus.Available),
                AssignedCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id && h.Status == HardwareStatus.Assigned),
                MaintenanceCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == hardwareType.Id && h.Status == HardwareStatus.Maintenance)
            };

            return Ok(ApiResponse<HardwareTypeDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareTypeDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareTypeDto>.ErrorResult($"Error updating hardware type: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteHardwareType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.DeleteType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var hardwareType = await tenantContext.HardwareTypes.FirstOrDefaultAsync(ht => ht.Id == id);
            if (hardwareType == null)
                return NotFound(ApiResponse<object>.ErrorResult("Hardware type not found"));

            // Check if hardware type is being used
            var hardwareCount = await tenantContext.Hardware.CountAsync(h => h.HardwareTypeId == id);
            if (hardwareCount > 0)
                return BadRequest(ApiResponse<object>.ErrorResult($"Cannot delete hardware type with {hardwareCount} hardware items"));

            tenantContext.HardwareTypes.Remove(hardwareType);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Hardware type deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deleting hardware type: {ex.Message}"));
        }
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateHardwareType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.EditType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var hardwareType = await tenantContext.HardwareTypes.FirstOrDefaultAsync(ht => ht.Id == id);
            if (hardwareType == null)
                return NotFound(ApiResponse<object>.ErrorResult("Hardware type not found"));

            hardwareType.IsActive = false;
            hardwareType.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Hardware type deactivated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deactivating hardware type: {ex.Message}"));
        }
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateHardwareType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.EditType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var hardwareType = await tenantContext.HardwareTypes.FirstOrDefaultAsync(ht => ht.Id == id);
            if (hardwareType == null)
                return NotFound(ApiResponse<object>.ErrorResult("Hardware type not found"));

            hardwareType.IsActive = true;
            hardwareType.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Hardware type activated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error activating hardware type: {ex.Message}"));
        }
    }
}