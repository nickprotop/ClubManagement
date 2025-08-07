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
public class HardwareController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IHardwareAuthorizationService _authService;

    public HardwareController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IHardwareAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<HardwareDto>>>> GetHardware([FromQuery] HardwareListFilter filter)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<HardwareDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view hardware");

            var query = tenantContext.Hardware
                .Include(h => h.HardwareType)
                .Include(h => h.Assignments.Where(a => a.Status == AssignmentStatus.Active))
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(h => h.Name.Contains(filter.Search) || 
                                       h.SerialNumber.Contains(filter.Search) ||
                                       h.HardwareType.Name.Contains(filter.Search));
            }

            if (filter.HardwareTypeId.HasValue)
                query = query.Where(h => h.HardwareTypeId == filter.HardwareTypeId.Value);

            if (filter.Status.HasValue)
                query = query.Where(h => h.Status == filter.Status.Value);

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(h => h.Location != null && h.Location.Contains(filter.Location));

            if (filter.Available.HasValue)
            {
                if (filter.Available.Value)
                    query = query.Where(h => h.Status == HardwareStatus.Available && !h.Assignments.Any(a => a.Status == AssignmentStatus.Active));
                else
                    query = query.Where(h => h.Status != HardwareStatus.Available || h.Assignments.Any(a => a.Status == AssignmentStatus.Active));
            }

            if (filter.MaintenanceDue.HasValue && filter.MaintenanceDue.Value)
                query = query.Where(h => h.NextMaintenanceDate.HasValue && h.NextMaintenanceDate.Value <= DateTime.UtcNow);

            if (filter.PurchasedAfter.HasValue)
                query = query.Where(h => h.PurchaseDate >= filter.PurchasedAfter.Value);

            if (filter.PurchasedBefore.HasValue)
                query = query.Where(h => h.PurchaseDate <= filter.PurchasedBefore.Value);

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name),
                "type" => filter.SortDescending ? query.OrderByDescending(h => h.HardwareType.Name) : query.OrderBy(h => h.HardwareType.Name),
                "status" => filter.SortDescending ? query.OrderByDescending(h => h.Status) : query.OrderBy(h => h.Status),
                "location" => filter.SortDescending ? query.OrderByDescending(h => h.Location) : query.OrderBy(h => h.Location),
                "purchasedate" => filter.SortDescending ? query.OrderByDescending(h => h.PurchaseDate) : query.OrderBy(h => h.PurchaseDate),
                _ => query.OrderBy(h => h.Name)
            };

            var totalCount = await query.CountAsync();
            var hardware = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(h => new HardwareDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    SerialNumber = h.SerialNumber,
                    HardwareTypeId = h.HardwareTypeId,
                    HardwareTypeName = h.HardwareType.Name,
                    HardwareTypeIcon = h.HardwareType.Icon,
                    Properties = h.Properties,
                    Status = h.Status,
                    PurchaseDate = h.PurchaseDate,
                    PurchasePrice = h.PurchasePrice,
                    LastMaintenanceDate = h.LastMaintenanceDate,
                    NextMaintenanceDate = h.NextMaintenanceDate,
                    Location = h.Location,
                    Notes = h.Notes,
                    CreatedAt = h.CreatedAt,
                    UpdatedAt = h.UpdatedAt ?? h.CreatedAt,
                    IsCurrentlyAssigned = h.Assignments.Any(a => a.Status == AssignmentStatus.Active),
                    CurrentAssignment = h.Assignments.Where(a => a.Status == AssignmentStatus.Active).Select(a => new HardwareAssignmentDto
                    {
                        Id = a.Id,
                        MemberId = a.MemberId,
                        MemberName = (a.Member.User.FirstName ?? "") + " " + (a.Member.User.LastName ?? ""),
                        MemberEmail = a.Member.User.Email,
                        AssignedAt = a.AssignedAt,
                        Status = a.Status,
                        Notes = a.Notes
                    }).FirstOrDefault(),
                    IsAvailableForAssignment = h.Status == HardwareStatus.Available && !h.Assignments.Any(a => a.Status == AssignmentStatus.Active),
                    IsMaintenanceDue = h.NextMaintenanceDate.HasValue && h.NextMaintenanceDate.Value <= DateTime.UtcNow,
                    DaysUntilMaintenance = h.NextMaintenanceDate.HasValue ? (int)(h.NextMaintenanceDate.Value - DateTime.UtcNow).TotalDays : int.MaxValue
                })
                .ToListAsync();

            var result = new PagedResult<HardwareDto>
            {
                Items = hardware,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Ok(ApiResponse<PagedResult<HardwareDto>>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PagedResult<HardwareDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<HardwareDto>>.ErrorResult($"Error retrieving hardware: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HardwareDto>>> GetHardware(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext, id))
                return Forbid("Insufficient permissions to view this hardware");

            var hardware = await tenantContext.Hardware
                .Include(h => h.HardwareType)
                .Include(h => h.Assignments)
                    .ThenInclude(a => a.Member)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hardware == null)
                return NotFound(ApiResponse<HardwareDto>.ErrorResult("Hardware not found"));

            var dto = new HardwareDto
            {
                Id = hardware.Id,
                Name = hardware.Name,
                SerialNumber = hardware.SerialNumber,
                HardwareTypeId = hardware.HardwareTypeId,
                HardwareTypeName = hardware.HardwareType.Name,
                HardwareTypeIcon = hardware.HardwareType.Icon,
                Properties = hardware.Properties,
                Status = hardware.Status,
                PurchaseDate = hardware.PurchaseDate,
                PurchasePrice = hardware.PurchasePrice,
                LastMaintenanceDate = hardware.LastMaintenanceDate,
                NextMaintenanceDate = hardware.NextMaintenanceDate,
                Location = hardware.Location,
                Notes = hardware.Notes,
                CreatedAt = hardware.CreatedAt,
                UpdatedAt = hardware.UpdatedAt ?? hardware.CreatedAt,
                IsCurrentlyAssigned = hardware.Assignments.Any(a => a.Status == AssignmentStatus.Active),
                CurrentAssignment = hardware.Assignments.Where(a => a.Status == AssignmentStatus.Active).Select(a => new HardwareAssignmentDto
                {
                    Id = a.Id,
                    MemberId = a.MemberId,
                    MemberName = (a.Member.User.FirstName ?? "") + " " + (a.Member.User.LastName ?? ""),
                    MemberEmail = a.Member.User.Email,
                    AssignedAt = a.AssignedAt,
                    Status = a.Status,
                    Notes = a.Notes,
                    DaysAssigned = (int)(DateTime.UtcNow - a.AssignedAt).TotalDays
                }).FirstOrDefault(),
                IsAvailableForAssignment = hardware.Status == HardwareStatus.Available && !hardware.Assignments.Any(a => a.Status == AssignmentStatus.Active),
                IsMaintenanceDue = hardware.NextMaintenanceDate.HasValue && hardware.NextMaintenanceDate.Value <= DateTime.UtcNow,
                DaysUntilMaintenance = hardware.NextMaintenanceDate.HasValue ? (int)(hardware.NextMaintenanceDate.Value - DateTime.UtcNow).TotalDays : int.MaxValue
            };

            return Ok(ApiResponse<HardwareDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareDto>.ErrorResult($"Error retrieving hardware: {ex.Message}"));
        }
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<HardwarePermissions>>> GetPermissions()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwarePermissions>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetPermissionsAsync(userId, tenantContext);
            return Ok(ApiResponse<HardwarePermissions>.SuccessResult(permissions));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwarePermissions>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwarePermissions>.ErrorResult($"Error: {ex.Message}"));
        }
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<HardwarePermissions>>> GetPermissions(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwarePermissions>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetPermissionsAsync(userId, tenantContext, id);
            return Ok(ApiResponse<HardwarePermissions>.SuccessResult(permissions));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwarePermissions>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwarePermissions>.ErrorResult($"Error: {ex.Message}"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<HardwareDto>>> CreateHardware([FromBody] CreateHardwareRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.Create, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Validate hardware type exists
            var hardwareType = await tenantContext.HardwareTypes.FirstOrDefaultAsync(ht => ht.Id == request.HardwareTypeId);
            if (hardwareType == null)
                return BadRequest(ApiResponse<HardwareDto>.ErrorResult("Invalid hardware type"));

            var hardware = new Hardware
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                SerialNumber = request.SerialNumber,
                HardwareTypeId = request.HardwareTypeId,
                Properties = request.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                Status = HardwareStatus.Available,
                PurchaseDate = request.PurchaseDate ?? DateTime.UtcNow,
                PurchasePrice = request.PurchasePrice,
                NextMaintenanceDate = request.NextMaintenanceDate,
                Location = request.Location,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.Hardware.Add(hardware);
            await tenantContext.SaveChangesAsync();

            // Return the created hardware with type info
            var createdHardware = await tenantContext.Hardware
                .Include(h => h.HardwareType)
                .FirstOrDefaultAsync(h => h.Id == hardware.Id);

            var dto = new HardwareDto
            {
                Id = createdHardware!.Id,
                Name = createdHardware.Name,
                SerialNumber = createdHardware.SerialNumber,
                HardwareTypeId = createdHardware.HardwareTypeId,
                HardwareTypeName = createdHardware.HardwareType.Name,
                HardwareTypeIcon = createdHardware.HardwareType.Icon,
                Properties = createdHardware.Properties,
                Status = createdHardware.Status,
                PurchaseDate = createdHardware.PurchaseDate,
                PurchasePrice = createdHardware.PurchasePrice,
                NextMaintenanceDate = createdHardware.NextMaintenanceDate,
                Location = createdHardware.Location,
                Notes = createdHardware.Notes,
                CreatedAt = createdHardware.CreatedAt,
                UpdatedAt = createdHardware.UpdatedAt ?? createdHardware.CreatedAt,
                IsAvailableForAssignment = true,
                IsMaintenanceDue = createdHardware.NextMaintenanceDate.HasValue && createdHardware.NextMaintenanceDate.Value <= DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetHardware), new { id = hardware.Id }, ApiResponse<HardwareDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareDto>.ErrorResult($"Error creating hardware: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<HardwareDto>>> UpdateHardware(Guid id, [FromBody] UpdateHardwareRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.Edit, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var hardware = await tenantContext.Hardware
                .Include(h => h.HardwareType)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hardware == null)
                return NotFound(ApiResponse<HardwareDto>.ErrorResult("Hardware not found"));

            // Update hardware properties
            hardware.Name = request.Name;
            hardware.SerialNumber = request.SerialNumber;
            hardware.Properties = request.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            hardware.Status = request.Status;
            hardware.LastMaintenanceDate = request.LastMaintenanceDate;
            hardware.NextMaintenanceDate = request.NextMaintenanceDate;
            hardware.Location = request.Location;
            hardware.Notes = request.Notes;
            hardware.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            var dto = new HardwareDto
            {
                Id = hardware.Id,
                Name = hardware.Name,
                SerialNumber = hardware.SerialNumber,
                HardwareTypeId = hardware.HardwareTypeId,
                HardwareTypeName = hardware.HardwareType.Name,
                HardwareTypeIcon = hardware.HardwareType.Icon,
                Properties = hardware.Properties,
                Status = hardware.Status,
                PurchaseDate = hardware.PurchaseDate,
                PurchasePrice = hardware.PurchasePrice,
                LastMaintenanceDate = hardware.LastMaintenanceDate,
                NextMaintenanceDate = hardware.NextMaintenanceDate,
                Location = hardware.Location,
                Notes = hardware.Notes,
                CreatedAt = hardware.CreatedAt,
                UpdatedAt = hardware.UpdatedAt ?? hardware.CreatedAt,
                IsMaintenanceDue = hardware.NextMaintenanceDate.HasValue && hardware.NextMaintenanceDate.Value <= DateTime.UtcNow
            };

            return Ok(ApiResponse<HardwareDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareDto>.ErrorResult($"Error updating hardware: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteHardware(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.Delete, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var hardware = await tenantContext.Hardware
                .Include(h => h.Assignments)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hardware == null)
                return NotFound(ApiResponse<object>.ErrorResult("Hardware not found"));

            // Check if hardware has active assignments
            if (hardware.Assignments.Any(a => a.Status == AssignmentStatus.Active))
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete hardware with active assignments"));

            tenantContext.Hardware.Remove(hardware);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Hardware deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deleting hardware: {ex.Message}"));
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<HardwareDto>>>> SearchHardware([FromQuery] string? searchTerm = null, [FromQuery] Guid? hardwareTypeId = null, [FromQuery] HardwareStatus? status = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<HardwareDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view hardware");

            var query = tenantContext.Hardware
                .Include(h => h.HardwareType)
                .Include(h => h.Assignments.Where(a => a.Status == AssignmentStatus.Active))
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(h => h.Name.Contains(searchTerm) || 
                                       h.SerialNumber.Contains(searchTerm) ||
                                       h.HardwareType.Name.Contains(searchTerm));
            }

            // Apply type filter
            if (hardwareTypeId.HasValue)
                query = query.Where(h => h.HardwareTypeId == hardwareTypeId.Value);

            // Apply status filter
            if (status.HasValue)
                query = query.Where(h => h.Status == status.Value);

            var hardware = await query
                .OrderBy(h => h.Name)
                .Take(50) // Limit results for search
                .Select(h => new HardwareDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    SerialNumber = h.SerialNumber,
                    HardwareTypeId = h.HardwareTypeId,
                    HardwareTypeName = h.HardwareType.Name,
                    HardwareTypeIcon = h.HardwareType.Icon,
                    Properties = h.Properties,
                    Status = h.Status,
                    PurchaseDate = h.PurchaseDate,
                    PurchasePrice = h.PurchasePrice,
                    LastMaintenanceDate = h.LastMaintenanceDate,
                    NextMaintenanceDate = h.NextMaintenanceDate,
                    Location = h.Location,
                    Notes = h.Notes,
                    CreatedAt = h.CreatedAt,
                    UpdatedAt = h.UpdatedAt ?? h.CreatedAt,
                    IsCurrentlyAssigned = h.Assignments.Any(a => a.Status == AssignmentStatus.Active),
                    CurrentAssignment = h.Assignments.Where(a => a.Status == AssignmentStatus.Active).Select(a => new HardwareAssignmentDto
                    {
                        Id = a.Id,
                        MemberId = a.MemberId,
                        MemberName = (a.Member.User.FirstName ?? "") + " " + (a.Member.User.LastName ?? ""),
                        MemberEmail = a.Member.User.Email,
                        AssignedAt = a.AssignedAt,
                        Status = a.Status,
                        Notes = a.Notes
                    }).FirstOrDefault(),
                    IsAvailableForAssignment = h.Status == HardwareStatus.Available && !h.Assignments.Any(a => a.Status == AssignmentStatus.Active),
                    IsMaintenanceDue = h.NextMaintenanceDate.HasValue && h.NextMaintenanceDate.Value <= DateTime.UtcNow,
                    DaysUntilMaintenance = h.NextMaintenanceDate.HasValue ? (int)(h.NextMaintenanceDate.Value - DateTime.UtcNow).TotalDays : int.MaxValue
                })
                .ToListAsync();

            return Ok(ApiResponse<List<HardwareDto>>.SuccessResult(hardware));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<HardwareDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<HardwareDto>>.ErrorResult($"Error searching hardware: {ex.Message}"));
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<HardwareDashboardDto>>> GetDashboard()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareDashboardDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid("Insufficient permissions to view hardware dashboard");

            var totalHardware = await tenantContext.Hardware.CountAsync();
            var availableHardware = await tenantContext.Hardware.CountAsync(h => h.Status == HardwareStatus.Available);
            var assignedHardware = await tenantContext.Hardware.CountAsync(h => h.Status == HardwareStatus.Assigned);
            var maintenanceHardware = await tenantContext.Hardware.CountAsync(h => h.Status == HardwareStatus.Maintenance);
            var outOfOrderHardware = await tenantContext.Hardware.CountAsync(h => h.Status == HardwareStatus.OutOfOrder);
            var totalAssetValue = await tenantContext.Hardware.Where(h => h.PurchasePrice.HasValue).SumAsync(h => h.PurchasePrice!.Value);
            var maintenanceDueCount = await tenantContext.Hardware.CountAsync(h => h.NextMaintenanceDate.HasValue && h.NextMaintenanceDate.Value <= DateTime.UtcNow);
            var overdueAssignments = await tenantContext.HardwareAssignments.CountAsync(a => a.Status == AssignmentStatus.Overdue);

            var dashboard = new HardwareDashboardDto
            {
                TotalHardware = totalHardware,
                AvailableHardware = availableHardware,
                AssignedHardware = assignedHardware,
                MaintenanceHardware = maintenanceHardware,
                OutOfOrderHardware = outOfOrderHardware,
                TotalAssetValue = totalAssetValue,
                MaintenanceDueCount = maintenanceDueCount,
                OverdueAssignments = overdueAssignments
            };

            return Ok(ApiResponse<HardwareDashboardDto>.SuccessResult(dashboard));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareDashboardDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareDashboardDto>.ErrorResult($"Error retrieving dashboard: {ex.Message}"));
        }
    }
}