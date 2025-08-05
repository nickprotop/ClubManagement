using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IImpersonationService
{
    Task<ImpersonationResult> StartImpersonationAsync(Guid memberId, string reason, int durationMinutes = 30);
    Task<bool> EndImpersonationAsync(string? reason = null);
    Task<ImpersonationStatusDto?> GetImpersonationStatusAsync();
    Task<List<ImpersonationSessionDto>> GetImpersonationSessionsAsync(bool activeOnly = false);
    event Action<ImpersonationStatusDto?> ImpersonationStatusChanged;
}