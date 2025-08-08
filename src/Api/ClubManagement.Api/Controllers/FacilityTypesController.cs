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
[Route("api/facility-types")]
[Authorize]
public class FacilityTypesController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IFacilityAuthorizationService _authService;

    public FacilityTypesController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IFacilityAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FacilityTypeDto>>>> GetFacilityTypes()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityTypeDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view facility types");

            var facilityTypes = await tenantContext.FacilityTypes
                .Where(ft => ft.IsActive)
                .OrderBy(ft => ft.SortOrder)
                .ThenBy(ft => ft.Name)
                .Select(ft => new FacilityTypeDto
                {
                    Id = ft.Id,
                    Name = ft.Name,
                    Description = ft.Description,
                    Icon = ft.Icon,
                    PropertySchema = ft.PropertySchema,
                    IsActive = ft.IsActive,
                    SortOrder = ft.SortOrder,
                    AllowedMembershipTiers = ft.AllowedMembershipTiers,
                    RequiredCertifications = ft.RequiredCertifications,
                    RequiresSupervision = ft.RequiresSupervision,
                    CreatedAt = ft.CreatedAt,
                    UpdatedAt = ft.UpdatedAt ?? ft.CreatedAt,
                    FacilityCount = tenantContext.Facilities.Count(f => f.FacilityTypeId == ft.Id),
                    AvailableCount = tenantContext.Facilities.Count(f => f.FacilityTypeId == ft.Id && f.Status == FacilityStatus.Available),
                    BookedCount = tenantContext.Facilities.Count(f => f.FacilityTypeId == ft.Id && f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)),
                    MaintenanceCount = tenantContext.Facilities.Count(f => f.FacilityTypeId == ft.Id && f.Status == FacilityStatus.Maintenance)
                })
                .ToListAsync();

            return Ok(ApiResponse<List<FacilityTypeDto>>.SuccessResult(facilityTypes));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityTypeDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityTypeDto>>.ErrorResult($"Error retrieving facility types: {ex.Message}"));
        }
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<FacilityPermissions>>> GetPermissions()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityPermissions>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetPermissionsAsync(userId, tenantContext);
            return Ok(ApiResponse<FacilityPermissions>.SuccessResult(permissions));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityPermissions>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityPermissions>.ErrorResult($"Error: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FacilityTypeDto>>> GetFacilityType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityTypeDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view facility type");

            var facilityType = await tenantContext.FacilityTypes
                .FirstOrDefaultAsync(ft => ft.Id == id);

            if (facilityType == null)
                return NotFound(ApiResponse<FacilityTypeDto>.ErrorResult("Facility type not found"));

            var dto = new FacilityTypeDto
            {
                Id = facilityType.Id,
                Name = facilityType.Name,
                Description = facilityType.Description,
                Icon = facilityType.Icon,
                PropertySchema = facilityType.PropertySchema,
                IsActive = facilityType.IsActive,
                SortOrder = facilityType.SortOrder,
                AllowedMembershipTiers = facilityType.AllowedMembershipTiers,
                RequiredCertifications = facilityType.RequiredCertifications,
                RequiresSupervision = facilityType.RequiresSupervision,
                CreatedAt = facilityType.CreatedAt,
                UpdatedAt = facilityType.UpdatedAt ?? facilityType.CreatedAt,
                FacilityCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id),
                AvailableCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id && f.Status == FacilityStatus.Available),
                BookedCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id && f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)),
                MaintenanceCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id && f.Status == FacilityStatus.Maintenance)
            };

            return Ok(ApiResponse<FacilityTypeDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityTypeDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityTypeDto>.ErrorResult($"Error retrieving facility type: {ex.Message}"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityTypeDto>>> CreateFacilityType([FromBody] CreateFacilityTypeRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityTypeDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.CreateType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Check if name already exists
            var existingType = await tenantContext.FacilityTypes
                .FirstOrDefaultAsync(ft => ft.Name.ToLower() == request.Name.ToLower());

            if (existingType != null)
                return BadRequest(ApiResponse<FacilityTypeDto>.ErrorResult("Facility type with this name already exists"));

            var facilityType = new FacilityType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                Icon = request.Icon,
                PropertySchema = request.PropertySchema,
                IsActive = true,
                SortOrder = request.SortOrder,
                AllowedMembershipTiers = request.AllowedMembershipTiers,
                RequiredCertifications = request.RequiredCertifications,
                RequiresSupervision = request.RequiresSupervision,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.FacilityTypes.Add(facilityType);
            await tenantContext.SaveChangesAsync();

            var dto = new FacilityTypeDto
            {
                Id = facilityType.Id,
                Name = facilityType.Name,
                Description = facilityType.Description,
                Icon = facilityType.Icon,
                PropertySchema = facilityType.PropertySchema,
                IsActive = facilityType.IsActive,
                SortOrder = facilityType.SortOrder,
                AllowedMembershipTiers = facilityType.AllowedMembershipTiers,
                RequiredCertifications = facilityType.RequiredCertifications,
                RequiresSupervision = facilityType.RequiresSupervision,
                CreatedAt = facilityType.CreatedAt,
                UpdatedAt = facilityType.UpdatedAt ?? facilityType.CreatedAt,
                FacilityCount = 0,
                AvailableCount = 0,
                BookedCount = 0,
                MaintenanceCount = 0
            };

            return CreatedAtAction(nameof(GetFacilityType), new { id = facilityType.Id }, ApiResponse<FacilityTypeDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityTypeDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityTypeDto>.ErrorResult($"Error creating facility type: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityTypeDto>>> UpdateFacilityType(Guid id, [FromBody] UpdateFacilityTypeRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityTypeDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.EditType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var facilityType = await tenantContext.FacilityTypes.FirstOrDefaultAsync(ft => ft.Id == id);
            if (facilityType == null)
                return NotFound(ApiResponse<FacilityTypeDto>.ErrorResult("Facility type not found"));

            // Check if name already exists (excluding current type)
            var existingType = await tenantContext.FacilityTypes
                .FirstOrDefaultAsync(ft => ft.Id != id && ft.Name.ToLower() == request.Name.ToLower());

            if (existingType != null)
                return BadRequest(ApiResponse<FacilityTypeDto>.ErrorResult("Facility type with this name already exists"));

            // Update facility type properties
            facilityType.Name = request.Name;
            facilityType.Description = request.Description;
            facilityType.Icon = request.Icon;
            facilityType.PropertySchema = request.PropertySchema;
            facilityType.IsActive = request.IsActive;
            facilityType.SortOrder = request.SortOrder;
            facilityType.AllowedMembershipTiers = request.AllowedMembershipTiers;
            facilityType.RequiredCertifications = request.RequiredCertifications;
            facilityType.RequiresSupervision = request.RequiresSupervision;
            facilityType.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            var dto = new FacilityTypeDto
            {
                Id = facilityType.Id,
                Name = facilityType.Name,
                Description = facilityType.Description,
                Icon = facilityType.Icon,
                PropertySchema = facilityType.PropertySchema,
                IsActive = facilityType.IsActive,
                SortOrder = facilityType.SortOrder,
                AllowedMembershipTiers = facilityType.AllowedMembershipTiers,
                RequiredCertifications = facilityType.RequiredCertifications,
                RequiresSupervision = facilityType.RequiresSupervision,
                CreatedAt = facilityType.CreatedAt,
                UpdatedAt = facilityType.UpdatedAt ?? facilityType.CreatedAt,
                FacilityCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id),
                AvailableCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id && f.Status == FacilityStatus.Available),
                BookedCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id && f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)),
                MaintenanceCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == facilityType.Id && f.Status == FacilityStatus.Maintenance)
            };

            return Ok(ApiResponse<FacilityTypeDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityTypeDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityTypeDto>.ErrorResult($"Error updating facility type: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFacilityType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.DeleteType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var facilityType = await tenantContext.FacilityTypes.FirstOrDefaultAsync(ft => ft.Id == id);
            if (facilityType == null)
                return NotFound(ApiResponse<object>.ErrorResult("Facility type not found"));

            // Check if facility type is being used
            var facilityCount = await tenantContext.Facilities.CountAsync(f => f.FacilityTypeId == id);
            if (facilityCount > 0)
                return BadRequest(ApiResponse<object>.ErrorResult($"Cannot delete facility type with {facilityCount} facilities"));

            tenantContext.FacilityTypes.Remove(facilityType);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Facility type deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deleting facility type: {ex.Message}"));
        }
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateFacilityType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.EditType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var facilityType = await tenantContext.FacilityTypes.FirstOrDefaultAsync(ft => ft.Id == id);
            if (facilityType == null)
                return NotFound(ApiResponse<object>.ErrorResult("Facility type not found"));

            facilityType.IsActive = false;
            facilityType.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Facility type deactivated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deactivating facility type: {ex.Message}"));
        }
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateFacilityType(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.EditType, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var facilityType = await tenantContext.FacilityTypes.FirstOrDefaultAsync(ft => ft.Id == id);
            if (facilityType == null)
                return NotFound(ApiResponse<object>.ErrorResult("Facility type not found"));

            facilityType.IsActive = true;
            facilityType.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Facility type activated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error activating facility type: {ex.Message}"));
        }
    }
}