using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Client.Services;

public class MemberService : IMemberService
{
    private readonly IApiService _apiService;

    public MemberService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<PagedResult<MemberListDto>>?> GetMembersAsync(MemberSearchRequest request)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        
        if (request.Tier.HasValue)
            queryParams.Add($"tier={request.Tier}");
        
        if (request.Status.HasValue)
            queryParams.Add($"status={request.Status}");
        
        if (request.JoinedAfter.HasValue)
            queryParams.Add($"joinedAfter={request.JoinedAfter:yyyy-MM-dd}");
        
        if (request.JoinedBefore.HasValue)
            queryParams.Add($"joinedBefore={request.JoinedBefore:yyyy-MM-dd}");
        
        queryParams.Add($"page={request.Page}");
        queryParams.Add($"pageSize={request.PageSize}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        
        return await _apiService.GetAsync<PagedResult<MemberListDto>>($"api/members{queryString}");
    }

    public async Task<ApiResponse<MemberDto>?> GetMemberAsync(Guid id)
    {
        return await _apiService.GetAsync<MemberDto>($"api/members/{id}");
    }

    public async Task<ApiResponse<MemberDto>?> CreateMemberAsync(CreateMemberRequest request)
    {
        return await _apiService.PostAsync<MemberDto>("api/members", request);
    }

    public async Task<ApiResponse<MemberDto>?> UpdateMemberAsync(Guid id, UpdateMemberRequest request)
    {
        return await _apiService.PutAsync<MemberDto>($"api/members/{id}", request);
    }

    public async Task<ApiResponse<bool>?> DeleteMemberAsync(Guid id)
    {
        return await _apiService.DeleteAsync<bool>($"api/members/{id}");
    }

    public async Task<ApiResponse<bool>?> UpdateMemberStatusAsync(Guid id, MembershipStatus status)
    {
        return await _apiService.PostAsync<bool>($"api/members/{id}/status", status);
    }
}