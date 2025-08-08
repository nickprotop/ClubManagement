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
[Route("api/member-facility-access")]
[Authorize]
public class MemberFacilityAccessController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IMemberFacilityService _memberFacilityService;
    private readonly IFacilityAuthorizationService _authService;

    public MemberFacilityAccessController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IMemberFacilityService memberFacilityService,
        IFacilityAuthorizationService authService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _memberFacilityService = memberFacilityService;
        _authService = authService;
    }

    [HttpGet("{memberId}/accessible")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetAccessibleFacilities(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own accessible facilities, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var facilities = await _memberFacilityService.GetAccessibleFacilitiesAsync(memberId);

            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(facilities));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error retrieving accessible facilities: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/restricted")]
    public async Task<ActionResult<ApiResponse<List<FacilityDto>>>> GetRestrictedFacilities(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own restrictions, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var facilities = await _memberFacilityService.GetRestrictedFacilitiesAsync(memberId);

            return Ok(ApiResponse<List<FacilityDto>>.SuccessResult(facilities));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityDto>>.ErrorResult($"Error retrieving restricted facilities: {ex.Message}"));
        }
    }

    [HttpPost("validate-booking-limits")]
    public async Task<ActionResult<ApiResponse<BookingLimitValidationResult>>> ValidateBookingLimits([FromBody] ValidateBookingLimitsRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<BookingLimitValidationResult>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can validate their own limits, staff can validate any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != request.MemberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _memberFacilityService.ValidateBookingLimitsAsync(
                request.MemberId, 
                request.FacilityId, 
                request.StartDateTime, 
                request.EndDateTime,
                request.ExcludeBookingId);

            return Ok(ApiResponse<BookingLimitValidationResult>.SuccessResult(result));
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

    [HttpGet("{memberId}/booking-limits")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<List<MemberBookingLimitDto>>>> GetMemberBookingLimits(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<MemberBookingLimitDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var limits = await _memberFacilityService.GetMemberBookingLimitsAsync(memberId);

            return Ok(ApiResponse<List<MemberBookingLimitDto>>.SuccessResult(limits));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<MemberBookingLimitDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MemberBookingLimitDto>>.ErrorResult($"Error retrieving booking limits: {ex.Message}"));
        }
    }

    [HttpGet("{memberId}/booking-usage")]
    public async Task<ActionResult<ApiResponse<MemberBookingUsageDto>>> GetMemberBookingUsage(Guid memberId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberBookingUsageDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own usage, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var usage = await _memberFacilityService.GetMemberBookingUsageAsync(memberId, date);

            return Ok(ApiResponse<MemberBookingUsageDto>.SuccessResult(usage));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<MemberBookingUsageDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberBookingUsageDto>.ErrorResult($"Error retrieving booking usage: {ex.Message}"));
        }
    }
}