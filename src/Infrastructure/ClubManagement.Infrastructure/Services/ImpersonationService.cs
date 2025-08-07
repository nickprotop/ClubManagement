using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Authorization;
using ClubManagement.Infrastructure.Authentication;
using ClubManagement.Domain.Entities;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Infrastructure.Services;

public class ImpersonationService : IImpersonationService
{
    private readonly IJwtService _jwtService;
    private readonly IMemberAuthorizationService _memberAuthService;
    private readonly IMemberAuditService _auditService;
    private readonly ILogger<ImpersonationService> _logger;

    public ImpersonationService(
        IJwtService jwtService,
        IMemberAuthorizationService memberAuthService,
        IMemberAuditService auditService,
        ILogger<ImpersonationService> logger)
    {
        _jwtService = jwtService;
        _memberAuthService = memberAuthService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ImpersonationResult> StartImpersonationAsync(
        ClubManagementDbContext tenantContext,
        Guid adminUserId, 
        StartImpersonationRequest request, 
        string? ipAddress = null, 
        string? userAgent = null)
    {
        try
        {
            // Validate permission to impersonate
            var authResult = await ValidateImpersonationPermissionAsync(tenantContext, adminUserId, request.TargetMemberId);
            if (!authResult.Succeeded)
                return ImpersonationResult.Failed(authResult.Reasons);

            // Check if admin is already impersonating someone
            var existingSession = await tenantContext.ImpersonationSessions
                .FirstOrDefaultAsync(s => s.AdminUserId == adminUserId && s.IsActive);
            
            if (existingSession != null)
            {
                // End existing session first
                await EndImpersonationAsync(tenantContext, existingSession.Id, "Starting new impersonation session");
            }

            // Get target member and user details
            var targetMember = await tenantContext.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == request.TargetMemberId);
            
            if (targetMember == null)
                return ImpersonationResult.Failed("Target member not found");

            // Create impersonation session
            var session = new ImpersonationSession
            {
                AdminUserId = adminUserId,
                TargetMemberId = request.TargetMemberId,
                Reason = request.Reason,
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Min(request.DurationMinutes, 120)), // Max 2 hours
                IsActive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            tenantContext.ImpersonationSessions.Add(session);
            await tenantContext.SaveChangesAsync();

            // Generate impersonation token
            var token = await _jwtService.GenerateImpersonationTokenAsync(targetMember.User, adminUserId, session.Id);

            // Log the impersonation start with audit service
            await _auditService.LogImpersonationStartAsync(tenantContext, adminUserId, request.TargetMemberId, request.Reason, request.DurationMinutes);

            // Log the impersonation start (existing logging)
            _logger.LogWarning("Impersonation started: Admin {AdminUserId} impersonating Member {TargetMemberId} (Session: {SessionId}). Reason: {Reason}",
                adminUserId, request.TargetMemberId, session.Id, request.Reason);

            session.AddAction("Impersonation session started");
            await tenantContext.SaveChangesAsync();

            return ImpersonationResult.Success(session.Id, token, session.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting impersonation for admin {AdminUserId} targeting member {TargetMemberId}",
                adminUserId, request.TargetMemberId);
            return ImpersonationResult.Failed("An error occurred while starting impersonation");
        }
    }

    public async Task<AuthorizationResult> EndImpersonationAsync(ClubManagementDbContext tenantContext, Guid sessionId, string? reason = null)
    {
        try
        {
            var session = await tenantContext.ImpersonationSessions.FindAsync(sessionId);
            if (session == null)
                return AuthorizationResult.Failed("Impersonation session not found");

            if (!session.IsActive)
                return AuthorizationResult.Failed("Impersonation session is already ended");

            session.End();
            if (!string.IsNullOrEmpty(reason))
                session.AddAction($"Session ended: {reason}");

            // Log the impersonation end with audit service
            await _auditService.LogImpersonationEndAsync(tenantContext, sessionId, reason);

            await tenantContext.SaveChangesAsync();

            _logger.LogWarning("Impersonation ended: Session {SessionId} (Duration: {Duration}). Reason: {Reason}",
                sessionId, session.Duration, reason ?? "Manual termination");

            return AuthorizationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending impersonation session {SessionId}", sessionId);
            return AuthorizationResult.Failed("An error occurred while ending impersonation");
        }
    }

    public async Task<ImpersonationStatusDto?> GetCurrentImpersonationStatusAsync(ClubManagementDbContext tenantContext, Guid userId)
    {
        // Check if this user is being impersonated
        var targetSession = await tenantContext.ImpersonationSessions
            .Include(s => s.AdminUser)
            .Include(s => s.TargetMember)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(s => s.TargetMember.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

        if (targetSession != null)
        {
            return new ImpersonationStatusDto
            {
                IsImpersonating = true,
                SessionId = targetSession.Id,
                TargetMemberName = $"{targetSession.TargetMember.User.FirstName} {targetSession.TargetMember.User.LastName}",
                TargetMemberId = targetSession.TargetMemberId,
                AdminName = $"{targetSession.AdminUser.FirstName} {targetSession.AdminUser.LastName}",
                AdminUserId = targetSession.AdminUserId,
                StartedAt = targetSession.StartedAt,
                ExpiresAt = targetSession.ExpiresAt,
                Reason = targetSession.Reason
            };
        }

        // Check if this user is impersonating someone else
        var adminSession = await tenantContext.ImpersonationSessions
            .Include(s => s.AdminUser)
            .Include(s => s.TargetMember)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(s => s.AdminUserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

        if (adminSession != null)
        {
            return new ImpersonationStatusDto
            {
                IsImpersonating = true,
                SessionId = adminSession.Id,
                TargetMemberName = $"{adminSession.TargetMember.User.FirstName} {adminSession.TargetMember.User.LastName}",
                TargetMemberId = adminSession.TargetMemberId,
                AdminName = $"{adminSession.AdminUser.FirstName} {adminSession.AdminUser.LastName}",
                AdminUserId = adminSession.AdminUserId,
                StartedAt = adminSession.StartedAt,
                ExpiresAt = adminSession.ExpiresAt,
                Reason = adminSession.Reason
            };
        }

        return new ImpersonationStatusDto { IsImpersonating = false };
    }

    public async Task<bool> IsUserImpersonatingAsync(ClubManagementDbContext tenantContext, Guid userId)
    {
        return await tenantContext.ImpersonationSessions
            .AnyAsync(s => s.AdminUserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<Guid?> GetOriginalUserIdAsync(ClubManagementDbContext tenantContext, Guid currentUserId)
    {
        var session = await tenantContext.ImpersonationSessions
            .FirstOrDefaultAsync(s => s.TargetMember.UserId == currentUserId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
        
        return session?.AdminUserId;
    }

    public async Task<List<ImpersonationSessionDto>> GetActiveSessionsAsync(ClubManagementDbContext tenantContext)
    {
        return await tenantContext.ImpersonationSessions
            .Include(s => s.AdminUser)
            .Include(s => s.TargetMember)
            .ThenInclude(m => m.User)
            .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .Select(s => new ImpersonationSessionDto
            {
                Id = s.Id,
                AdminName = $"{s.AdminUser.FirstName} {s.AdminUser.LastName}",
                TargetMemberName = $"{s.TargetMember.User.FirstName} {s.TargetMember.User.LastName}",
                Reason = s.Reason,
                StartedAt = s.StartedAt,
                ExpiresAt = s.ExpiresAt,
                EndedAt = s.EndedAt,
                IsActive = s.IsActive,
                IpAddress = s.IpAddress,
                Duration = s.Duration,
                ActionsPerformed = s.ActionsPerformed
            })
            .ToListAsync();
    }

    public async Task<List<ImpersonationSessionDto>> GetSessionHistoryAsync(ClubManagementDbContext tenantContext, int page = 1, int pageSize = 50)
    {
        return await tenantContext.ImpersonationSessions
            .Include(s => s.AdminUser)
            .Include(s => s.TargetMember)
            .ThenInclude(m => m.User)
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ImpersonationSessionDto
            {
                Id = s.Id,
                AdminName = $"{s.AdminUser.FirstName} {s.AdminUser.LastName}",
                TargetMemberName = $"{s.TargetMember.User.FirstName} {s.TargetMember.User.LastName}",
                Reason = s.Reason,
                StartedAt = s.StartedAt,
                ExpiresAt = s.ExpiresAt,
                EndedAt = s.EndedAt,
                IsActive = s.IsActive,
                IpAddress = s.IpAddress,
                Duration = s.Duration,
                ActionsPerformed = s.ActionsPerformed
            })
            .ToListAsync();
    }

    public async Task LogImpersonationActionAsync(ClubManagementDbContext tenantContext, Guid sessionId, string action)
    {
        var session = await tenantContext.ImpersonationSessions.FindAsync(sessionId);
        if (session != null && session.IsActive)
        {
            session.AddAction(action);
            await tenantContext.SaveChangesAsync();
        }
    }

    public async Task CleanupExpiredSessionsAsync(ClubManagementDbContext tenantContext)
    {
        var expiredSessions = await tenantContext.ImpersonationSessions
            .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var session in expiredSessions)
        {
            session.End();
            session.AddAction("Session expired automatically");
        }

        if (expiredSessions.Any())
        {
            await tenantContext.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired impersonation sessions", expiredSessions.Count);
        }
    }

    public async Task<AuthorizationResult> ValidateImpersonationPermissionAsync(ClubManagementDbContext tenantContext, Guid adminUserId, Guid targetMemberId)
    {
        // Check if admin has impersonation permission
        var authResult = await _memberAuthService.CheckAuthorizationAsync(adminUserId, MemberAction.ImpersonateMember, tenantContext, targetMemberId);
        if (!authResult.Succeeded)
            return authResult;

        // Additional validation: cannot impersonate yourself
        var targetMember = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == targetMemberId);
        if (targetMember?.UserId == adminUserId)
            return AuthorizationResult.Failed("Cannot impersonate yourself");

        // Check if target user exists and is a member
        if (targetMember == null)
            return AuthorizationResult.Failed("Target member not found");

        return AuthorizationResult.Success();
    }
}