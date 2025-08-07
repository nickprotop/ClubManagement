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
public class MembersController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IMemberAuthorizationService _authService;
    private readonly IImpersonationService _impersonationService;

    public MembersController(
        ITenantDbContextFactory tenantDbContextFactory, 
        ITenantService tenantService,
        IMemberAuthorizationService authService,
        IImpersonationService impersonationService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _authService = authService;
        _impersonationService = impersonationService;
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<MemberPermissions>>> GetMemberPermissions(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberPermissions>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetMemberPermissionsAsync(userId, tenantContext, id);
            return Ok(ApiResponse<MemberPermissions>.SuccessResult(permissions));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberPermissions>.ErrorResult($"Error retrieving member permissions: {ex.Message}"));
        }
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<MemberPermissions>>> GetGeneralMemberPermissions()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberPermissions>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var permissions = await _authService.GetMemberPermissionsAsync(userId, tenantContext);
            return Ok(ApiResponse<MemberPermissions>.SuccessResult(permissions));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<MemberPermissions>.ErrorResult($"Unauthorized: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberPermissions>.ErrorResult($"Error retrieving member permissions: {ex.Message}"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MemberListDto>>>> GetMembers([FromQuery] MemberSearchRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<MemberListDto>>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            var query = tenantContext.Members
                .Include(m => m.User)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(m => 
                    m.MembershipNumber.ToLower().Contains(searchLower) ||
                    m.User.FirstName.ToLower().Contains(searchLower) ||
                    m.User.LastName.ToLower().Contains(searchLower) ||
                    m.User.Email.ToLower().Contains(searchLower));
            }

            if (request.Tier.HasValue)
                query = query.Where(m => m.Tier == request.Tier.Value);

            if (request.Status.HasValue)
                query = query.Where(m => m.Status == request.Status.Value);

            if (request.JoinedAfter.HasValue)
                query = query.Where(m => m.JoinedAt >= request.JoinedAfter.Value);

            if (request.JoinedBefore.HasValue)
                query = query.Where(m => m.JoinedAt <= request.JoinedBefore.Value);

            var totalCount = await query.CountAsync();
            
            var members = await query
                .OrderBy(m => m.User.LastName)
                .ThenBy(m => m.User.FirstName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new MemberListDto
                {
                    Id = m.Id,
                    MembershipNumber = m.MembershipNumber,
                    FirstName = m.User.FirstName,
                    LastName = m.User.LastName,
                    Email = m.User.Email,
                    Tier = m.Tier,
                    Status = m.Status,
                    JoinedAt = m.JoinedAt,
                    LastVisitAt = m.LastVisitAt
                })
                .ToListAsync();

            var result = new PagedResult<MemberListDto>
            {
                Items = members,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Ok(ApiResponse<PagedResult<MemberListDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<MemberListDto>>.ErrorResult($"Error retrieving members: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MemberDto>>> GetMember(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.ViewDetails, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            var member = await tenantContext.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return NotFound(ApiResponse<MemberDto>.ErrorResult("Member not found"));

            var memberDto = new MemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                MembershipNumber = member.MembershipNumber,
                
                // Flattened personal information
                FirstName = member.User.FirstName,
                LastName = member.User.LastName,
                Email = member.User.Email,
                PhoneNumber = member.User.PhoneNumber,
                DateOfBirth = member.User.DateOfBirth,
                Gender = member.User.Gender,
                
                // Membership information
                Tier = member.Tier,
                Status = member.Status,
                JoinedAt = member.JoinedAt,
                MembershipExpiresAt = member.MembershipExpiresAt,
                LastVisitAt = member.LastVisitAt,
                Balance = member.Balance,
                
                // Related information
                EmergencyContact = member.EmergencyContact,
                MedicalInfo = member.MedicalInfo,
                CustomFields = member.CustomFields,
                
                // Original User object for backward compatibility
                User = new UserProfileDto
                {
                    Id = member.User.Id,
                    Email = member.User.Email,
                    FirstName = member.User.FirstName,
                    LastName = member.User.LastName,
                    PhoneNumber = member.User.PhoneNumber,
                    Role = member.User.Role,
                    Status = member.User.Status,
                    ProfilePhotoUrl = member.User.ProfilePhotoUrl,
                    LastLoginAt = member.User.LastLoginAt,
                    EmailVerified = member.User.EmailVerified,
                    CustomFields = member.User.CustomFields
                }
            };

            return Ok(ApiResponse<MemberDto>.SuccessResult(memberDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberDto>.ErrorResult($"Error retrieving member: {ex.Message}"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MemberDto>>> CreateMember([FromBody] CreateMemberRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.Create, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            // Check if email already exists
            var existingUser = await tenantContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("A user with this email already exists"));

            // Create the User first
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Role = UserRole.Member,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.ToString(),
                PasswordHash = "TEMP_HASH", // This should be set when user first logs in
                PasswordSalt = "TEMP_SALT",
                PasswordChangedAt = DateTime.UtcNow
            };

            tenantContext.Users.Add(newUser);

            // Generate membership number
            var membershipNumber = await GenerateMembershipNumberAsync(tenantContext);

            // Create Emergency Contact
            EmergencyContact? emergencyContact = null;
            if (!string.IsNullOrEmpty(request.EmergencyContactName))
            {
                emergencyContact = new EmergencyContact
                {
                    Name = request.EmergencyContactName,
                    PhoneNumber = request.EmergencyContactPhone,
                    Relationship = request.EmergencyContactRelationship
                };
            }

            // Create Medical Info
            MedicalInfo? medicalInfo = null;
            if (!string.IsNullOrEmpty(request.Allergies) || !string.IsNullOrEmpty(request.MedicalConditions) || !string.IsNullOrEmpty(request.Medications))
            {
                medicalInfo = new MedicalInfo
                {
                    Allergies = request.Allergies ?? string.Empty,
                    MedicalConditions = request.MedicalConditions ?? string.Empty,
                    Medications = request.Medications ?? string.Empty
                };
            }

            // Create the Member
            var member = new Member
            {
                UserId = newUser.Id,
                MembershipNumber = membershipNumber,
                Tier = request.Tier,
                Status = request.Status,
                JoinedAt = DateTime.UtcNow,
                MembershipExpiresAt = request.MembershipExpiresAt,
                EmergencyContact = emergencyContact,
                MedicalInfo = medicalInfo,
                CustomFields = request.CustomFields,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.ToString()
            };

            tenantContext.Members.Add(member);
            await tenantContext.SaveChangesAsync();

            // Return the created member with user details
            var createdMember = await GetMember(member.Id);
            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, createdMember.Value);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberDto>.ErrorResult($"Error creating member: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<MemberDto>>> UpdateMember(Guid id, [FromBody] UpdateMemberRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.Edit, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            var member = await tenantContext.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
                return NotFound(ApiResponse<MemberDto>.ErrorResult("Member not found"));

            // Update User information
            member.User.FirstName = request.FirstName;
            member.User.LastName = request.LastName;
            member.User.Email = request.Email;
            member.User.PhoneNumber = request.PhoneNumber;
            member.User.DateOfBirth = request.DateOfBirth;
            member.User.Gender = request.Gender;
            member.User.UpdatedAt = DateTime.UtcNow;
            member.User.UpdatedBy = userId.ToString();

            // Update Member information
            member.Tier = request.Tier;
            member.Status = request.Status;
            member.MembershipExpiresAt = request.MembershipExpiresAt;
            member.CustomFields = request.CustomFields;
            member.UpdatedAt = DateTime.UtcNow;
            member.UpdatedBy = userId.ToString();

            // Update Emergency Contact
            if (!string.IsNullOrEmpty(request.EmergencyContactName))
            {
                member.EmergencyContact = new EmergencyContact
                {
                    Name = request.EmergencyContactName,
                    PhoneNumber = request.EmergencyContactPhone,
                    Relationship = request.EmergencyContactRelationship
                };
            }
            else
            {
                member.EmergencyContact = null;
            }

            // Update Medical Info
            if (!string.IsNullOrEmpty(request.Allergies) || !string.IsNullOrEmpty(request.MedicalConditions) || !string.IsNullOrEmpty(request.Medications))
            {
                member.MedicalInfo = new MedicalInfo
                {
                    Allergies = request.Allergies ?? string.Empty,
                    MedicalConditions = request.MedicalConditions ?? string.Empty,
                    Medications = request.Medications ?? string.Empty
                };
            }
            else
            {
                member.MedicalInfo = null;
            }

            await tenantContext.SaveChangesAsync();

            var updatedMember = await GetMember(id);
            return updatedMember;
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MemberDto>.ErrorResult($"Error updating member: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteMember(Guid id)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.Delete, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            var member = await tenantContext.Members.FindAsync(id);
            if (member == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Member not found"));

            tenantContext.Members.Remove(member);
            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Member deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error deleting member: {ex.Message}"));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateMemberStatus(Guid id, [FromBody] MembershipStatus status)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.ChangeStatus, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            var member = await tenantContext.Members.FindAsync(id);
            if (member == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Member not found"));

            member.Status = status;
            member.UpdatedAt = DateTime.UtcNow;

            await tenantContext.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Member status updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error updating member status: {ex.Message}"));
        }
    }

    [HttpPost("search")]
    public async Task<ActionResult<ApiResponse<List<MemberSearchDto>>>> SearchMembers([FromBody] MemberQuickSearchRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<MemberSearchDto>>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.View, tenantContext);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.SearchTerm.Length < 2)
                return Ok(ApiResponse<List<MemberSearchDto>>.SuccessResult(new List<MemberSearchDto>()));

            var query = tenantContext.Members
                .Include(m => m.User)
                .Where(m => request.IncludeInactive || m.Status == MembershipStatus.Active);

            // Search across multiple fields
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(m => 
                m.User.FirstName.ToLower().Contains(searchTerm) ||
                m.User.LastName.ToLower().Contains(searchTerm) ||
                m.User.Email.ToLower().Contains(searchTerm) ||
                m.MembershipNumber.ToLower().Contains(searchTerm));

            var members = await query
                .OrderBy(m => m.User.FirstName)
                .ThenBy(m => m.User.LastName)
                .Take(request.MaxResults)
                .Select(m => new MemberSearchDto
                {
                    Id = m.Id,
                    FirstName = m.User.FirstName,
                    LastName = m.User.LastName,
                    Email = m.User.Email,
                    MembershipNumber = m.MembershipNumber,
                    Status = m.Status,
                    ProfilePhotoUrl = m.User.ProfilePhotoUrl
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MemberSearchDto>>.SuccessResult(members));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MemberSearchDto>>.ErrorResult($"Error searching members: {ex.Message}"));
        }
    }

    [HttpPost("{id}/impersonate")]
    public async Task<ActionResult<ApiResponse<ImpersonationResult>>> StartImpersonation(Guid id, [FromBody] StartImpersonationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and create tenant-specific database context
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ImpersonationResult>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Check permissions
            var authResult = await _authService.CheckAuthorizationAsync(userId, MemberAction.ImpersonateMember, tenantContext, id);
            if (!authResult.Succeeded)
                return Forbid(string.Join(", ", authResult.Reasons));
            
            // Set the target member ID from the route
            request.TargetMemberId = id;
            
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();
            
            var result = await _impersonationService.StartImpersonationAsync(tenantContext, userId, request, ipAddress, userAgent);
            
            if (result.Succeeded)
                return Ok(ApiResponse<ImpersonationResult>.SuccessResult(result));
            else
                return BadRequest(ApiResponse<ImpersonationResult>.ErrorResult(string.Join(", ", result.Reasons)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ImpersonationResult>.ErrorResult($"Error starting impersonation: {ex.Message}"));
        }
    }

    [HttpPost("end-impersonation")]
    public async Task<ActionResult<ApiResponse<bool>>> EndImpersonation([FromBody] EndImpersonationRequest request)
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            // Get current impersonation status
            var status = await _impersonationService.GetCurrentImpersonationStatusAsync(tenantContext, userId);
            if (status?.SessionId == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("No active impersonation session found"));
            
            var result = await _impersonationService.EndImpersonationAsync(tenantContext, status.SessionId.Value, request.Reason);
            
            if (result.Succeeded)
                return Ok(ApiResponse<bool>.SuccessResult(true, "Impersonation ended successfully"));
            else
                return BadRequest(ApiResponse<bool>.ErrorResult(string.Join(", ", result.Reasons)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult($"Error ending impersonation: {ex.Message}"));
        }
    }

    [HttpGet("impersonation-status")]
    public async Task<ActionResult<ApiResponse<ImpersonationStatusDto>>> GetImpersonationStatus()
    {
        try
        {
            var userId = this.GetCurrentUserId();
            var tenantId = this.GetCurrentTenantId();
            
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<ImpersonationStatusDto>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var status = await _impersonationService.GetCurrentImpersonationStatusAsync(tenantContext, userId);
            return Ok(ApiResponse<ImpersonationStatusDto>.SuccessResult(status ?? new ImpersonationStatusDto { IsImpersonating = false }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ImpersonationStatusDto>.ErrorResult($"Error retrieving impersonation status: {ex.Message}"));
        }
    }

    [HttpGet("impersonation-sessions")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<List<ImpersonationSessionDto>>>> GetImpersonationSessions([FromQuery] bool activeOnly = false)
    {
        try
        {
            var tenantId = this.GetCurrentTenantId();
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<ImpersonationSessionDto>>.ErrorResult("Invalid tenant"));
                
            using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
            
            var sessions = activeOnly 
                ? await _impersonationService.GetActiveSessionsAsync(tenantContext)
                : await _impersonationService.GetSessionHistoryAsync(tenantContext);
                
            return Ok(ApiResponse<List<ImpersonationSessionDto>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<ImpersonationSessionDto>>.ErrorResult($"Error retrieving impersonation sessions: {ex.Message}"));
        }
    }

    private async Task<string> GenerateMembershipNumberAsync(ClubManagementDbContext tenantContext)
    {
        // Generate a unique membership number
        string membershipNumber;
        bool exists;
        do
        {
            var number = new Random().Next(100000, 999999);
            membershipNumber = $"MB{DateTime.UtcNow.Year}{number:D6}";
            exists = await tenantContext.Members.AnyAsync(m => m.MembershipNumber == membershipNumber);
        } while (exists);

        return membershipNumber;
    }
}