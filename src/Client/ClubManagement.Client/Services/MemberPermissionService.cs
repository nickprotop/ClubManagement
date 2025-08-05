using Microsoft.Extensions.Logging;
using ClubManagement.Shared.Models.Authorization;
using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class MemberPermissionService : IMemberPermissionService
{
    private readonly IApiService _apiService;
    private readonly ILogger<MemberPermissionService> _logger;
    private readonly Dictionary<string, (MemberPermissions permissions, DateTime cachedAt)> _permissionsCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public MemberPermissionService(IApiService apiService, ILogger<MemberPermissionService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<MemberPermissions> GetMemberPermissionsAsync(Guid? memberId = null)
    {
        var cacheKey = memberId?.ToString() ?? "general";
        
        // Check cache first
        if (_permissionsCache.TryGetValue(cacheKey, out var cachedEntry))
        {
            if (DateTime.UtcNow - cachedEntry.cachedAt < _cacheExpiration)
            {
                return cachedEntry.permissions;
            }
            // Remove expired cache entry
            _permissionsCache.Remove(cacheKey);
        }

        try
        {
            var endpoint = memberId.HasValue 
                ? $"api/members/{memberId}/permissions"
                : "api/members/permissions";
                
            var response = await _apiService.GetAsync<MemberPermissions>(endpoint);
            
            if (response?.Success == true && response.Data != null)
            {
                // Cache the result
                _permissionsCache[cacheKey] = (response.Data, DateTime.UtcNow);
                return response.Data;
            }
            
            _logger.LogWarning("Failed to get member permissions: {Message}", response?.Message ?? "Unknown error");
            return new MemberPermissions(); // Return empty permissions on failure
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting member permissions for member {MemberId}", memberId);
            return new MemberPermissions(); // Return empty permissions on error
        }
    }

    public async Task<MemberPermissions> GetGeneralMemberPermissionsAsync()
    {
        return await GetMemberPermissionsAsync(null);
    }

    public void ClearPermissionsCache()
    {
        _permissionsCache.Clear();
        _logger.LogDebug("Permissions cache cleared");
    }

    public async Task<bool> CanPerformActionAsync(MemberAction action, Guid? memberId = null)
    {
        var permissions = await GetMemberPermissionsAsync(memberId);
        
        return action switch
        {
            MemberAction.View => permissions.CanView,
            MemberAction.ViewDetails => permissions.CanViewDetails,
            MemberAction.Create => permissions.CanCreate,
            MemberAction.Edit => permissions.CanEdit,
            MemberAction.Delete => permissions.CanDelete,
            MemberAction.ChangeStatus => permissions.CanChangeStatus,
            MemberAction.ExtendMembership => permissions.CanExtendMembership,
            MemberAction.ChangeTier => permissions.CanChangeTier,
            MemberAction.ViewOwnProfile => permissions.CanViewOwnProfile,
            MemberAction.EditOwnProfile => permissions.CanEditOwnProfile,
            MemberAction.ViewOtherProfiles => permissions.CanViewOtherProfiles,
            MemberAction.EditOtherProfiles => permissions.CanEditOtherProfiles,
            MemberAction.ViewMedicalInfo => permissions.CanViewMedicalInfo,
            MemberAction.EditMedicalInfo => permissions.CanEditMedicalInfo,
            MemberAction.ViewEmergencyContact => permissions.CanViewEmergencyContact,
            MemberAction.EditEmergencyContact => permissions.CanEditEmergencyContact,
            MemberAction.ViewFinancialInfo => permissions.CanViewFinancialInfo,
            MemberAction.EditBalance => permissions.CanEditBalance,
            MemberAction.ExportMemberData => permissions.CanExportMemberData,
            MemberAction.ViewMemberActivity => permissions.CanViewMemberActivity,
            MemberAction.ViewAllMembers => permissions.CanViewAllMembers,
            MemberAction.ImpersonateMember => permissions.CanImpersonateMember,
            MemberAction.ViewContactInfo => permissions.CanViewContactInfo,
            MemberAction.SendNotifications => permissions.CanSendNotifications,
            MemberAction.AccessMemberDirectory => permissions.CanAccessMemberDirectory,
            _ => false
        };
    }
}