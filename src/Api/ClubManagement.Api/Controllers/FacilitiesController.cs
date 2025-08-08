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
public class FacilitiesController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IFacilityAuthorizationService _authService;

    public FacilitiesController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IFacilityAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FacilityDto>>>> GetFacilities([FromQuery] FacilityListFilter filter)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid();

            var query = tenantContext.Facilities
                .Include(f => f.FacilityType)
                .Include(f => f.Bookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
                    .ThenInclude(b => b.Member)
                        .ThenInclude(m => m.User)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(f => f.Name.Contains(filter.Search) || 
                                       f.Description.Contains(filter.Search) ||
                                       f.FacilityType.Name.Contains(filter.Search));
            }

            if (filter.FacilityTypeId.HasValue)
                query = query.Where(f => f.FacilityTypeId == filter.FacilityTypeId.Value);

            if (filter.Status.HasValue)
                query = query.Where(f => f.Status == filter.Status.Value);

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(f => f.Location != null && f.Location.Contains(filter.Location));

            if (filter.RequiresBooking.HasValue)
                query = query.Where(f => f.RequiresBooking == filter.RequiresBooking.Value);

            if (filter.MinCapacity.HasValue)
                query = query.Where(f => f.Capacity.HasValue && f.Capacity >= filter.MinCapacity.Value);

            if (filter.MaxCapacity.HasValue)
                query = query.Where(f => f.Capacity.HasValue && f.Capacity <= filter.MaxCapacity.Value);

            if (filter.Available.HasValue && filter.Available.Value)
            {
                query = query.Where(f => f.Status == FacilityStatus.Available && 
                                        !f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
            }

            // Member-specific filtering
            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Member != null && filter.MembershipTier.HasValue)
            {
                query = query.Where(f => !f.AllowedMembershipTiers.Any() || 
                                        f.AllowedMembershipTiers.Contains(filter.MembershipTier.Value));
            }

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
                "type" => filter.SortDescending ? query.OrderByDescending(f => f.FacilityType.Name) : query.OrderBy(f => f.FacilityType.Name),
                "status" => filter.SortDescending ? query.OrderByDescending(f => f.Status) : query.OrderBy(f => f.Status),
                "location" => filter.SortDescending ? query.OrderByDescending(f => f.Location) : query.OrderBy(f => f.Location),
                "capacity" => filter.SortDescending ? query.OrderByDescending(f => f.Capacity) : query.OrderBy(f => f.Capacity),
                _ => query.OrderBy(f => f.Name)
            };

            var totalCount = await query.CountAsync();
            var facilities = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(f => new FacilityDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    FacilityTypeId = f.FacilityTypeId,
                    FacilityTypeName = f.FacilityType.Name,
                    FacilityTypeIcon = f.FacilityType.Icon,
                    Icon = f.Icon,
                    Properties = f.Properties,
                    Status = f.Status,
                    Capacity = f.Capacity,
                    HourlyRate = f.HourlyRate,
                    RequiresBooking = f.RequiresBooking,
                    MaxBookingDaysInAdvance = f.MaxBookingDaysInAdvance,
                    MinBookingDurationMinutes = f.MinBookingDurationMinutes,
                    MaxBookingDurationMinutes = f.MaxBookingDurationMinutes,
                    OperatingHoursStart = f.OperatingHoursStart,
                    OperatingHoursEnd = f.OperatingHoursEnd,
                    OperatingDays = f.OperatingDays,
                    AllowedMembershipTiers = f.AllowedMembershipTiers,
                    RequiredCertifications = f.RequiredCertifications,
                    MemberConcurrentBookingLimit = f.MemberConcurrentBookingLimit,
                    RequiresMemberSupervision = f.RequiresMemberSupervision,
                    MemberHourlyRate = f.MemberHourlyRate,
                    NonMemberHourlyRate = f.NonMemberHourlyRate,
                    Location = f.Location,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt ?? f.CreatedAt,
                    IsAvailableNow = f.Status == FacilityStatus.Available,
                    IsCurrentlyBooked = f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn),
                    ActiveBookingsCount = f.Bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn),
                    CurrentBooking = f.Bookings.Where(b => b.Status == BookingStatus.CheckedIn).Select(b => new FacilityBookingDto
                    {
                        Id = b.Id,
                        MemberId = b.MemberId,
                        MemberName = (b.Member.User.FirstName ?? "") + " " + (b.Member.User.LastName ?? ""),
                        MemberEmail = b.Member.User.Email,
                        StartDateTime = b.StartDateTime,
                        EndDateTime = b.EndDateTime,
                        Status = b.Status,
                        Purpose = b.Purpose,
                        ParticipantCount = b.ParticipantCount
                    }).FirstOrDefault()
                })
                .ToListAsync();

            // Check member access for each facility if user is a member
            if (user?.Member != null)
            {
                foreach (var facility in facilities)
                {
                    facility.CanMemberAccess = await _authService.CanMemberAccessFacilityAsync(userId, facility.Id, tenantContext);
                    if (!facility.CanMemberAccess)
                    {
                        var memberCertifications = await GetActiveMemberCertificationsAsync(userId, tenantContext);
                        facility.MissingCertifications = facility.RequiredCertifications
                            .Where(req => !memberCertifications.Any(cert => cert.CertificationType == req))
                            .ToList();
                    }
                }
            }

            var result = new PagedResult<FacilityDto>
            {
                Items = facilities,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Ok(ApiResponse<PagedResult<FacilityDto>>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PagedResult<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<FacilityDto>>.ErrorResult($"Error retrieving facilities: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FacilityDto>>> GetFacility(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext, id))
                return Forbid();

            var facility = await tenantContext.Facilities
                .Include(f => f.FacilityType)
                .Include(f => f.Bookings)
                    .ThenInclude(b => b.Member)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (facility == null)
                return NotFound(ApiResponse<FacilityDto>.ErrorResult("Facility not found"));

            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            var canMemberAccess = user?.Member != null ? 
                await _authService.CanMemberAccessFacilityAsync(userId, id, tenantContext) : true;

            var missingCertifications = new List<string>();
            if (user?.Member != null && !canMemberAccess)
            {
                var memberCertifications = await GetActiveMemberCertificationsAsync(userId, tenantContext);
                missingCertifications = facility.RequiredCertifications
                    .Where(req => !memberCertifications.Any(cert => cert.CertificationType == req))
                    .ToList();
            }

            var dto = new FacilityDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Description = facility.Description,
                FacilityTypeId = facility.FacilityTypeId,
                FacilityTypeName = facility.FacilityType.Name,
                FacilityTypeIcon = facility.FacilityType.Icon,
                Icon = facility.Icon,
                Properties = facility.Properties,
                Status = facility.Status,
                Capacity = facility.Capacity,
                HourlyRate = facility.HourlyRate,
                RequiresBooking = facility.RequiresBooking,
                MaxBookingDaysInAdvance = facility.MaxBookingDaysInAdvance,
                MinBookingDurationMinutes = facility.MinBookingDurationMinutes,
                MaxBookingDurationMinutes = facility.MaxBookingDurationMinutes,
                OperatingHoursStart = facility.OperatingHoursStart,
                OperatingHoursEnd = facility.OperatingHoursEnd,
                OperatingDays = facility.OperatingDays,
                AllowedMembershipTiers = facility.AllowedMembershipTiers,
                RequiredCertifications = facility.RequiredCertifications,
                MemberConcurrentBookingLimit = facility.MemberConcurrentBookingLimit,
                RequiresMemberSupervision = facility.RequiresMemberSupervision,
                MemberHourlyRate = facility.MemberHourlyRate,
                NonMemberHourlyRate = facility.NonMemberHourlyRate,
                Location = facility.Location,
                CreatedAt = facility.CreatedAt,
                UpdatedAt = facility.UpdatedAt ?? facility.CreatedAt,
                IsAvailableNow = facility.Status == FacilityStatus.Available,
                IsCurrentlyBooked = facility.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn),
                ActiveBookingsCount = facility.Bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn),
                CanMemberAccess = canMemberAccess,
                MissingCertifications = missingCertifications,
                CurrentBooking = facility.Bookings.Where(b => b.Status == BookingStatus.CheckedIn).Select(b => new FacilityBookingDto
                {
                    Id = b.Id,
                    MemberId = b.MemberId,
                    MemberName = (b.Member.User.FirstName ?? "") + " " + (b.Member.User.LastName ?? ""),
                    MemberEmail = b.Member.User.Email,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    Status = b.Status,
                    Purpose = b.Purpose,
                    ParticipantCount = b.ParticipantCount,
                    CheckedInAt = b.CheckedInAt
                }).FirstOrDefault()
            };

            return Ok(ApiResponse<FacilityDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityDto>.ErrorResult($"Error retrieving facility: {ex.Message}"));
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

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<FacilityPermissions>>> GetPermissions(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityPermissions>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetPermissionsAsync(userId, tenantContext, id);
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

    [HttpPost]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityDto>>> CreateFacility([FromBody] CreateFacilityRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Create, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Validate facility type exists
            var facilityType = await tenantContext.FacilityTypes.FirstOrDefaultAsync(ft => ft.Id == request.FacilityTypeId);
            if (facilityType == null)
                return BadRequest(ApiResponse<FacilityDto>.ErrorResult("Invalid facility type"));

            var facility = new Facility
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                FacilityTypeId = request.FacilityTypeId,
                Properties = request.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                Status = FacilityStatus.Available,
                Capacity = request.Capacity,
                HourlyRate = request.HourlyRate,
                RequiresBooking = request.RequiresBooking,
                MaxBookingDaysInAdvance = request.MaxBookingDaysInAdvance,
                MinBookingDurationMinutes = request.MinBookingDurationMinutes,
                MaxBookingDurationMinutes = request.MaxBookingDurationMinutes,
                OperatingHoursStart = request.OperatingHoursStart,
                OperatingHoursEnd = request.OperatingHoursEnd,
                OperatingDays = request.OperatingDays,
                AllowedMembershipTiers = request.AllowedMembershipTiers,
                RequiredCertifications = request.RequiredCertifications,
                MemberConcurrentBookingLimit = request.MemberConcurrentBookingLimit,
                RequiresMemberSupervision = request.RequiresMemberSupervision,
                MemberHourlyRate = request.MemberHourlyRate,
                NonMemberHourlyRate = request.NonMemberHourlyRate,
                Location = request.Location,
                Icon = request.Icon,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.Facilities.Add(facility);
            await tenantContext.SaveChangesAsync();

            // Return the created facility with type info
            var createdFacility = await tenantContext.Facilities
                .Include(f => f.FacilityType)
                .FirstOrDefaultAsync(f => f.Id == facility.Id);

            var dto = new FacilityDto
            {
                Id = createdFacility!.Id,
                Name = createdFacility.Name,
                Description = createdFacility.Description,
                FacilityTypeId = createdFacility.FacilityTypeId,
                FacilityTypeName = createdFacility.FacilityType.Name,
                FacilityTypeIcon = createdFacility.FacilityType.Icon,
                Icon = createdFacility.Icon,
                Properties = createdFacility.Properties,
                Status = createdFacility.Status,
                Capacity = createdFacility.Capacity,
                HourlyRate = createdFacility.HourlyRate,
                RequiresBooking = createdFacility.RequiresBooking,
                MaxBookingDaysInAdvance = createdFacility.MaxBookingDaysInAdvance,
                MinBookingDurationMinutes = createdFacility.MinBookingDurationMinutes,
                MaxBookingDurationMinutes = createdFacility.MaxBookingDurationMinutes,
                OperatingHoursStart = createdFacility.OperatingHoursStart,
                OperatingHoursEnd = createdFacility.OperatingHoursEnd,
                OperatingDays = createdFacility.OperatingDays,
                AllowedMembershipTiers = createdFacility.AllowedMembershipTiers,
                RequiredCertifications = createdFacility.RequiredCertifications,
                MemberConcurrentBookingLimit = createdFacility.MemberConcurrentBookingLimit,
                RequiresMemberSupervision = createdFacility.RequiresMemberSupervision,
                MemberHourlyRate = createdFacility.MemberHourlyRate,
                NonMemberHourlyRate = createdFacility.NonMemberHourlyRate,
                Location = createdFacility.Location,
                CreatedAt = createdFacility.CreatedAt,
                UpdatedAt = createdFacility.UpdatedAt ?? createdFacility.CreatedAt,
                IsAvailableNow = true,
                IsCurrentlyBooked = false,
                ActiveBookingsCount = 0
            };

            return CreatedAtAction(nameof(GetFacility), new { id = facility.Id }, ApiResponse<FacilityDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityDto>.ErrorResult($"Error creating facility: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityDto>>> UpdateFacility(Guid id, [FromBody] UpdateFacilityRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Edit, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var facility = await tenantContext.Facilities
                .Include(f => f.FacilityType)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (facility == null)
                return NotFound(ApiResponse<FacilityDto>.ErrorResult("Facility not found"));

            // Check for active bookings before allowing certain status changes
            var activeBookings = await tenantContext.FacilityBookings
                .Where(b => b.FacilityId == id && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .ToListAsync();

            if (activeBookings.Any() && request.Status != FacilityStatus.Available)
            {
                var bookingDetails = activeBookings.Select(b =>
                {
                    if (b.Member?.User != null)
                        return $"Member: {b.Member.User.FirstName} {b.Member.User.LastName}";
                    else
                        return "Unknown booking";
                }).ToList();

                return BadRequest(ApiResponse<FacilityDto>.ErrorResult(
                    $"Cannot change status while facility has active bookings: {string.Join(", ", bookingDetails)}. " +
                    $"Please process bookings first."));
            }

            // Update facility properties
            facility.Name = request.Name;
            facility.Description = request.Description;
            facility.Properties = request.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            facility.Status = request.Status;
            facility.Capacity = request.Capacity;
            facility.HourlyRate = request.HourlyRate;
            facility.RequiresBooking = request.RequiresBooking;
            facility.MaxBookingDaysInAdvance = request.MaxBookingDaysInAdvance;
            facility.MinBookingDurationMinutes = request.MinBookingDurationMinutes;
            facility.MaxBookingDurationMinutes = request.MaxBookingDurationMinutes;
            facility.OperatingHoursStart = request.OperatingHoursStart;
            facility.OperatingHoursEnd = request.OperatingHoursEnd;
            facility.OperatingDays = request.OperatingDays;
            facility.AllowedMembershipTiers = request.AllowedMembershipTiers;
            facility.RequiredCertifications = request.RequiredCertifications;
            facility.MemberConcurrentBookingLimit = request.MemberConcurrentBookingLimit;
            facility.RequiresMemberSupervision = request.RequiresMemberSupervision;
            facility.MemberHourlyRate = request.MemberHourlyRate;
            facility.NonMemberHourlyRate = request.NonMemberHourlyRate;
            facility.Location = request.Location;
            facility.Icon = request.Icon;
            facility.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            var dto = new FacilityDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Description = facility.Description,
                FacilityTypeId = facility.FacilityTypeId,
                FacilityTypeName = facility.FacilityType.Name,
                FacilityTypeIcon = facility.FacilityType.Icon,
                Icon = facility.Icon,
                Properties = facility.Properties,
                Status = facility.Status,
                Capacity = facility.Capacity,
                HourlyRate = facility.HourlyRate,
                RequiresBooking = facility.RequiresBooking,
                MaxBookingDaysInAdvance = facility.MaxBookingDaysInAdvance,
                MinBookingDurationMinutes = facility.MinBookingDurationMinutes,
                MaxBookingDurationMinutes = facility.MaxBookingDurationMinutes,
                OperatingHoursStart = facility.OperatingHoursStart,
                OperatingHoursEnd = facility.OperatingHoursEnd,
                OperatingDays = facility.OperatingDays,
                AllowedMembershipTiers = facility.AllowedMembershipTiers,
                RequiredCertifications = facility.RequiredCertifications,
                MemberConcurrentBookingLimit = facility.MemberConcurrentBookingLimit,
                RequiresMemberSupervision = facility.RequiresMemberSupervision,
                MemberHourlyRate = facility.MemberHourlyRate,
                NonMemberHourlyRate = facility.NonMemberHourlyRate,
                Location = facility.Location,
                CreatedAt = facility.CreatedAt,
                UpdatedAt = facility.UpdatedAt ?? facility.CreatedAt
            };

            return Ok(ApiResponse<FacilityDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityDto>.ErrorResult($"Error updating facility: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFacility(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Delete, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var facility = await tenantContext.Facilities
                .Include(f => f.Bookings)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (facility == null)
                return NotFound(ApiResponse<object>.ErrorResult("Facility not found"));

            // Check if facility has active bookings
            if (facility.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete facility with active bookings"));

            // Check if facility has any bookings at all (including past ones)
            if (facility.Bookings.Any())
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete facility with booking history. Consider retiring it instead."));

            tenantContext.Facilities.Remove(facility);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Facility deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deleting facility: {ex.Message}"));
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> SearchFacilities([FromQuery] string? searchTerm = null, [FromQuery] Guid? facilityTypeId = null, [FromQuery] FacilityStatus? status = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid();

            var query = tenantContext.Facilities
                .Include(f => f.FacilityType)
                .Include(f => f.Bookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
                .AsQueryable();

            // Apply search filter (case-insensitive)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(f => f.Name.ToLower().Contains(lowerSearchTerm) || 
                                       f.Description.ToLower().Contains(lowerSearchTerm) ||
                                       f.FacilityType.Name.ToLower().Contains(lowerSearchTerm));
            }

            // Apply type filter
            if (facilityTypeId.HasValue)
                query = query.Where(f => f.FacilityTypeId == facilityTypeId.Value);

            // Apply status filter
            if (status.HasValue)
                query = query.Where(f => f.Status == status.Value);

            var facilities = await query
                .OrderBy(f => f.Name)
                .Take(50) // Limit results for search
                .Select(f => new FacilityDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    FacilityTypeId = f.FacilityTypeId,
                    FacilityTypeName = f.FacilityType.Name,
                    FacilityTypeIcon = f.FacilityType.Icon,
                    Icon = f.Icon,
                    Properties = f.Properties,
                    Status = f.Status,
                    Capacity = f.Capacity,
                    HourlyRate = f.HourlyRate,
                    Location = f.Location,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt ?? f.CreatedAt,
                    IsAvailableNow = f.Status == FacilityStatus.Available,
                    IsCurrentlyBooked = f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn),
                    ActiveBookingsCount = f.Bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
                })
                .ToListAsync();

            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(facilities));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error searching facilities: {ex.Message}"));
        }
    }

    [HttpGet("type/{facilityTypeId}")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetFacilitiesByType(Guid facilityTypeId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid();

            var facilities = await tenantContext.Facilities
                .Where(f => f.FacilityTypeId == facilityTypeId)
                .Include(f => f.FacilityType)
                .Select(f => new FacilityDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    Status = f.Status,
                    FacilityTypeId = f.FacilityTypeId,
                    FacilityTypeName = f.FacilityType.Name,
                    FacilityTypeIcon = f.FacilityType.Icon,
                    Properties = f.Properties,
                    Icon = f.Icon,
                    Location = f.Location,
                    Capacity = f.Capacity,
                    HourlyRate = f.HourlyRate,
                    RequiresBooking = f.RequiresBooking,
                    MaxBookingDaysInAdvance = f.MaxBookingDaysInAdvance,
                    MinBookingDurationMinutes = f.MinBookingDurationMinutes,
                    MaxBookingDurationMinutes = f.MaxBookingDurationMinutes,
                    OperatingHoursStart = f.OperatingHoursStart,
                    OperatingHoursEnd = f.OperatingHoursEnd,
                    OperatingDays = f.OperatingDays,
                    AllowedMembershipTiers = f.AllowedMembershipTiers,
                    RequiredCertifications = f.RequiredCertifications,
                    MemberConcurrentBookingLimit = f.MemberConcurrentBookingLimit,
                    RequiresMemberSupervision = f.RequiresMemberSupervision,
                    MemberHourlyRate = f.MemberHourlyRate,
                    NonMemberHourlyRate = f.NonMemberHourlyRate,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(facilities));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error retrieving facilities by type: {ex.Message}"));
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<FacilityDashboardDto>>> GetDashboard()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityDashboardDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid();

            var totalFacilities = await tenantContext.Facilities.CountAsync();
            var availableFacilities = await tenantContext.Facilities.CountAsync(f => f.Status == FacilityStatus.Available);
            var bookedFacilities = await tenantContext.Facilities.CountAsync(f => f.Bookings.Any(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
            var maintenanceFacilities = await tenantContext.Facilities.CountAsync(f => f.Status == FacilityStatus.Maintenance);
            var outOfOrderFacilities = await tenantContext.Facilities.CountAsync(f => f.Status == FacilityStatus.OutOfOrder);

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var todaysBookings = await tenantContext.FacilityBookings
                .CountAsync(b => b.StartDateTime.Date == today);

            var activeBookings = await tenantContext.FacilityBookings
                .CountAsync(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn);

            var upcomingBookings = await tenantContext.FacilityBookings
                .CountAsync(b => b.Status == BookingStatus.Confirmed && b.StartDateTime >= DateTime.UtcNow && b.StartDateTime < tomorrow);

            var todaysRevenue = await tenantContext.FacilityBookings
                .Where(b => b.StartDateTime.Date == today && b.Cost.HasValue && b.Status != BookingStatus.Cancelled)
                .SumAsync(b => b.Cost!.Value);

            var monthlyRevenue = await tenantContext.FacilityBookings
                .Where(b => b.StartDateTime >= startOfMonth && b.StartDateTime < endOfMonth && 
                           b.Cost.HasValue && b.Status != BookingStatus.Cancelled)
                .SumAsync(b => b.Cost!.Value);

            // Calculate utilization rate
            var totalFacilityHours = totalFacilities * 24; // Simplified calculation
            var todayBookings = await tenantContext.FacilityBookings
                .Where(b => b.StartDateTime.Date == today && b.Status != BookingStatus.Cancelled)
                .Select(b => new { b.StartDateTime, b.EndDateTime })
                .ToListAsync();
            
            var bookedHours = todayBookings.Sum(b => (b.EndDateTime - b.StartDateTime).TotalHours);

            var utilizationRate = totalFacilityHours > 0 ? (bookedHours / totalFacilityHours) * 100 : 0;

            var dashboard = new FacilityDashboardDto
            {
                TotalFacilities = totalFacilities,
                AvailableFacilities = availableFacilities,
                BookedFacilities = bookedFacilities,
                MaintenanceFacilities = maintenanceFacilities,
                OutOfOrderFacilities = outOfOrderFacilities,
                TodaysBookings = todaysBookings,
                ActiveBookings = activeBookings,
                UpcomingBookings = upcomingBookings,
                TodaysRevenue = todaysRevenue,
                MonthlyRevenue = monthlyRevenue,
                AverageUtilizationRate = utilizationRate
            };

            return Ok(ApiResponse<FacilityDashboardDto>.SuccessResult(dashboard));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityDashboardDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityDashboardDto>.ErrorResult($"Error retrieving dashboard: {ex.Message}"));
        }
    }

    [HttpGet("available-for-member")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetAvailableFacilitiesForMember(
        [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanAccessMemberPortalAsync(userId, tenantContext))
                return Forbid();

            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Member == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("User is not a member"));

            var query = tenantContext.Facilities
                .Include(f => f.FacilityType)
                .Where(f => f.Status == FacilityStatus.Available && f.RequiresBooking);

            // Filter by membership tier access
            query = query.Where(f => !f.AllowedMembershipTiers.Any() || 
                                    f.AllowedMembershipTiers.Contains(user.Member.Tier));

            // Check for time conflicts
            query = query.Where(f => !f.Bookings.Any(b => 
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                ((startTime >= b.StartDateTime && startTime < b.EndDateTime) ||
                 (endTime > b.StartDateTime && endTime <= b.EndDateTime) ||
                 (startTime <= b.StartDateTime && endTime >= b.EndDateTime))));

            var facilities = await query
                .OrderBy(f => f.Name)
                .Select(f => new FacilityDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    FacilityTypeId = f.FacilityTypeId,
                    FacilityTypeName = f.FacilityType.Name,
                    FacilityTypeIcon = f.FacilityType.Icon,
                    Icon = f.Icon,
                    Status = f.Status,
                    Capacity = f.Capacity,
                    MemberHourlyRate = f.MemberHourlyRate,
                    Location = f.Location,
                    RequiredCertifications = f.RequiredCertifications,
                    MinBookingDurationMinutes = f.MinBookingDurationMinutes,
                    MaxBookingDurationMinutes = f.MaxBookingDurationMinutes,
                    OperatingHoursStart = f.OperatingHoursStart,
                    OperatingHoursEnd = f.OperatingHoursEnd,
                    OperatingDays = f.OperatingDays
                })
                .ToListAsync();

            // Check member access and certifications for each facility
            foreach (var facility in facilities)
            {
                facility.CanMemberAccess = await _authService.CanMemberAccessFacilityAsync(userId, facility.Id, tenantContext);
                if (!facility.CanMemberAccess)
                {
                    var memberCertifications = await GetActiveMemberCertificationsAsync(userId, tenantContext);
                    facility.MissingCertifications = facility.RequiredCertifications
                        .Where(req => !memberCertifications.Any(cert => cert.CertificationType == req))
                        .ToList();
                }
            }

            // Only return facilities the member can access
            var accessibleFacilities = facilities.Where(f => f.CanMemberAccess).ToList();

            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(accessibleFacilities));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error retrieving available facilities: {ex.Message}"));
        }
    }

    [HttpGet("available-for-member/{memberId}")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetAvailableFacilitiesForMemberById(
        Guid memberId, 
        [FromQuery] DateTime? startDateTime = null, 
        [FromQuery] DateTime? endDateTime = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Get the specified member
            var member = await tenantContext.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == memberId);
            
            if (member == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Member not found"));

            // Check if current user can access this member's data (self or staff)
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId && !await _authService.CanViewFacilitiesAsync(userId, tenantContext))
                return Forbid();

            var query = tenantContext.Facilities
                .Include(f => f.FacilityType)
                .Where(f => f.Status == FacilityStatus.Available);

            // Check for time conflicts if dates are provided
            if (startDateTime.HasValue && endDateTime.HasValue)
            {
                query = query.Where(f => !f.Bookings.Any(b => 
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                    ((startDateTime.Value >= b.StartDateTime && startDateTime.Value < b.EndDateTime) ||
                     (endDateTime.Value > b.StartDateTime && endDateTime.Value <= b.EndDateTime) ||
                     (startDateTime.Value <= b.StartDateTime && endDateTime.Value >= b.EndDateTime))));
            }

            var allFacilities = await query
                .OrderBy(f => f.Name)
                .Select(f => new FacilityDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    FacilityTypeId = f.FacilityTypeId,
                    FacilityTypeName = f.FacilityType.Name,
                    FacilityTypeIcon = f.FacilityType.Icon,
                    Icon = f.Icon,
                    Status = f.Status,
                    Capacity = f.Capacity,
                    HourlyRate = f.HourlyRate,
                    RequiresBooking = f.RequiresBooking,
                    MaxBookingDaysInAdvance = f.MaxBookingDaysInAdvance,
                    MinBookingDurationMinutes = f.MinBookingDurationMinutes,
                    MaxBookingDurationMinutes = f.MaxBookingDurationMinutes,
                    OperatingHoursStart = f.OperatingHoursStart,
                    OperatingHoursEnd = f.OperatingHoursEnd,
                    OperatingDays = f.OperatingDays,
                    AllowedMembershipTiers = f.AllowedMembershipTiers,
                    RequiredCertifications = f.RequiredCertifications,
                    MemberConcurrentBookingLimit = f.MemberConcurrentBookingLimit,
                    RequiresMemberSupervision = f.RequiresMemberSupervision,
                    MemberHourlyRate = f.MemberHourlyRate,
                    NonMemberHourlyRate = f.NonMemberHourlyRate,
                    Location = f.Location,
                    Properties = f.Properties,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            // Filter by membership tier access (client-side evaluation)
            var facilities = allFacilities
                .Where(f => !f.AllowedMembershipTiers.Any() || f.AllowedMembershipTiers.Contains(member.Tier))
                .ToList();

            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(facilities));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error retrieving facilities for member: {ex.Message}"));
        }
    }

    [HttpPost("{id}/check-member-access")]
    public async Task<ActionResult<ApiResponse<MemberFacilityAccessDto>>> CheckMemberAccess(
        Guid id, [FromBody] CheckMemberAccessRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberFacilityAccessDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var facility = await tenantContext.Facilities.FindAsync(id);
            if (facility == null)
                return NotFound(ApiResponse<MemberFacilityAccessDto>.ErrorResult("Facility not found"));

            var member = await tenantContext.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == request.MemberId);

            if (member == null)
                return BadRequest(ApiResponse<MemberFacilityAccessDto>.ErrorResult("Member not found"));

            var canAccess = await _authService.CanMemberAccessFacilityAsync(member.UserId, id, tenantContext);
            var missingCertifications = new List<string>();
            MembershipTier? requiredTier = null;
            var restrictions = new List<string>();

            if (!canAccess)
            {
                // Check membership tier requirement
                if (facility.AllowedMembershipTiers.Any() && !facility.AllowedMembershipTiers.Contains(member.Tier))
                {
                    requiredTier = facility.AllowedMembershipTiers.Min();
                    restrictions.Add($"Requires {requiredTier} membership or higher");
                }

                // Check missing certifications
                var memberCertifications = await GetActiveMemberCertificationsAsync(member.UserId, tenantContext);
                missingCertifications = facility.RequiredCertifications
                    .Where(req => !memberCertifications.Any(cert => cert.CertificationType == req))
                    .ToList();

                if (missingCertifications.Any())
                {
                    restrictions.Add($"Missing certifications: {string.Join(", ", missingCertifications)}");
                }
            }

            var result = new MemberFacilityAccessDto
            {
                CanAccess = canAccess,
                MissingCertifications = missingCertifications,
                RequiredTier = requiredTier,
                ReasonsDenied = restrictions.ToArray(),
                RequiredCertifications = facility.RequiredCertifications?.ToArray() ?? Array.Empty<string>(),
                MembershipTierAllowed = !facility.AllowedMembershipTiers.Any() || facility.AllowedMembershipTiers.Contains(member.Tier),
                CertificationsMet = !missingCertifications.Any()
            };

            return Ok(ApiResponse<MemberFacilityAccessDto>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<MemberFacilityAccessDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberFacilityAccessDto>.ErrorResult($"Error checking member access: {ex.Message}"));
        }
    }

    private async Task<List<MemberFacilityCertification>> GetActiveMemberCertificationsAsync(Guid userId, ClubManagementDbContext tenantContext)
    {
        var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Member == null) return new List<MemberFacilityCertification>();

        return await tenantContext.MemberFacilityCertifications
            .Where(c => c.MemberId == user.Member.Id && c.IsActive && 
                       (!c.ExpiryDate.HasValue || c.ExpiryDate > DateTime.UtcNow))
            .ToListAsync();
    }
}