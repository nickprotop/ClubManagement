using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Api.Extensions;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly ClubManagementDbContext _context;
    private readonly ITenantService _tenantService;

    public MembersController(ClubManagementDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MemberListDto>>>> GetMembers([FromQuery] MemberSearchRequest request)
    {
        try
        {
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<PagedResult<MemberListDto>>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var query = _context.Members
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
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return NotFound(ApiResponse<MemberDto>.ErrorResult("Member not found"));

            var memberDto = new MemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                MembershipNumber = member.MembershipNumber,
                Tier = member.Tier,
                Status = member.Status,
                JoinedAt = member.JoinedAt,
                MembershipExpiresAt = member.MembershipExpiresAt,
                LastVisitAt = member.LastVisitAt,
                Balance = member.Balance,
                EmergencyContact = member.EmergencyContact,
                MedicalInfo = member.MedicalInfo,
                CustomFields = member.CustomFields,
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
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            // Verify user exists
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("User not found"));

            // Check if user is already a member
            var existingMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == request.UserId);
            if (existingMember != null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("User is already a member"));

            // Generate membership number
            var membershipNumber = await GenerateMembershipNumberAsync();

            var member = new Member
            {
                UserId = request.UserId,
                MembershipNumber = membershipNumber,
                Tier = request.Tier,
                Status = MembershipStatus.Active,
                JoinedAt = DateTime.UtcNow,
                MembershipExpiresAt = request.MembershipExpiresAt,
                EmergencyContact = request.EmergencyContact,
                MedicalInfo = request.MedicalInfo,
                CustomFields = request.CustomFields,
                Balance = 0
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

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
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<MemberDto>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var member = await _context.Members.FindAsync(id);
            if (member == null)
                return NotFound(ApiResponse<MemberDto>.ErrorResult("Member not found"));

            member.Tier = request.Tier;
            member.MembershipExpiresAt = request.MembershipExpiresAt;
            member.EmergencyContact = request.EmergencyContact;
            member.MedicalInfo = request.MedicalInfo;
            member.CustomFields = request.CustomFields;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var member = await _context.Members.FindAsync(id);
            if (member == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Member not found"));

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

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
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            var member = await _context.Members.FindAsync(id);
            if (member == null)
                return NotFound(ApiResponse<bool>.ErrorResult("Member not found"));

            member.Status = status;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
            var tenantId = this.GetCurrentTenantId();
            
            // Get tenant and switch to tenant schema
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return BadRequest(ApiResponse<List<MemberSearchDto>>.ErrorResult("Invalid tenant"));
                
            await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");
            
            if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.SearchTerm.Length < 2)
                return Ok(ApiResponse<List<MemberSearchDto>>.SuccessResult(new List<MemberSearchDto>()));

            var query = _context.Members
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

    private async Task<string> GenerateMembershipNumberAsync()
    {
        // Generate a unique membership number
        string membershipNumber;
        bool exists;
        do
        {
            var number = new Random().Next(100000, 999999);
            membershipNumber = $"MB{DateTime.UtcNow.Year}{number:D6}";
            exists = await _context.Members.AnyAsync(m => m.MembershipNumber == membershipNumber);
        } while (exists);

        return membershipNumber;
    }
}