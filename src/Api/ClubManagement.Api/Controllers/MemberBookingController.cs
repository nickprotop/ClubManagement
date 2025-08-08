using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services.Interfaces;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Infrastructure.Authorization;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Api.Extensions;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/member-booking")]
[Authorize]
public class MemberBookingController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IMemberBookingService _memberBookingService;
    private readonly IFacilityAuthorizationService _authService;

    public MemberBookingController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IMemberBookingService memberBookingService,
        IFacilityAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _memberBookingService = memberBookingService;
        _authService = authService;
    }

    [HttpGet("{memberId}/bookings")]
    public async Task<ActionResult<ApiResponse<PagedResult<FacilityBookingDto>>>> GetMemberBookings(
        Guid memberId, [FromQuery] MemberBookingFilter filter)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<FacilityBookingDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own bookings, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetMemberBookingsAsync(memberId, filter);
            return Ok(ApiResponse<PagedResult<FacilityBookingDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<FacilityBookingDto>>.ErrorResult($"Error retrieving bookings: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/bookings/upcoming")]
    public async Task<ActionResult<ApiResponse<List<FacilityBookingDto>>>> GetMemberUpcomingBookings(
        Guid memberId, [FromQuery] int daysAhead = 7)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityBookingDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own upcoming bookings, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetMemberUpcomingBookingsAsync(memberId, daysAhead);
            return Ok(ApiResponse<List<FacilityBookingDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityBookingDto>>.ErrorResult($"Error retrieving upcoming bookings: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/bookings/history")]
    public async Task<ActionResult<ApiResponse<MemberBookingHistoryDto>>> GetMemberBookingHistory(
        Guid memberId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberBookingHistoryDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own history, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetMemberBookingHistoryAsync(memberId, startDate, endDate);
            return Ok(ApiResponse<MemberBookingHistoryDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberBookingHistoryDto>.ErrorResult($"Error retrieving booking history: {ex.Message}"));
        }
    }

    [HttpPost("{memberId}/bookings")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> CreateMemberBooking(
        Guid memberId, [FromBody] CreateMemberBookingRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can create their own bookings, staff can create for any member
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Create, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.CreateMemberBookingAsync(memberId, request);
            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error creating booking: {ex.Message}"));
        }
    }

    [HttpDelete("{memberId}/bookings/{bookingId}")]
    public async Task<ActionResult<ApiResponse<BookingCancellationResult>>> CancelMemberBooking(
        Guid memberId, Guid bookingId, [FromBody] CancelBookingRequest? request = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<BookingCancellationResult>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can cancel their own bookings, staff can cancel any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Delete, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.CancelMemberBookingAsync(memberId, bookingId, request?.Reason);
            return Ok(ApiResponse<BookingCancellationResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<BookingCancellationResult>.ErrorResult($"Error cancelling booking: {ex.Message}"));
        }
    }

    [HttpPut("{memberId}/bookings/{bookingId}")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> ModifyMemberBooking(
        Guid memberId, Guid bookingId, [FromBody] ModifyBookingRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can modify their own bookings, staff can modify any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Edit, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.ModifyMemberBookingAsync(memberId, bookingId, request);
            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error modifying booking: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/recommendations/{facilityId}")]
    public async Task<ActionResult<ApiResponse<List<RecommendedBookingSlot>>>> GetRecommendedBookingSlots(
        Guid memberId, Guid facilityId, [FromQuery] DateTime? preferredDate = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<RecommendedBookingSlot>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can get their own recommendations, staff can get for any member
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetRecommendedBookingSlotsAsync(memberId, facilityId, preferredDate);
            return Ok(ApiResponse<List<RecommendedBookingSlot>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<RecommendedBookingSlot>>.ErrorResult($"Error getting recommendations: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/preferences")]
    public async Task<ActionResult<ApiResponse<MemberFacilityPreferencesDto>>> GetMemberPreferences(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberFacilityPreferencesDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own preferences, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetMemberPreferencesAsync(memberId);
            return Ok(ApiResponse<MemberFacilityPreferencesDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberFacilityPreferencesDto>.ErrorResult($"Error retrieving preferences: {ex.Message}"));
        }
    }

    [HttpPut("{memberId}/preferences")]
    public async Task<ActionResult<ApiResponse<MemberFacilityPreferencesDto>>> UpdateMemberPreferences(
        Guid memberId, [FromBody] UpdateMemberPreferencesRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberFacilityPreferencesDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can update their own preferences, staff can update for any member
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Edit, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.UpdateMemberPreferencesAsync(memberId, request);
            return Ok(ApiResponse<MemberFacilityPreferencesDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberFacilityPreferencesDto>.ErrorResult($"Error updating preferences: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/access-status")]
    public async Task<ActionResult<ApiResponse<MemberAccessStatusDto>>> GetMemberAccessStatus(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberAccessStatusDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own status, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetMemberAccessStatusAsync(memberId);
            return Ok(ApiResponse<MemberAccessStatusDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberAccessStatusDto>.ErrorResult($"Error retrieving access status: {ex.Message}"));
        }
    }

    [HttpPost("{memberId}/check-availability")]
    public async Task<ActionResult<ApiResponse<BookingAvailabilityResult>>> CheckBookingAvailability(
        Guid memberId, [FromBody] CheckAvailabilityRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<BookingAvailabilityResult>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can check availability for themselves, staff can check for any member
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.CheckBookingAvailabilityAsync(
                memberId, request.FacilityId, request.StartTime, request.EndTime);
            
            return Ok(ApiResponse<BookingAvailabilityResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<BookingAvailabilityResult>.ErrorResult($"Error checking availability: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/favorites")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetMemberFavoriteFacilities(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own favorites, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberBookingService.GetMemberFavoriteFacilitiesAsync(memberId);
            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error retrieving favorites: {ex.Message}"));
        }
    }
}

// Request DTOs specific to this controller
public class CancelBookingRequest
{
    public string? Reason { get; set; }
}

public class CheckAvailabilityRequest
{
    public Guid FacilityId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}