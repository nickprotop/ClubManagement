using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Client.Services;

public interface IMemberService
{
    Task<ApiResponse<PagedResult<MemberListDto>>?> GetMembersAsync(MemberSearchRequest request);
    Task<ApiResponse<MemberDto>?> GetMemberAsync(Guid id);
    Task<ApiResponse<MemberDto>?> CreateMemberAsync(CreateMemberRequest request);
    Task<ApiResponse<MemberDto>?> UpdateMemberAsync(Guid id, UpdateMemberRequest request);
    Task<ApiResponse<bool>?> DeleteMemberAsync(Guid id);
    Task<ApiResponse<bool>?> UpdateMemberStatusAsync(Guid id, MembershipStatus status);
    Task<ApiResponse<List<MemberSearchDto>>?> SearchMembersAsync(MemberQuickSearchRequest request);
}