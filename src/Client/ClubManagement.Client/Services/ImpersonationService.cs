using Microsoft.Extensions.Logging;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public class ImpersonationService : IImpersonationService
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private readonly ILogger<ImpersonationService> _logger;
    private ImpersonationStatusDto? _currentStatus;

    public event Action<ImpersonationStatusDto?>? ImpersonationStatusChanged;

    public ImpersonationService(
        IApiService apiService,
        IAuthService authService,
        ILogger<ImpersonationService> logger)
    {
        _apiService = apiService;
        _authService = authService;
        _logger = logger;
    }

    public async Task<ImpersonationResult> StartImpersonationAsync(Guid memberId, string reason, int durationMinutes = 30)
    {
        try
        {
            var request = new StartImpersonationRequest
            {
                TargetMemberId = memberId,
                Reason = reason,
                DurationMinutes = durationMinutes
            };

            var response = await _apiService.PostAsync<ImpersonationResult>($"api/members/{memberId}/impersonate", request);
            
            if (response?.Success == true && response.Data != null)
            {
                // If impersonation was successful, update the auth token
                if (response.Data.Succeeded && !string.IsNullOrEmpty(response.Data.Token))
                {
                    await _authService.SetTokenAsync(response.Data.Token);
                    
                    // Update status and notify listeners
                    await RefreshImpersonationStatusAsync();
                }
                
                return response.Data;
            }

            var errorMessage = response?.Message ?? "Unknown error occurred";
            _logger.LogWarning("Failed to start impersonation: {Message}", errorMessage);
            return ImpersonationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting impersonation for member {MemberId}", memberId);
            return ImpersonationResult.Failed("An error occurred while starting impersonation");
        }
    }

    public async Task<bool> EndImpersonationAsync(string? reason = null)
    {
        try
        {
            var request = new EndImpersonationRequest { Reason = reason };
            var response = await _apiService.PostAsync<bool>("api/members/end-impersonation", request);
            
            if (response?.Success == true)
            {
                // Clear the impersonation status and notify listeners
                _currentStatus = new ImpersonationStatusDto { IsImpersonating = false };
                ImpersonationStatusChanged?.Invoke(_currentStatus);
                
                // Note: The backend should handle token invalidation/refresh
                // The frontend may need to refresh the page or get a new token
                return true;
            }

            _logger.LogWarning("Failed to end impersonation: {Message}", response?.Message ?? "Unknown error");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending impersonation");
            return false;
        }
    }

    public async Task<ImpersonationStatusDto?> GetImpersonationStatusAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<ImpersonationStatusDto>("api/members/impersonation-status");
            
            if (response?.Success == true && response.Data != null)
            {
                _currentStatus = response.Data;
                return response.Data;
            }

            _logger.LogWarning("Failed to get impersonation status: {Message}", response?.Message ?? "Unknown error");
            return new ImpersonationStatusDto { IsImpersonating = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting impersonation status");
            return new ImpersonationStatusDto { IsImpersonating = false };
        }
    }

    public async Task<List<ImpersonationSessionDto>> GetImpersonationSessionsAsync(bool activeOnly = false)
    {
        try
        {
            var endpoint = $"api/members/impersonation-sessions?activeOnly={activeOnly}";
            var response = await _apiService.GetAsync<List<ImpersonationSessionDto>>(endpoint);
            
            if (response?.Success == true && response.Data != null)
            {
                return response.Data;
            }

            _logger.LogWarning("Failed to get impersonation sessions: {Message}", response?.Message ?? "Unknown error");
            return new List<ImpersonationSessionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting impersonation sessions");
            return new List<ImpersonationSessionDto>();
        }
    }

    private async Task RefreshImpersonationStatusAsync()
    {
        var status = await GetImpersonationStatusAsync();
        ImpersonationStatusChanged?.Invoke(status);
    }
}