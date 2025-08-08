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
[Route("api/facility-certifications")]
[Authorize]
public class FacilityCertificationsController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IMemberFacilityService _memberFacilityService;
    private readonly IFacilityAuthorizationService _authService;

    public FacilityCertificationsController(
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

    [HttpGet]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PagedResult<FacilityCertificationDto>>>> GetCertifications([FromQuery] CertificationListFilter filter)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<FacilityCertificationDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var query = tenantContext.MemberFacilityCertifications
                .Include(c => c.Member)
                    .ThenInclude(m => m.User)
                .Include(c => c.CertifiedByUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(c => c.CertificationType.Contains(filter.Search) ||
                                       c.Member.User.FirstName.Contains(filter.Search) ||
                                       c.Member.User.LastName.Contains(filter.Search) ||
                                       c.Member.User.Email.Contains(filter.Search));
            }

            if (filter.MemberId.HasValue)
                query = query.Where(c => c.MemberId == filter.MemberId.Value);

            if (!string.IsNullOrEmpty(filter.CertificationType))
                query = query.Where(c => c.CertificationType == filter.CertificationType);

            if (filter.IsActive.HasValue)
                query = query.Where(c => c.IsActive == filter.IsActive.Value);

            if (filter.ExpiringWithinDays.HasValue)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(filter.ExpiringWithinDays.Value);
                query = query.Where(c => c.ExpiryDate.HasValue && 
                                       c.ExpiryDate.Value <= cutoffDate &&
                                       c.ExpiryDate.Value > DateTime.UtcNow);
            }

            if (filter.ExpiredOnly == true)
            {
                query = query.Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value <= DateTime.UtcNow);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "certifieddate" => filter.SortDescending ? query.OrderByDescending(c => c.CertifiedDate) : query.OrderBy(c => c.CertifiedDate),
                "expirydate" => filter.SortDescending ? query.OrderByDescending(c => c.ExpiryDate) : query.OrderBy(c => c.ExpiryDate),
                "membername" => filter.SortDescending ? query.OrderByDescending(c => c.Member.User.LastName) : query.OrderBy(c => c.Member.User.LastName),
                "certificationtype" => filter.SortDescending ? query.OrderByDescending(c => c.CertificationType) : query.OrderBy(c => c.CertificationType),
                _ => query.OrderByDescending(c => c.CertifiedDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new FacilityCertificationDto
                {
                    Id = c.Id,
                    MemberId = c.MemberId,
                    MemberName = c.Member.User.FirstName + " " + c.Member.User.LastName,
                    CertificationType = c.CertificationType,
                    CertifiedDate = c.CertifiedDate,
                    ExpiryDate = c.ExpiryDate,
                    CertifiedByUserId = c.CertifiedByUserId,
                    CertifiedByUserName = c.CertifiedByUser.FirstName + " " + c.CertifiedByUser.LastName,
                    IsActive = c.IsActive,
                    Notes = c.Notes,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResult<FacilityCertificationDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Ok(ApiResponse<PagedResult<FacilityCertificationDto>>.SuccessResult(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<PagedResult<FacilityCertificationDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<FacilityCertificationDto>>.ErrorResult($"Error retrieving certifications: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FacilityCertificationDto>>> GetCertification(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityCertificationDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var certification = await tenantContext.MemberFacilityCertifications
                .Include(c => c.Member)
                    .ThenInclude(m => m.User)
                .Include(c => c.CertifiedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certification == null)
                return NotFound(ApiResponse<FacilityCertificationDto>.ErrorResult("Certification not found"));

            var dto = new FacilityCertificationDto
            {
                Id = certification.Id,
                MemberId = certification.MemberId,
                MemberName = certification.Member.User.FirstName + " " + certification.Member.User.LastName,
                CertificationType = certification.CertificationType,
                CertifiedDate = certification.CertifiedDate,
                ExpiryDate = certification.ExpiryDate,
                CertifiedByUserId = certification.CertifiedByUserId,
                CertifiedByUserName = certification.CertifiedByUser.FirstName + " " + certification.CertifiedByUser.LastName,
                IsActive = certification.IsActive,
                Notes = certification.Notes,
                CreatedAt = certification.CreatedAt
            };

            return Ok(ApiResponse<FacilityCertificationDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityCertificationDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityCertificationDto>.ErrorResult($"Error retrieving certification: {ex.Message}"));
        }
    }

    [HttpGet("member/{memberId}")]
    public async Task<ActionResult<ApiResponse<List<FacilityCertificationDto>>>> GetMemberCertifications(Guid memberId)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityCertificationDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Members can view their own certifications, staff can view any
            var currentUser = await tenantContext.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser?.Member?.Id != memberId)
            {
                var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
                if (!authResult.Succeeded)
                    return Forbid(string.Join(", ", authResult.Reasons));
            }

            var certifications = await _memberFacilityService.GetMemberCertificationsAsync(tenantContext, memberId);

            return Ok(ApiResponse<List<FacilityCertificationDto>>.SuccessResult(certifications));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityCertificationDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityCertificationDto>>.ErrorResult($"Error retrieving member certifications: {ex.Message}"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityCertificationDto>>> CreateCertification([FromBody] CreateCertificationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityCertificationDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Create, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            // Validate member exists
            var member = await tenantContext.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == request.MemberId);
            if (member == null)
                return BadRequest(ApiResponse<FacilityCertificationDto>.ErrorResult("Member not found"));

            // Check if certification already exists and is active
            var existingCertification = await tenantContext.MemberFacilityCertifications
                .FirstOrDefaultAsync(c => c.MemberId == request.MemberId && 
                                        c.CertificationType == request.CertificationType && 
                                        c.IsActive);

            if (existingCertification != null)
                return BadRequest(ApiResponse<FacilityCertificationDto>.ErrorResult($"Member already has an active {request.CertificationType} certification"));

            var certification = new MemberFacilityCertification
            {
                Id = Guid.NewGuid(),
                MemberId = request.MemberId,
                CertificationType = request.CertificationType,
                CertifiedDate = DateTime.UtcNow,
                ExpiryDate = request.ExpiryDate,
                CertifiedByUserId = userId,
                IsActive = true,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = this.GetCurrentUserEmail() ?? "System"
            };

            tenantContext.MemberFacilityCertifications.Add(certification);
            await tenantContext.SaveChangesAsync();

            // Return the created certification with full details
            var createdCertification = await tenantContext.MemberFacilityCertifications
                .Include(c => c.Member)
                    .ThenInclude(m => m.User)
                .Include(c => c.CertifiedByUser)
                .FirstOrDefaultAsync(c => c.Id == certification.Id);

            var dto = new FacilityCertificationDto
            {
                Id = createdCertification!.Id,
                MemberId = createdCertification.MemberId,
                MemberName = createdCertification.Member.User.FirstName + " " + createdCertification.Member.User.LastName,
                CertificationType = createdCertification.CertificationType,
                CertifiedDate = createdCertification.CertifiedDate,
                ExpiryDate = createdCertification.ExpiryDate,
                CertifiedByUserId = createdCertification.CertifiedByUserId,
                CertifiedByUserName = createdCertification.CertifiedByUser.FirstName + " " + createdCertification.CertifiedByUser.LastName,
                IsActive = createdCertification.IsActive,
                Notes = createdCertification.Notes,
                CreatedAt = createdCertification.CreatedAt
            };

            return CreatedAtAction(nameof(GetCertification), new { id = certification.Id }, ApiResponse<FacilityCertificationDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityCertificationDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityCertificationDto>.ErrorResult($"Error creating certification: {ex.Message}"));
        }
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<FacilityCertificationDto>>> DeactivateCertification(Guid id, [FromBody] DeactivateCertificationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<FacilityCertificationDto>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.Edit, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var certification = await tenantContext.MemberFacilityCertifications
                .Include(c => c.Member)
                    .ThenInclude(m => m.User)
                .Include(c => c.CertifiedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certification == null)
                return NotFound(ApiResponse<FacilityCertificationDto>.ErrorResult("Certification not found"));

            certification.IsActive = false;
            certification.Notes = string.IsNullOrEmpty(request.Reason) ? certification.Notes : 
                                $"{certification.Notes}\n[DEACTIVATED {DateTime.UtcNow:yyyy-MM-dd}]: {request.Reason}".Trim();
            certification.UpdatedAt = DateTime.UtcNow;
            certification.UpdatedBy = this.GetCurrentUserEmail() ?? "System";

            await tenantContext.SaveChangesAsync();

            var dto = new FacilityCertificationDto
            {
                Id = certification.Id,
                MemberId = certification.MemberId,
                MemberName = certification.Member.User.FirstName + " " + certification.Member.User.LastName,
                CertificationType = certification.CertificationType,
                CertifiedDate = certification.CertifiedDate,
                ExpiryDate = certification.ExpiryDate,
                CertifiedByUserId = certification.CertifiedByUserId,
                CertifiedByUserName = certification.CertifiedByUser.FirstName + " " + certification.CertifiedByUser.LastName,
                IsActive = certification.IsActive,
                Notes = certification.Notes,
                CreatedAt = certification.CreatedAt
            };

            return Ok(ApiResponse<FacilityCertificationDto>.SuccessResult(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FacilityCertificationDto>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FacilityCertificationDto>.ErrorResult($"Error deactivating certification: {ex.Message}"));
        }
    }

    [HttpGet("expiring")]
    [Authorize(Roles = "Staff,Instructor,Coach,Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<List<FacilityCertificationDto>>>> GetExpiringCertifications([FromQuery] int daysAhead = 30)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<FacilityCertificationDto>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            var authResult = await _authService.CheckAuthorizationAsync(userId, FacilityAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));

            var expiringCertifications = await _memberFacilityService.GetExpiringCertificationsAsync(tenantContext, daysAhead);

            return Ok(ApiResponse<List<FacilityCertificationDto>>.SuccessResult(expiringCertifications));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<FacilityCertificationDto>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<FacilityCertificationDto>>.ErrorResult($"Error retrieving expiring certifications: {ex.Message}"));
        }
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCertificationTypes()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<string>>.ErrorResult("Invalid tenant"));

            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

            // Get certification types from facilities and existing certifications
            // Use ToListAsync() first to bring data to client side, then use LINQ to Objects
            var facilities = await tenantContext.Facilities.ToListAsync();
            var facilityRequiredCerts = facilities
                .SelectMany(f => f.RequiredCertifications ?? new List<string>())
                .Where(cert => !string.IsNullOrEmpty(cert))
                .Distinct()
                .ToList();

            var existingCertTypes = await tenantContext.MemberFacilityCertifications
                .Select(c => c.CertificationType)
                .Distinct()
                .ToListAsync();

            // Combine and deduplicate
            var allTypes = facilityRequiredCerts.Union(existingCertTypes)
                .Where(t => !string.IsNullOrEmpty(t))
                .OrderBy(t => t)
                .ToList();

            // Add common certification types if none exist
            if (!allTypes.Any())
            {
                allTypes = new List<string>
                {
                    "Pool Safety",
                    "Equipment Training",
                    "First Aid",
                    "CPR",
                    "Gym Orientation",
                    "Rock Climbing",
                    "Kayak Safety",
                    "Tennis Court",
                    "Yoga Instructor",
                    "Personal Training"
                };
            }

            return Ok(ApiResponse<List<string>>.SuccessResult(allTypes));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<string>>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<string>>.ErrorResult($"Error retrieving certification types: {ex.Message}"));
        }
    }
}