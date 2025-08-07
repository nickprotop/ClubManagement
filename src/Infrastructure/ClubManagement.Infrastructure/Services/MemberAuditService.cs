using Microsoft.Extensions.Logging;
using ClubManagement.Domain.Entities;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ClubManagement.Infrastructure.Services;

public interface IMemberAuditService
{
    Task LogMemberActionAsync(ClubManagementDbContext tenantContext, Guid performedBy, Guid? targetMemberId, MemberAction action, string? details = null, Dictionary<string, object>? metadata = null);
    Task LogImpersonationStartAsync(ClubManagementDbContext tenantContext, Guid adminUserId, Guid targetMemberId, string reason, int durationMinutes);
    Task LogImpersonationEndAsync(ClubManagementDbContext tenantContext, Guid sessionId, string? reason = null);
    Task LogPermissionDeniedAsync(ClubManagementDbContext tenantContext, Guid userId, MemberAction action, Guid? targetMemberId, string[] reasons);
    Task<List<MemberAuditLog>> GetMemberAuditLogsAsync(ClubManagementDbContext tenantContext, Guid? memberId = null, DateTime? fromDate = null, DateTime? toDate = null, int maxResults = 100);
}

public class MemberAuditService : IMemberAuditService
{
    private readonly ILogger<MemberAuditService> _logger;

    public MemberAuditService(ILogger<MemberAuditService> logger)
    {
        _logger = logger;
    }

    public async Task LogMemberActionAsync(ClubManagementDbContext tenantContext, Guid performedBy, Guid? targetMemberId, MemberAction action, string? details = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var auditLog = new MemberAuditLog
            {
                Id = Guid.NewGuid(),
                PerformedBy = performedBy,
                TargetMemberId = targetMemberId,
                Action = action.ToString(),
                Details = details,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent()
            };

            tenantContext.MemberAuditLogs.Add(auditLog);
            await tenantContext.SaveChangesAsync();

            _logger.LogInformation("Member action logged: {Action} by {PerformedBy} on {TargetMemberId}", 
                action, performedBy, targetMemberId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging member action: {Action} by {PerformedBy}", action, performedBy);
        }
    }

    public async Task LogImpersonationStartAsync(ClubManagementDbContext tenantContext, Guid adminUserId, Guid targetMemberId, string reason, int durationMinutes)
    {
        var metadata = new Dictionary<string, object>
        {
            ["reason"] = reason,
            ["duration_minutes"] = durationMinutes,
            ["started_at"] = DateTime.UtcNow
        };

        await LogMemberActionAsync(tenantContext, adminUserId, targetMemberId, MemberAction.ImpersonateMember, 
            $"Started impersonation for {durationMinutes} minutes: {reason}", metadata);
    }

    public async Task LogImpersonationEndAsync(ClubManagementDbContext tenantContext, Guid sessionId, string? reason = null)
    {
        try
        {
            var session = await tenantContext.ImpersonationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session != null)
            {
                var metadata = new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["ended_at"] = DateTime.UtcNow,
                    ["duration_actual"] = DateTime.UtcNow - session.StartedAt
                };

                if (!string.IsNullOrEmpty(reason))
                    metadata["end_reason"] = reason;

                await LogMemberActionAsync(tenantContext, session.AdminUserId, session.TargetMemberId, MemberAction.ImpersonateMember,
                    $"Ended impersonation{(reason != null ? $": {reason}" : "")}", metadata);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging impersonation end for session {SessionId}", sessionId);
        }
    }

    public async Task LogPermissionDeniedAsync(ClubManagementDbContext tenantContext, Guid userId, MemberAction action, Guid? targetMemberId, string[] reasons)
    {
        var metadata = new Dictionary<string, object>
        {
            ["denied_reasons"] = reasons,
            ["attempted_action"] = action.ToString()
        };

        await LogMemberActionAsync(tenantContext, userId, targetMemberId, action, 
            $"Permission denied: {string.Join(", ", reasons)}", metadata);

        _logger.LogWarning("Permission denied for user {UserId} attempting {Action} on member {TargetMemberId}: {Reasons}",
            userId, action, targetMemberId, string.Join(", ", reasons));
    }

    public async Task<List<MemberAuditLog>> GetMemberAuditLogsAsync(ClubManagementDbContext tenantContext, Guid? memberId = null, DateTime? fromDate = null, DateTime? toDate = null, int maxResults = 100)
    {
        try
        {
            var query = tenantContext.MemberAuditLogs.AsQueryable();

            if (memberId.HasValue)
                query = query.Where(log => log.TargetMemberId == memberId || log.PerformedBy == memberId);

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate);

            return await query
                .OrderByDescending(log => log.Timestamp)
                .Take(maxResults)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving member audit logs");
            return new List<MemberAuditLog>();
        }
    }

    private string? GetCurrentIpAddress()
    {
        // This would be injected through IHttpContextAccessor in a real implementation
        // For now, return a placeholder
        return "Unknown"; // TODO: Implement proper IP address extraction
    }

    private string? GetCurrentUserAgent()
    {
        // This would be injected through IHttpContextAccessor in a real implementation
        // For now, return a placeholder
        return "Unknown"; // TODO: Implement proper User-Agent extraction
    }
}