using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Infrastructure.Services.Interfaces;
using ClubManagement.Infrastructure.Authorization;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Api.Extensions;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/facility-bookings")]
[Authorize]
public class FacilityBookingsController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IFacilityAuthorizationService _authService;
    private readonly IMemberFacilityService _memberFacilityService;

    public FacilityBookingsController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IFacilityAuthorizationService authService,
        IMemberFacilityService memberFacilityService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
        _memberFacilityService = memberFacilityService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FacilityBookingDto>>>> GetBookings([FromQuery] BookingListFilter filter)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<FacilityBookingDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewBookingsAsync(userId, tenantContext))
                return Forbid();

            var query = tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Include(b => b.BookedByUser)
                .Include(b => b.CheckedInByUser)
                .Include(b => b.CheckedOutByUser)
                .Include(b => b.CancelledByUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(b => b.Facility.Name.Contains(filter.Search) || 
                                       b.Member.User.FirstName.Contains(filter.Search) ||
                                       b.Member.User.LastName.Contains(filter.Search) ||
                                       b.Member.User.Email.Contains(filter.Search) ||
                                       (b.Purpose != null && b.Purpose.Contains(filter.Search)));
            }

            if (filter.FacilityId.HasValue)
                query = query.Where(b => b.FacilityId == filter.FacilityId.Value);

            if (filter.MemberId.HasValue)
                query = query.Where(b => b.MemberId == filter.MemberId.Value);

            if (filter.Status.HasValue)
                query = query.Where(b => b.Status == filter.Status.Value);

            if (filter.Source.HasValue)
                query = query.Where(b => b.BookingSource == filter.Source.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(b => b.StartDateTime >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(b => b.EndDateTime <= filter.EndDate.Value);

            if (filter.NoShow.HasValue)
                query = query.Where(b => b.NoShow == filter.NoShow.Value);

            if (filter.RequiresSupervision.HasValue)
                query = query.Where(b => b.RequiresStaffSupervision == filter.RequiresSupervision.Value);

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "facility" => filter.SortDescending ? query.OrderByDescending(b => b.Facility.Name) : query.OrderBy(b => b.Facility.Name),
                "member" => filter.SortDescending ? query.OrderByDescending(b => b.Member.User.FirstName) : query.OrderBy(b => b.Member.User.FirstName),
                "status" => filter.SortDescending ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
                "startdatetime" => filter.SortDescending ? query.OrderByDescending(b => b.StartDateTime) : query.OrderBy(b => b.StartDateTime),
                "enddatetime" => filter.SortDescending ? query.OrderByDescending(b => b.EndDateTime) : query.OrderBy(b => b.EndDateTime),
                _ => filter.SortDescending ? query.OrderByDescending(b => b.StartDateTime) : query.OrderBy(b => b.StartDateTime)
            };

            var totalCount = await query.CountAsync();
            var bookings = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new FacilityBookingDto
                {
                    Id = b.Id,
                    FacilityId = b.FacilityId,
                    FacilityName = b.Facility.Name,
                    FacilityLocation = b.Facility.Location,
                    MemberId = b.MemberId,
                    MemberName = (b.Member.User.FirstName ?? "") + " " + (b.Member.User.LastName ?? ""),
                    MemberEmail = b.Member.User.Email,
                    BookedByUserId = b.BookedByUserId,
                    BookedByUserName = b.BookedByUser != null ? (b.BookedByUser.FirstName ?? "") + " " + (b.BookedByUser.LastName ?? "") : null,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    Status = b.Status,
                    BookingSource = b.BookingSource,
                    Cost = b.Cost,
                    PaymentDate = b.PaymentDate,
                    PaymentMethod = b.PaymentMethod,
                    Purpose = b.Purpose,
                    ParticipantCount = b.ParticipantCount,
                    CheckedInAt = b.CheckedInAt,
                    CheckedInByUserName = b.CheckedInByUser != null ? (b.CheckedInByUser.FirstName ?? "") + " " + (b.CheckedInByUser.LastName ?? "") : null,
                    CheckedOutAt = b.CheckedOutAt,
                    CheckedOutByUserName = b.CheckedOutByUser != null ? (b.CheckedOutByUser.FirstName ?? "") + " " + (b.CheckedOutByUser.LastName ?? "") : null,
                    NoShow = b.NoShow,
                    Notes = b.Notes,
                    MemberNotes = b.MemberNotes,
                    CancellationReason = b.CancellationReason,
                    CancelledAt = b.CancelledAt,
                    CancelledByUserName = b.CancelledByUser != null ? (b.CancelledByUser.FirstName ?? "") + " " + (b.CancelledByUser.LastName ?? "") : null,
                    IsRecurring = b.IsRecurring,
                    RecurrenceGroupId = b.RecurrenceGroupId,
                    RequiresStaffSupervision = b.RequiresStaffSupervision,
                    AdditionalParticipants = b.AdditionalParticipants,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResult<FacilityBookingDto>
            {
                Items = bookings,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Ok(ApiResponse<PagedResult<FacilityBookingDto>>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PagedResult<FacilityBookingDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<FacilityBookingDto>>.ErrorResult($"Error retrieving bookings: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> GetBooking(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var booking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Include(b => b.BookedByUser)
                .Include(b => b.CheckedInByUser)
                .Include(b => b.CheckedOutByUser)
                .Include(b => b.CancelledByUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(ApiResponse<FacilityBookingDto>.ErrorResult("Booking not found"));

            // Check if user can view this booking
            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            var canViewAll = await _authService.CanViewBookingsAsync(userId, tenantContext, false);
            var canViewOwn = await _authService.CanViewBookingsAsync(userId, tenantContext, true);

            if (!canViewAll && (!canViewOwn || user?.Member?.Id != booking.MemberId))
                return Forbid();

            var dto = new FacilityBookingDto
            {
                Id = booking.Id,
                FacilityId = booking.FacilityId,
                FacilityName = booking.Facility.Name,
                FacilityLocation = booking.Facility.Location,
                MemberId = booking.MemberId,
                MemberName = (booking.Member.User.FirstName ?? "") + " " + (booking.Member.User.LastName ?? ""),
                MemberEmail = booking.Member.User.Email,
                BookedByUserId = booking.BookedByUserId,
                BookedByUserName = booking.BookedByUser != null ? (booking.BookedByUser.FirstName ?? "") + " " + (booking.BookedByUser.LastName ?? "") : null,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                Status = booking.Status,
                BookingSource = booking.BookingSource,
                Cost = booking.Cost,
                PaymentDate = booking.PaymentDate,
                PaymentMethod = booking.PaymentMethod,
                Purpose = booking.Purpose,
                ParticipantCount = booking.ParticipantCount,
                CheckedInAt = booking.CheckedInAt,
                CheckedInByUserName = booking.CheckedInByUser != null ? (booking.CheckedInByUser.FirstName ?? "") + " " + (booking.CheckedInByUser.LastName ?? "") : null,
                CheckedOutAt = booking.CheckedOutAt,
                CheckedOutByUserName = booking.CheckedOutByUser != null ? (booking.CheckedOutByUser.FirstName ?? "") + " " + (booking.CheckedOutByUser.LastName ?? "") : null,
                NoShow = booking.NoShow,
                Notes = booking.Notes,
                MemberNotes = booking.MemberNotes,
                CancellationReason = booking.CancellationReason,
                CancelledAt = booking.CancelledAt,
                CancelledByUserName = booking.CancelledByUser != null ? (booking.CancelledByUser.FirstName ?? "") + " " + (booking.CancelledByUser.LastName ?? "") : null,
                IsRecurring = booking.IsRecurring,
                RecurrenceGroupId = booking.RecurrenceGroupId,
                RequiresStaffSupervision = booking.RequiresStaffSupervision,
                AdditionalParticipants = booking.AdditionalParticipants,
                CreatedAt = booking.CreatedAt
            };

            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityBookingDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error retrieving booking: {ex.Message}"));
        }
    }

    [HttpGet("my-bookings")]
    public async Task<ActionResult<ApiResponse<List<FacilityBookingDto>>>> GetMyBookings()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityBookingDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanViewBookingsAsync(userId, tenantContext, true))
                return Forbid();

            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Member == null)
                return BadRequest(ApiResponse<List<FacilityBookingDto>>.ErrorResult("User is not a member"));

            var bookings = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Where(b => b.MemberId == user.Member.Id)
                .OrderByDescending(b => b.StartDateTime)
                .Select(b => new FacilityBookingDto
                {
                    Id = b.Id,
                    FacilityId = b.FacilityId,
                    FacilityName = b.Facility.Name,
                    FacilityLocation = b.Facility.Location,
                    MemberId = b.MemberId,
                    MemberName = (b.Member.User.FirstName ?? "") + " " + (b.Member.User.LastName ?? ""),
                    MemberEmail = b.Member.User.Email,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    Status = b.Status,
                    BookingSource = b.BookingSource,
                    Cost = b.Cost,
                    PaymentDate = b.PaymentDate,
                    PaymentMethod = b.PaymentMethod,
                    Purpose = b.Purpose,
                    ParticipantCount = b.ParticipantCount,
                    CheckedInAt = b.CheckedInAt,
                    CheckedOutAt = b.CheckedOutAt,
                    NoShow = b.NoShow,
                    Notes = b.Notes,
                    MemberNotes = b.MemberNotes,
                    CancellationReason = b.CancellationReason,
                    CancelledAt = b.CancelledAt,
                    IsRecurring = b.IsRecurring,
                    RecurrenceGroupId = b.RecurrenceGroupId,
                    RequiresStaffSupervision = b.RequiresStaffSupervision,
                    AdditionalParticipants = b.AdditionalParticipants,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<FacilityBookingDto>>.SuccessResult(bookings));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityBookingDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityBookingDto>>.ErrorResult($"Error retrieving your bookings: {ex.Message}"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Book, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Validate facility exists and is available
            var facility = await tenantContext.Facilities.FindAsync(request.FacilityId);
            if (facility == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Facility not found"));

            if (facility.Status != FacilityStatus.Available)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Facility is not available for booking"));

            // Validate member exists
            var member = await tenantContext.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == request.MemberId);
            if (member == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Member not found"));

            // Check for conflicts
            var availabilityResult = await CheckBookingAvailabilityAsync(tenantContext, request.FacilityId, request.StartDateTime, request.EndDateTime);
            if (!availabilityResult.IsAvailable)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking conflicts: {string.Join(", ", availabilityResult.ConflictReasons)}"));

            // Validate member access and booking limits
            using var memberFacilityContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            var accessResult = await _memberFacilityService.CheckMemberAccessAsync(request.FacilityId, request.MemberId);
            if (!accessResult.CanAccess)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Member access denied: {string.Join(", ", accessResult.ReasonsDenied)}"));

            var limitValidation = await _memberFacilityService.ValidateBookingLimitsAsync(
                request.MemberId, request.FacilityId, request.StartDateTime, request.EndDateTime);
            if (!limitValidation.IsValid)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking limit violations: {string.Join(", ", limitValidation.Violations)}"));

            // Calculate cost
            var durationHours = (decimal)(request.EndDateTime - request.StartDateTime).TotalHours;
            var cost = durationHours * (facility.MemberHourlyRate > 0 ? facility.MemberHourlyRate : facility.HourlyRate ?? 0);

            var booking = new FacilityBooking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = request.FacilityId,
                MemberId = request.MemberId,
                BookedByUserId = userId,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Status = BookingStatus.Confirmed,
                BookingSource = BookingSource.StaffBooking,
                Cost = cost > 0 ? cost : null,
                Purpose = request.Purpose,
                ParticipantCount = request.ParticipantCount,
                Notes = request.Notes,
                MemberNotes = request.MemberNotes,
                IsRecurring = request.IsRecurring,
                RequiresStaffSupervision = facility.RequiresMemberSupervision,
                AdditionalParticipants = request.AdditionalParticipants,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.FacilityBookings.Add(booking);
            await tenantContext.SaveChangesAsync();

            // Return the created booking with full details
            var createdBooking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Include(b => b.BookedByUser)
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            var dto = new FacilityBookingDto
            {
                Id = createdBooking!.Id,
                FacilityId = createdBooking.FacilityId,
                FacilityName = createdBooking.Facility.Name,
                FacilityLocation = createdBooking.Facility.Location,
                MemberId = createdBooking.MemberId,
                MemberName = (createdBooking.Member.User.FirstName ?? "") + " " + (createdBooking.Member.User.LastName ?? ""),
                MemberEmail = createdBooking.Member.User.Email,
                BookedByUserId = createdBooking.BookedByUserId,
                BookedByUserName = createdBooking.BookedByUser != null ? (createdBooking.BookedByUser.FirstName ?? "") + " " + (createdBooking.BookedByUser.LastName ?? "") : null,
                StartDateTime = createdBooking.StartDateTime,
                EndDateTime = createdBooking.EndDateTime,
                Status = createdBooking.Status,
                BookingSource = createdBooking.BookingSource,
                Cost = createdBooking.Cost,
                Purpose = createdBooking.Purpose,
                ParticipantCount = createdBooking.ParticipantCount,
                Notes = createdBooking.Notes,
                MemberNotes = createdBooking.MemberNotes,
                IsRecurring = createdBooking.IsRecurring,
                RequiresStaffSupervision = createdBooking.RequiresStaffSupervision,
                AdditionalParticipants = createdBooking.AdditionalParticipants,
                CreatedAt = createdBooking.CreatedAt
            };

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, ApiResponse<FacilityBookingDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityBookingDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error creating booking: {ex.Message}"));
        }
    }

    [HttpPost("book-for-self")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> BookForSelf([FromBody] MemberBookingRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.BookForSelf, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Member == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("User is not a member"));

            // Check member access to facility
            if (!await _authService.CanMemberAccessFacilityAsync(userId, request.FacilityId, tenantContext))
                return Forbid("You don't have access to this facility. Check membership tier or certification requirements.");

            // Validate facility
            var facility = await tenantContext.Facilities.FindAsync(request.FacilityId);
            if (facility == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Facility not found"));

            if (facility.Status != FacilityStatus.Available)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Facility is not available for booking"));

            // Check booking limits
            var memberPermissions = await _authService.GetMemberFacilityPermissionsAsync(userId, tenantContext, request.FacilityId);
            var currentBookings = await tenantContext.FacilityBookings
                .CountAsync(b => b.MemberId == user.Member.Id && 
                           (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));

            if (currentBookings >= memberPermissions.MaxConcurrentBookings)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Maximum concurrent bookings limit reached ({memberPermissions.MaxConcurrentBookings})"));

            var durationMinutes = (int)(request.EndDateTime - request.StartDateTime).TotalMinutes;
            if (durationMinutes > memberPermissions.MaxBookingDurationMinutes)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking duration exceeds limit ({memberPermissions.MaxBookingDurationMinutes} minutes)"));

            // Check advance booking limit
            var daysInAdvance = (request.StartDateTime.Date - DateTime.UtcNow.Date).Days;
            if (daysInAdvance > memberPermissions.MaxAdvanceBookingDays)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Cannot book more than {memberPermissions.MaxAdvanceBookingDays} days in advance"));

            // Check time window restrictions
            if (memberPermissions.BookingTimeWindowStart.HasValue && request.StartDateTime.TimeOfDay < memberPermissions.BookingTimeWindowStart.Value)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking time is outside allowed window"));

            if (memberPermissions.BookingTimeWindowEnd.HasValue && request.EndDateTime.TimeOfDay > memberPermissions.BookingTimeWindowEnd.Value)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking time is outside allowed window"));

            // Check for conflicts
            var availabilityResult = await CheckBookingAvailabilityAsync(tenantContext, request.FacilityId, request.StartDateTime, request.EndDateTime);
            if (!availabilityResult.IsAvailable)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking conflicts: {string.Join(", ", availabilityResult.ConflictReasons)}"));

            // Calculate cost
            var durationHours = (decimal)(request.EndDateTime - request.StartDateTime).TotalHours;
            var cost = durationHours * facility.MemberHourlyRate;

            var booking = new FacilityBooking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = request.FacilityId,
                MemberId = user.Member.Id,
                BookedByUserId = userId,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Status = BookingStatus.Confirmed,
                BookingSource = BookingSource.MemberPortal,
                Cost = cost > 0 ? cost : null,
                Purpose = request.Purpose,
                ParticipantCount = request.ParticipantCount,
                MemberNotes = request.MemberNotes,
                IsRecurring = request.IsRecurring,
                RequiresStaffSupervision = facility.RequiresMemberSupervision,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.FacilityBookings.Add(booking);
            await tenantContext.SaveChangesAsync();

            // Return the created booking with full details
            var createdBooking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            var dto = new FacilityBookingDto
            {
                Id = createdBooking!.Id,
                FacilityId = createdBooking.FacilityId,
                FacilityName = createdBooking.Facility.Name,
                FacilityLocation = createdBooking.Facility.Location,
                MemberId = createdBooking.MemberId,
                MemberName = (createdBooking.Member.User.FirstName ?? "") + " " + (createdBooking.Member.User.LastName ?? ""),
                MemberEmail = createdBooking.Member.User.Email,
                StartDateTime = createdBooking.StartDateTime,
                EndDateTime = createdBooking.EndDateTime,
                Status = createdBooking.Status,
                BookingSource = createdBooking.BookingSource,
                Cost = createdBooking.Cost,
                Purpose = createdBooking.Purpose,
                ParticipantCount = createdBooking.ParticipantCount,
                MemberNotes = createdBooking.MemberNotes,
                IsRecurring = createdBooking.IsRecurring,
                RequiresStaffSupervision = createdBooking.RequiresStaffSupervision,
                CreatedAt = createdBooking.CreatedAt
            };

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, ApiResponse<FacilityBookingDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityBookingDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error creating booking: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> UpdateBooking(Guid id, [FromBody] UpdateBookingRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var booking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(ApiResponse<FacilityBookingDto>.ErrorResult("Booking not found"));

            // Check authorization
            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            var canModifyAll = await _authService.CanPerformActionAsync(userId, FacilityAction.ModifyBooking, tenantContext);
            var canModifyOwn = await _authService.CanPerformActionAsync(userId, FacilityAction.ModifyOwnBooking, tenantContext);

            if (!canModifyAll && (!canModifyOwn || user?.Member?.Id != booking.MemberId))
                return Forbid();

            // Check if booking can be modified (not too close to start time)
            if (booking.StartDateTime <= DateTime.UtcNow.AddHours(2))
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Cannot modify booking less than 2 hours before start time"));

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.Completed)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Cannot modify booking with status: {booking.Status}"));

            // Update properties that are provided
            if (request.StartDateTime.HasValue && request.EndDateTime.HasValue)
            {
                // Check for conflicts with new times
                var availabilityResult = await CheckBookingAvailabilityAsync(tenantContext, booking.FacilityId, 
                    request.StartDateTime.Value, request.EndDateTime.Value, booking.Id);
                
                if (!availabilityResult.IsAvailable)
                    return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Booking conflicts: {string.Join(", ", availabilityResult.ConflictReasons)}"));

                booking.StartDateTime = request.StartDateTime.Value;
                booking.EndDateTime = request.EndDateTime.Value;

                // Recalculate cost
                var durationHours = (decimal)(booking.EndDateTime - booking.StartDateTime).TotalHours;
                booking.Cost = durationHours * booking.Facility.MemberHourlyRate;
            }

            if (!string.IsNullOrEmpty(request.Purpose))
                booking.Purpose = request.Purpose;

            if (request.ParticipantCount.HasValue)
                booking.ParticipantCount = request.ParticipantCount;

            if (!string.IsNullOrEmpty(request.Notes))
                booking.Notes = request.Notes;

            if (!string.IsNullOrEmpty(request.MemberNotes))
                booking.MemberNotes = request.MemberNotes;

            booking.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            var dto = new FacilityBookingDto
            {
                Id = booking.Id,
                FacilityId = booking.FacilityId,
                FacilityName = booking.Facility.Name,
                FacilityLocation = booking.Facility.Location,
                MemberId = booking.MemberId,
                MemberName = (booking.Member.User.FirstName ?? "") + " " + (booking.Member.User.LastName ?? ""),
                MemberEmail = booking.Member.User.Email,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                Status = booking.Status,
                BookingSource = booking.BookingSource,
                Cost = booking.Cost,
                PaymentDate = booking.PaymentDate,
                PaymentMethod = booking.PaymentMethod,
                Purpose = booking.Purpose,
                ParticipantCount = booking.ParticipantCount,
                Notes = booking.Notes,
                MemberNotes = booking.MemberNotes,
                IsRecurring = booking.IsRecurring,
                RequiresStaffSupervision = booking.RequiresStaffSupervision,
                AdditionalParticipants = booking.AdditionalParticipants,
                CreatedAt = booking.CreatedAt
            };

            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityBookingDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error updating booking: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> CancelBooking(Guid id, [FromQuery] string? reason = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var booking = await tenantContext.FacilityBookings
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(ApiResponse<object>.ErrorResult("Booking not found"));

            // Check authorization
            var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            var canCancelAll = await _authService.CanPerformActionAsync(userId, FacilityAction.CancelBooking, tenantContext);
            var canCancelOwn = await _authService.CanPerformActionAsync(userId, FacilityAction.CancelOwnBooking, tenantContext);

            if (!canCancelAll && (!canCancelOwn || user?.Member?.Id != booking.MemberId))
                return Forbid();

            if (booking.Status == BookingStatus.Cancelled)
                return BadRequest(ApiResponse<object>.ErrorResult("Booking is already cancelled"));

            if (booking.Status == BookingStatus.Completed)
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot cancel completed booking"));

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason ?? "Cancelled by user";
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelledByUserId = userId;
            booking.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Booking cancelled successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error cancelling booking: {ex.Message}"));
        }
    }

    [HttpPost("{id}/checkin")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> CheckIn(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanCheckInMembersAsync(userId, tenantContext))
                return Forbid();

            var booking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(ApiResponse<FacilityBookingDto>.ErrorResult("Booking not found"));

            if (booking.Status != BookingStatus.Confirmed)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Cannot check in booking with status: {booking.Status}"));

            if (DateTime.UtcNow < booking.StartDateTime.AddMinutes(-15))
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Cannot check in more than 15 minutes before booking start time"));

            booking.Status = BookingStatus.CheckedIn;
            booking.CheckedInAt = DateTime.UtcNow;
            booking.CheckedInByUserId = userId;
            booking.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            // Return updated booking
            var updatedBooking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Include(b => b.CheckedInByUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            var dto = new FacilityBookingDto
            {
                Id = updatedBooking!.Id,
                FacilityId = updatedBooking.FacilityId,
                FacilityName = updatedBooking.Facility.Name,
                FacilityLocation = updatedBooking.Facility.Location,
                MemberId = updatedBooking.MemberId,
                MemberName = (updatedBooking.Member.User.FirstName ?? "") + " " + (updatedBooking.Member.User.LastName ?? ""),
                MemberEmail = updatedBooking.Member.User.Email,
                StartDateTime = updatedBooking.StartDateTime,
                EndDateTime = updatedBooking.EndDateTime,
                Status = updatedBooking.Status,
                CheckedInAt = updatedBooking.CheckedInAt,
                CheckedInByUserName = updatedBooking.CheckedInByUser != null ? (updatedBooking.CheckedInByUser.FirstName ?? "") + " " + (updatedBooking.CheckedInByUser.LastName ?? "") : null,
                Purpose = updatedBooking.Purpose,
                ParticipantCount = updatedBooking.ParticipantCount,
                Notes = updatedBooking.Notes,
                CreatedAt = updatedBooking.CreatedAt
            };

            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityBookingDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error checking in: {ex.Message}"));
        }
    }

    [HttpPost("{id}/checkout")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> CheckOut(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanCheckInMembersAsync(userId, tenantContext))
                return Forbid();

            var booking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(ApiResponse<FacilityBookingDto>.ErrorResult("Booking not found"));

            if (booking.Status != BookingStatus.CheckedIn)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult($"Cannot check out booking with status: {booking.Status}"));

            booking.Status = BookingStatus.CheckedOut;
            booking.CheckedOutAt = DateTime.UtcNow;
            booking.CheckedOutByUserId = userId;
            booking.UpdatedAt = DateTime.UtcNow;

            // Auto-complete if checkout is after end time
            if (DateTime.UtcNow >= booking.EndDateTime)
            {
                booking.Status = BookingStatus.Completed;
            }

            await tenantContext.SaveChangesAsync();

            // Return updated booking
            var updatedBooking = await tenantContext.FacilityBookings
                .Include(b => b.Facility)
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Include(b => b.CheckedInByUser)
                .Include(b => b.CheckedOutByUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            var dto = new FacilityBookingDto
            {
                Id = updatedBooking!.Id,
                FacilityId = updatedBooking.FacilityId,
                FacilityName = updatedBooking.Facility.Name,
                FacilityLocation = updatedBooking.Facility.Location,
                MemberId = updatedBooking.MemberId,
                MemberName = (updatedBooking.Member.User.FirstName ?? "") + " " + (updatedBooking.Member.User.LastName ?? ""),
                MemberEmail = updatedBooking.Member.User.Email,
                StartDateTime = updatedBooking.StartDateTime,
                EndDateTime = updatedBooking.EndDateTime,
                Status = updatedBooking.Status,
                CheckedInAt = updatedBooking.CheckedInAt,
                CheckedInByUserName = updatedBooking.CheckedInByUser != null ? (updatedBooking.CheckedInByUser.FirstName ?? "") + " " + (updatedBooking.CheckedInByUser.LastName ?? "") : null,
                CheckedOutAt = updatedBooking.CheckedOutAt,
                CheckedOutByUserName = updatedBooking.CheckedOutByUser != null ? (updatedBooking.CheckedOutByUser.FirstName ?? "") + " " + (updatedBooking.CheckedOutByUser.LastName ?? "") : null,
                Purpose = updatedBooking.Purpose,
                ParticipantCount = updatedBooking.ParticipantCount,
                Notes = updatedBooking.Notes,
                CreatedAt = updatedBooking.CreatedAt
            };

            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityBookingDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error checking out: {ex.Message}"));
        }
    }

    [HttpPost("{id}/noshow")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> MarkNoShow(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var booking = await tenantContext.FacilityBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return NotFound(ApiResponse<object>.ErrorResult("Booking not found"));

            if (booking.Status != BookingStatus.Confirmed)
                return BadRequest(ApiResponse<object>.ErrorResult($"Cannot mark no-show for booking with status: {booking.Status}"));

            // Only allow marking no-show after booking start time
            if (DateTime.UtcNow < booking.StartDateTime.AddMinutes(15))
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot mark no-show until 15 minutes after booking start time"));

            booking.Status = BookingStatus.NoShow;
            booking.NoShow = true;
            booking.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Booking marked as no-show"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error marking no-show: {ex.Message}"));
        }
    }

    [HttpGet("check-availability-for-member")]
    public async Task<ActionResult<ApiResponse<AvailabilityResult>>> CheckAvailabilityForMember(
        [FromQuery] Guid facilityId, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<AvailabilityResult>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            if (!await _authService.CanAccessMemberPortalAsync(userId, tenantContext))
                return Forbid();

            var result = await CheckBookingAvailabilityAsync(tenantContext, facilityId, startTime, endTime);
            
            // Add member-specific checks
            if (result.IsAvailable)
            {
                if (!await _authService.CanMemberAccessFacilityAsync(userId, facilityId, tenantContext))
                {
                    result.IsAvailable = false;
                    result.ConflictReasons.Add("You don't have access to this facility");
                }
                else
                {
                    // Check member booking limits
                    var memberPermissions = await _authService.GetMemberFacilityPermissionsAsync(userId, tenantContext, facilityId);
                    var user = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
                    
                    if (user?.Member != null)
                    {
                        var currentBookings = await tenantContext.FacilityBookings
                            .CountAsync(b => b.MemberId == user.Member.Id && 
                                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));

                        if (currentBookings >= memberPermissions.MaxConcurrentBookings)
                        {
                            result.IsAvailable = false;
                            result.ConflictReasons.Add($"Maximum concurrent bookings limit reached ({memberPermissions.MaxConcurrentBookings})");
                        }

                        var durationMinutes = (int)(endTime - startTime).TotalMinutes;
                        if (durationMinutes > memberPermissions.MaxBookingDurationMinutes)
                        {
                            result.IsAvailable = false;
                            result.ConflictReasons.Add($"Booking duration exceeds limit ({memberPermissions.MaxBookingDurationMinutes} minutes)");
                        }
                    }
                }
            }

            return Ok(ApiResponse<AvailabilityResult>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AvailabilityResult>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AvailabilityResult>.ErrorResult($"Error checking availability: {ex.Message}"));
        }
    }

    private async Task<AvailabilityResult> CheckBookingAvailabilityAsync(
        ClubManagementDbContext tenantContext, 
        Guid facilityId, 
        DateTime startTime, 
        DateTime endTime, 
        Guid? excludeBookingId = null)
    {
        var result = new AvailabilityResult { IsAvailable = true };

        // Check for conflicting bookings
        var conflictingBookings = await tenantContext.FacilityBookings
            .Include(b => b.Member)
                .ThenInclude(m => m.User)
            .Where(b => b.FacilityId == facilityId && 
                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                       (excludeBookingId == null || b.Id != excludeBookingId) &&
                       ((startTime >= b.StartDateTime && startTime < b.EndDateTime) ||
                        (endTime > b.StartDateTime && endTime <= b.EndDateTime) ||
                        (startTime <= b.StartDateTime && endTime >= b.EndDateTime)))
            .Select(b => new FacilityBookingDto
            {
                Id = b.Id,
                MemberName = (b.Member.User.FirstName ?? "") + " " + (b.Member.User.LastName ?? ""),
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Status = b.Status
            })
            .ToListAsync();

        if (conflictingBookings.Any())
        {
            result.IsAvailable = false;
            result.ConflictingBookings = conflictingBookings;
            result.ConflictReasons.Add("Time slot conflicts with existing bookings");
        }

        // Check facility operating hours
        var facility = await tenantContext.Facilities.FindAsync(facilityId);
        if (facility != null)
        {
            var startDayOfWeek = startTime.DayOfWeek;
            var endDayOfWeek = endTime.DayOfWeek;

            if (facility.OperatingDays.Any() && !facility.OperatingDays.Contains(startDayOfWeek))
            {
                result.IsAvailable = false;
                result.ConflictReasons.Add($"Facility is not open on {startDayOfWeek}");
            }

            if (facility.OperatingHoursStart.HasValue && startTime.TimeOfDay < facility.OperatingHoursStart.Value)
            {
                result.IsAvailable = false;
                result.ConflictReasons.Add($"Booking starts before operating hours ({facility.OperatingHoursStart.Value:hh\\:mm})");
            }

            if (facility.OperatingHoursEnd.HasValue && endTime.TimeOfDay > facility.OperatingHoursEnd.Value)
            {
                result.IsAvailable = false;
                result.ConflictReasons.Add($"Booking ends after operating hours ({facility.OperatingHoursEnd.Value:hh\\:mm})");
            }

            // Check minimum and maximum booking duration
            var durationMinutes = (int)(endTime - startTime).TotalMinutes;
            if (durationMinutes < facility.MinBookingDurationMinutes)
            {
                result.IsAvailable = false;
                result.ConflictReasons.Add($"Booking duration below minimum ({facility.MinBookingDurationMinutes} minutes)");
            }

            if (durationMinutes > facility.MaxBookingDurationMinutes)
            {
                result.IsAvailable = false;
                result.ConflictReasons.Add($"Booking duration exceeds maximum ({facility.MaxBookingDurationMinutes} minutes)");
            }
        }

        // Find next available slot if current is not available
        if (!result.IsAvailable)
        {
            var duration = endTime - startTime;
            var searchStart = endTime;
            var maxSearchDays = 7;

            for (int day = 0; day < maxSearchDays; day++)
            {
                var dayStart = searchStart.Date.AddDays(day);
                var proposedStart = dayStart.Add(startTime.TimeOfDay);
                var proposedEnd = proposedStart.Add(duration);

                var nextSlotResult = await CheckBookingAvailabilityAsync(tenantContext, facilityId, proposedStart, proposedEnd, excludeBookingId);
                if (nextSlotResult.IsAvailable)
                {
                    result.NextAvailableSlot = proposedStart;
                    break;
                }
            }
        }

        return result;
    }

    [HttpPost("validate-booking-limits")]
    public async Task<ActionResult<ApiResponse<BookingLimitValidationResult>>> ValidateBookingLimits(
        [FromBody] ValidateBookingLimitsRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<BookingLimitValidationResult>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var validationResult = await _memberFacilityService.ValidateBookingLimitsAsync(
                request.MemberId, request.FacilityId, request.StartDateTime, request.EndDateTime, request.ExcludeBookingId);

            return Ok(ApiResponse<BookingLimitValidationResult>.SuccessResult(validationResult));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<BookingLimitValidationResult>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<BookingLimitValidationResult>.ErrorResult($"Error validating booking limits: {ex.Message}"));
        }
    }

    [HttpGet("member/{memberId}/usage")]
    public async Task<ActionResult<ApiResponse<MemberBookingUsageDto>>> GetMemberBookingUsage(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberBookingUsageDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var usage = await _memberFacilityService.GetMemberBookingUsageAsync(memberId);

            return Ok(ApiResponse<MemberBookingUsageDto>.SuccessResult(usage));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<MemberBookingUsageDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberBookingUsageDto>.ErrorResult($"Error getting member usage: {ex.Message}"));
        }
    }

    [HttpPost("{facilityId}/check-conflicts")]
    public async Task<ActionResult<ApiResponse<List<FacilityBookingConflictDto>>>> CheckBookingConflicts(
        Guid facilityId,
        [FromBody] CheckBookingConflictsRequest request)
    {
        try
        {
            // Convert to UTC for consistent database comparisons
            var utcStartDateTime = request.StartDateTime.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.StartDateTime, DateTimeKind.Utc) 
                : request.StartDateTime.ToUniversalTime();
            
            var utcEndDateTime = request.EndDateTime.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(request.EndDateTime, DateTimeKind.Utc) 
                : request.EndDateTime.ToUniversalTime();

            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityBookingConflictDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Check if user can view bookings for this facility
            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Find conflicting bookings
            var conflictingBookingsQuery = tenantContext.FacilityBookings
                .Include(b => b.Member)
                    .ThenInclude(m => m.User)
                .Where(b => b.FacilityId == facilityId && 
                           (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                           ((utcStartDateTime >= b.StartDateTime && utcStartDateTime < b.EndDateTime) ||
                            (utcEndDateTime > b.StartDateTime && utcEndDateTime <= b.EndDateTime) ||
                            (utcStartDateTime <= b.StartDateTime && utcEndDateTime >= b.EndDateTime)));

            // Exclude specific booking if provided (useful for editing existing bookings)
            if (request.ExcludeBookingId.HasValue)
                conflictingBookingsQuery = conflictingBookingsQuery.Where(b => b.Id != request.ExcludeBookingId.Value);

            var conflictingBookings = await conflictingBookingsQuery.ToListAsync();

            var conflicts = conflictingBookings.Select(b => new FacilityBookingConflictDto
            {
                BookingId = b.Id,
                MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Status = b.Status,
                ConflictType = GetConflictType(utcStartDateTime, utcEndDateTime, b.StartDateTime, b.EndDateTime),
                OverlapMinutes = CalculateOverlapMinutes(utcStartDateTime, utcEndDateTime, b.StartDateTime, b.EndDateTime)
            }).ToList();

            return Ok(ApiResponse<List<FacilityBookingConflictDto>>.SuccessResult(conflicts));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityBookingConflictDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityBookingConflictDto>>.ErrorResult($"Error checking booking conflicts: {ex.Message}"));
        }
    }

    private static string GetConflictType(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        if (start1 <= start2 && end1 >= end2)
            return "Complete Overlap";
        if (start2 <= start1 && end2 >= end1)
            return "Contained Within";
        if (start1 < end2 && start2 < end1)
            return "Partial Overlap";
        return "Adjacent";
    }

    private static int CalculateOverlapMinutes(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        var overlapStart = start1 > start2 ? start1 : start2;
        var overlapEnd = end1 < end2 ? end1 : end2;
        
        if (overlapStart >= overlapEnd)
            return 0;
            
        return (int)(overlapEnd - overlapStart).TotalMinutes;
    }
}