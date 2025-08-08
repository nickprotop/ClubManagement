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
public class HardwareAssignmentController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IHardwareAuthorizationService _authService;

    public HardwareAssignmentController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IHardwareAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<HardwareAssignmentDto>>>> GetAssignments([FromQuery] HardwareAssignmentFilter filter)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<HardwareAssignmentDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid();

            var query = tenantContext.HardwareAssignments
                .Include(a => a.Hardware)
                    .ThenInclude(h => h.HardwareType)
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.AssignedByUser)
                .Include(a => a.ReturnedByUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(a => 
                    a.Hardware.Name.Contains(filter.Search) ||
                    a.Hardware.SerialNumber.Contains(filter.Search) ||
                    a.Member.User.FirstName.Contains(filter.Search) ||
                    a.Member.User.LastName.Contains(filter.Search) ||
                    a.Member.User.Email.Contains(filter.Search));
            }

            if (filter.HardwareId.HasValue)
                query = query.Where(a => a.HardwareId == filter.HardwareId.Value);

            if (filter.MemberId.HasValue)
                query = query.Where(a => a.MemberId == filter.MemberId.Value);

            if (filter.Status.HasValue)
                query = query.Where(a => a.Status == filter.Status.Value);

            if (filter.AssignedAfter.HasValue)
                query = query.Where(a => a.AssignedAt >= filter.AssignedAfter.Value);

            if (filter.AssignedBefore.HasValue)
                query = query.Where(a => a.AssignedAt <= filter.AssignedBefore.Value);

            if (filter.ReturnedAfter.HasValue)
                query = query.Where(a => a.ReturnedAt >= filter.ReturnedAfter.Value);

            if (filter.ReturnedBefore.HasValue)
                query = query.Where(a => a.ReturnedAt <= filter.ReturnedBefore.Value);

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
                query = query.Where(a => a.Status == AssignmentStatus.Overdue);

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value)
                    query = query.Where(a => a.Status == AssignmentStatus.Active);
                else
                    query = query.Where(a => a.Status != AssignmentStatus.Active);
            }

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "hardware" => filter.SortDescending ? query.OrderByDescending(a => a.Hardware.Name) : query.OrderBy(a => a.Hardware.Name),
                "member" => filter.SortDescending ? query.OrderByDescending(a => a.Member.User.FirstName).ThenByDescending(a => a.Member.User.LastName) : query.OrderBy(a => a.Member.User.FirstName).ThenBy(a => a.Member.User.LastName),
                "status" => filter.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                "assigneddate" => filter.SortDescending ? query.OrderByDescending(a => a.AssignedAt) : query.OrderBy(a => a.AssignedAt),
                "returneddate" => filter.SortDescending ? query.OrderByDescending(a => a.ReturnedAt) : query.OrderBy(a => a.ReturnedAt),
                _ => query.OrderByDescending(a => a.AssignedAt)
            };

            var totalCount = await query.CountAsync();
            var assignments = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new HardwareAssignmentDto
                {
                    Id = a.Id,
                    HardwareId = a.HardwareId,
                    HardwareName = a.Hardware.Name,
                    HardwareSerialNumber = a.Hardware.SerialNumber,
                    HardwareTypeName = a.Hardware.HardwareType.Name,
                    MemberId = a.MemberId,
                    MemberName = (a.Member.User.FirstName ?? "") + " " + (a.Member.User.LastName ?? ""),
                    MemberEmail = a.Member.User.Email,
                    AssignedByUserId = a.AssignedByUserId,
                    AssignedByName = (a.AssignedByUser.FirstName ?? "") + " " + (a.AssignedByUser.LastName ?? ""),
                    AssignedAt = a.AssignedAt,
                    ReturnedAt = a.ReturnedAt,
                    ReturnedByUserId = a.ReturnedByUserId,
                    ReturnedByName = a.ReturnedByUser != null ? (a.ReturnedByUser.FirstName ?? "") + " " + (a.ReturnedByUser.LastName ?? "") : null,
                    Status = a.Status,
                    Notes = a.Notes,
                    ReturnNotes = a.ReturnNotes,
                    LateFee = a.LateFee,
                    DamageFee = a.DamageFee,
                    DaysAssigned = (int)(DateTime.UtcNow - a.AssignedAt).TotalDays,
                    IsOverdue = a.Status == AssignmentStatus.Overdue
                })
                .ToListAsync();

            var result = new PagedResult<HardwareAssignmentDto>
            {
                Items = assignments,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Ok(ApiResponse<PagedResult<HardwareAssignmentDto>>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PagedResult<HardwareAssignmentDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<HardwareAssignmentDto>>.ErrorResult($"Error retrieving assignments: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HardwareAssignmentDto>>> GetAssignment(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid();

            var assignment = await tenantContext.HardwareAssignments
                .Include(a => a.Hardware)
                    .ThenInclude(h => h.HardwareType)
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.AssignedByUser)
                .Include(a => a.ReturnedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
                return NotFound(ApiResponse<HardwareAssignmentDto>.ErrorResult("Assignment not found"));

            var dto = new HardwareAssignmentDto
            {
                Id = assignment.Id,
                HardwareId = assignment.HardwareId,
                HardwareName = assignment.Hardware.Name,
                HardwareSerialNumber = assignment.Hardware.SerialNumber,
                HardwareTypeName = assignment.Hardware.HardwareType.Name,
                MemberId = assignment.MemberId,
                MemberName = (assignment.Member.User.FirstName ?? "") + " " + (assignment.Member.User.LastName ?? ""),
                MemberEmail = assignment.Member.User.Email,
                AssignedByUserId = assignment.AssignedByUserId,
                AssignedByName = (assignment.AssignedByUser.FirstName ?? "") + " " + (assignment.AssignedByUser.LastName ?? ""),
                AssignedAt = assignment.AssignedAt,
                ReturnedAt = assignment.ReturnedAt,
                ReturnedByUserId = assignment.ReturnedByUserId,
                ReturnedByName = assignment.ReturnedByUser != null ? (assignment.ReturnedByUser.FirstName ?? "") + " " + (assignment.ReturnedByUser.LastName ?? "") : null,
                Status = assignment.Status,
                Notes = assignment.Notes,
                ReturnNotes = assignment.ReturnNotes,
                LateFee = assignment.LateFee,
                DamageFee = assignment.DamageFee,
                DaysAssigned = (int)(DateTime.UtcNow - assignment.AssignedAt).TotalDays,
                IsOverdue = assignment.Status == AssignmentStatus.Overdue
            };

            return Ok(ApiResponse<HardwareAssignmentDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareAssignmentDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareAssignmentDto>.ErrorResult($"Error retrieving assignment: {ex.Message}"));
        }
    }

    [HttpPost("assign")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<HardwareAssignmentDto>>> AssignHardware([FromBody] AssignHardwareRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.Assign, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Validate hardware exists and is available
            var hardware = await tenantContext.Hardware
                .Include(h => h.Assignments.Where(a => a.Status == AssignmentStatus.Active))
                .FirstOrDefaultAsync(h => h.Id == request.HardwareId);

            if (hardware == null)
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Hardware not found"));

            if (hardware.Status != HardwareStatus.Available)
            {
                var statusMessage = hardware.Status switch
                {
                    HardwareStatus.Unavailable => "Hardware is marked as unavailable and cannot be assigned.",
                    HardwareStatus.InUse => "Hardware is currently in use and cannot be assigned.",
                    HardwareStatus.Maintenance => "Hardware is under maintenance and cannot be assigned.",
                    HardwareStatus.OutOfOrder => "Hardware is out of order and cannot be assigned.",
                    HardwareStatus.OutOfService => "Hardware is out of service and cannot be assigned.",
                    HardwareStatus.Lost => "Hardware is marked as lost and cannot be assigned.",
                    HardwareStatus.Retired => "Hardware has been retired and cannot be assigned.",
                    _ => $"Hardware status '{hardware.Status}' does not allow assignment. Status must be 'Available'."
                };
                
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult(statusMessage));
            }

            if (hardware.Assignments.Any(a => a.Status == AssignmentStatus.Active))
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Hardware is already assigned to another member"));

            // Validate member exists
            var member = await tenantContext.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == request.MemberId);

            if (member == null)
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Member not found"));

            // Create assignment
            var assignment = new HardwareAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                HardwareId = request.HardwareId,
                MemberId = request.MemberId,
                AssignedByUserId = userId,
                AssignedAt = DateTime.UtcNow,
                Status = AssignmentStatus.Active,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            // Note: Hardware status remains unchanged - staff controls it manually
            hardware.UpdatedAt = DateTime.UtcNow;

            tenantContext.HardwareAssignments.Add(assignment);
            await tenantContext.SaveChangesAsync();

            // Return the created assignment with full details
            var createdAssignment = await tenantContext.HardwareAssignments
                .Include(a => a.Hardware)
                    .ThenInclude(h => h.HardwareType)
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.AssignedByUser)
                .FirstOrDefaultAsync(a => a.Id == assignment.Id);

            var dto = new HardwareAssignmentDto
            {
                Id = createdAssignment!.Id,
                HardwareId = createdAssignment.HardwareId,
                HardwareName = createdAssignment.Hardware.Name,
                HardwareSerialNumber = createdAssignment.Hardware.SerialNumber,
                HardwareTypeName = createdAssignment.Hardware.HardwareType.Name,
                MemberId = createdAssignment.MemberId,
                MemberName = (createdAssignment.Member.User.FirstName ?? "") + " " + (createdAssignment.Member.User.LastName ?? ""),
                MemberEmail = createdAssignment.Member.User.Email,
                AssignedByUserId = createdAssignment.AssignedByUserId,
                AssignedByName = (createdAssignment.AssignedByUser.FirstName ?? "") + " " + (createdAssignment.AssignedByUser.LastName ?? ""),
                AssignedAt = createdAssignment.AssignedAt,
                Status = createdAssignment.Status,
                Notes = createdAssignment.Notes,
                DaysAssigned = 0,
                IsOverdue = false
            };

            return CreatedAtAction(nameof(GetAssignment), new { id = assignment.Id }, ApiResponse<HardwareAssignmentDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareAssignmentDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareAssignmentDto>.ErrorResult($"Error creating assignment: {ex.Message}"));
        }
    }

    [HttpPost("{id}/return")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<HardwareAssignmentDto>>> ReturnHardware(Guid id, [FromBody] ReturnHardwareRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, HardwareAction.Assign, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var assignment = await tenantContext.HardwareAssignments
                .Include(a => a.Hardware)
                    .ThenInclude(h => h.HardwareType)
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.AssignedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
                return NotFound(ApiResponse<HardwareAssignmentDto>.ErrorResult("Assignment not found"));

            if (assignment.Status != AssignmentStatus.Active && assignment.Status != AssignmentStatus.Overdue)
                return BadRequest(ApiResponse<HardwareAssignmentDto>.ErrorResult("Assignment is not active and cannot be returned"));

            // Update assignment
            assignment.ReturnedAt = DateTime.UtcNow;
            assignment.ReturnedByUserId = userId;
            assignment.Status = request.Status ?? AssignmentStatus.Returned;
            assignment.ReturnNotes = request.ReturnNotes;
            assignment.LateFee = request.LateFee;
            assignment.DamageFee = request.DamageFee;
            assignment.UpdatedAt = DateTime.UtcNow;

            // Note: Hardware status remains unchanged - staff controls it manually
            // Staff can separately change hardware status if needed (e.g., to Maintenance if damaged)
            assignment.Hardware.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            // Reload with return user info
            await tenantContext.Entry(assignment).Reference(a => a.ReturnedByUser).LoadAsync();

            var dto = new HardwareAssignmentDto
            {
                Id = assignment.Id,
                HardwareId = assignment.HardwareId,
                HardwareName = assignment.Hardware.Name,
                HardwareSerialNumber = assignment.Hardware.SerialNumber,
                HardwareTypeName = assignment.Hardware.HardwareType.Name,
                MemberId = assignment.MemberId,
                MemberName = (assignment.Member.User.FirstName ?? "") + " " + (assignment.Member.User.LastName ?? ""),
                MemberEmail = assignment.Member.User.Email,
                AssignedByUserId = assignment.AssignedByUserId,
                AssignedByName = (assignment.AssignedByUser.FirstName ?? "") + " " + (assignment.AssignedByUser.LastName ?? ""),
                AssignedAt = assignment.AssignedAt,
                ReturnedAt = assignment.ReturnedAt,
                ReturnedByUserId = assignment.ReturnedByUserId,
                ReturnedByName = assignment.ReturnedByUser != null ? (assignment.ReturnedByUser.FirstName ?? "") + " " + (assignment.ReturnedByUser.LastName ?? "") : null,
                Status = assignment.Status,
                Notes = assignment.Notes,
                ReturnNotes = assignment.ReturnNotes,
                LateFee = assignment.LateFee,
                DamageFee = assignment.DamageFee,
                DaysAssigned = assignment.ReturnedAt.HasValue ? (int)(assignment.ReturnedAt.Value - assignment.AssignedAt).TotalDays : (int)(DateTime.UtcNow - assignment.AssignedAt).TotalDays,
                IsOverdue = false
            };

            return Ok(ApiResponse<HardwareAssignmentDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareAssignmentDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareAssignmentDto>.ErrorResult($"Error returning hardware: {ex.Message}"));
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<HardwareAssignmentDashboardDto>>> GetDashboard()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<HardwareAssignmentDashboardDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewHardwareAsync(userId, tenantContext))
                return Forbid();

            var totalAssignments = await tenantContext.HardwareAssignments.CountAsync();
            var activeAssignments = await tenantContext.HardwareAssignments.CountAsync(a => a.Status == AssignmentStatus.Active);
            var overdueAssignments = await tenantContext.HardwareAssignments.CountAsync(a => a.Status == AssignmentStatus.Overdue);
            var returnedThisMonth = await tenantContext.HardwareAssignments.CountAsync(a => 
                a.Status == AssignmentStatus.Returned && 
                a.ReturnedAt.HasValue && 
                a.ReturnedAt.Value >= DateTime.UtcNow.AddDays(-30));
            var damagedReturns = await tenantContext.HardwareAssignments.CountAsync(a => a.Status == AssignmentStatus.Damaged);
            var lostItems = await tenantContext.HardwareAssignments.CountAsync(a => a.Status == AssignmentStatus.Lost);
            var totalFees = await tenantContext.HardwareAssignments
                .Where(a => a.LateFee.HasValue || a.DamageFee.HasValue)
                .SumAsync(a => (a.LateFee ?? 0) + (a.DamageFee ?? 0));

            var dashboard = new HardwareAssignmentDashboardDto
            {
                TotalAssignments = totalAssignments,
                ActiveAssignments = activeAssignments,
                OverdueAssignments = overdueAssignments,
                ReturnedThisMonth = returnedThisMonth,
                DamagedReturns = damagedReturns,
                LostItems = lostItems,
                TotalFeesCollected = totalFees
            };

            return Ok(ApiResponse<HardwareAssignmentDashboardDto>.SuccessResult(dashboard));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<HardwareAssignmentDashboardDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<HardwareAssignmentDashboardDto>.ErrorResult($"Error retrieving dashboard: {ex.Message}"));
        }
    }
}