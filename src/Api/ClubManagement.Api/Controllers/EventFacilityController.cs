using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClubManagement.Infrastructure.Services.Interfaces;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Authorization;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Api.Extensions;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/event-facility")]
[Authorize]
public class EventFacilityController : ControllerBase
{
    private readonly IEventFacilityService _eventFacilityService;
    private readonly IEventAuthorizationService _eventAuthService;
    private readonly IFacilityAuthorizationService _facilityAuthService;
    private readonly ITenantService _tenantService;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;

    public EventFacilityController(
        IEventFacilityService eventFacilityService,
        IEventAuthorizationService eventAuthService,
        IFacilityAuthorizationService facilityAuthService,
        ITenantService tenantService,
        ITenantDbContextFactory tenantDbContextFactory)
    {
        _eventFacilityService = eventFacilityService;
        _eventAuthService = eventAuthService;
        _facilityAuthService = facilityAuthService;
        _tenantService = tenantService;
        _tenantDbContextFactory = tenantDbContextFactory;
    }

    [HttpGet("{eventId}/eligibility/{memberId}")]
    public async Task<ActionResult<ApiResponse<ClubManagement.Shared.DTOs.EventEligibilityResult>>> CheckMemberEligibility(Guid eventId, Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ClubManagement.Shared.DTOs.EventEligibilityResult>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check if user can view event details (members can check their own eligibility)
            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _eventFacilityService.CheckMemberEligibilityAsync(eventId, memberId);
            return Ok(ApiResponse<ClubManagement.Shared.DTOs.EventEligibilityResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ClubManagement.Shared.DTOs.EventEligibilityResult>.ErrorResult($"Error checking eligibility: {ex.Message}"));
        }
    }

    [HttpGet("{eventId}/facility-requirements")]
    public async Task<ActionResult<ApiResponse<ClubManagement.Shared.DTOs.EventFacilityRequirementsDto>>> GetEventFacilityRequirements(Guid eventId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ClubManagement.Shared.DTOs.EventFacilityRequirementsDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var result = await _eventFacilityService.GetEventFacilityRequirementsAsync(eventId);
            return Ok(ApiResponse<ClubManagement.Shared.DTOs.EventFacilityRequirementsDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ClubManagement.Shared.DTOs.EventFacilityRequirementsDto>.ErrorResult($"Error retrieving facility requirements: {ex.Message}"));
        }
    }

    [HttpGet("facility/{facilityId}/availability")]
    public async Task<ActionResult<ApiResponse<ClubManagement.Shared.DTOs.FacilityAvailabilityResult>>> CheckFacilityAvailability(
        Guid facilityId, 
        [FromQuery] DateTime startTime, 
        [FromQuery] DateTime endTime, 
        [FromQuery] Guid? excludeEventId = null)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ClubManagement.Shared.DTOs.FacilityAvailabilityResult>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _facilityAuthService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var result = await _eventFacilityService.CheckFacilityAvailabilityAsync(facilityId, startTime, endTime, excludeEventId);
            return Ok(ApiResponse<ClubManagement.Shared.DTOs.FacilityAvailabilityResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ClubManagement.Shared.DTOs.FacilityAvailabilityResult>.ErrorResult($"Error checking availability: {ex.Message}"));
        }
    }

    [HttpPost("{eventId}/book-facility/{facilityId}")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityBookingDto>>> BookFacilityForEvent(Guid eventId, Guid facilityId, [FromBody] BookFacilityRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Edit, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var result = await _eventFacilityService.BookFacilityForEventAsync(eventId, facilityId, request?.Notes ?? "");
            return Ok(ApiResponse<FacilityBookingDto>.SuccessResult(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<FacilityBookingDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityBookingDto>.ErrorResult($"Error booking facility: {ex.Message}"));
        }
    }

    [HttpDelete("{eventId}/cancel-facility-booking")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> CancelFacilityBookingForEvent(Guid eventId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Edit, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            await _eventFacilityService.CancelFacilityBookingForEventAsync(eventId);
            return Ok(ApiResponse<object>.SuccessResult("Facility booking cancelled successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Error cancelling booking: {ex.Message}"));
        }
    }

    [HttpGet("certification/{certificationType}/events")]
    public async Task<ActionResult<ApiResponse<List<EventListDto>>>> GetEventsByCertificationRequirement(string certificationType)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<EventListDto>>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var result = await _eventFacilityService.GetEventsByCertificationRequirementAsync(certificationType);
            return Ok(ApiResponse<List<EventListDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<EventListDto>>.ErrorResult($"Error retrieving events: {ex.Message}"));
        }
    }

    [HttpGet("tier/{tier}/events")]
    public async Task<ActionResult<ApiResponse<List<EventListDto>>>> GetEventsForMembershipTier(MembershipTier tier)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<EventListDto>>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Members can view events for their own tier
            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Member?.Tier != tier)
            {
                var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var result = await _eventFacilityService.GetEventsForMembershipTierAsync(tier);
            return Ok(ApiResponse<List<EventListDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<EventListDto>>.ErrorResult($"Error retrieving events: {ex.Message}"));
        }
    }

    [HttpPost("validate-requirements")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>>> ValidateEventFacilityRequirements([FromBody] CreateEventRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Create, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var result = await _eventFacilityService.ValidateEventFacilityRequirementsAsync(request);
            return Ok(ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>.ErrorResult($"Error validating requirements: {ex.Message}"));
        }
    }

    [HttpPost("{eventId}/validate-requirements")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>>> ValidateEventFacilityRequirementsForUpdate(Guid eventId, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var authResult = await _eventAuthService.CheckAuthorizationAsync(userId, EventAction.Edit, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var result = await _eventFacilityService.ValidateEventFacilityRequirementsAsync(eventId, request);
            return Ok(ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ClubManagement.Shared.DTOs.ValidationResult>.ErrorResult($"Error validating requirements: {ex.Message}"));
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        // This would typically use a service to get the current user
        // For now, returning null - this should be implemented based on your auth system
        return null;
    }
}

// Request DTOs for this controller
public class BookFacilityRequest
{
    public string? Notes { get; set; }
}