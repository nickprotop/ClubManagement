using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Client.Services;

public interface IMemberBookingService
{
    // Member booking retrieval
    Task<ApiResponse<PagedResult<FacilityBookingDto>>?> GetMemberBookingsAsync(Guid memberId, MemberBookingFilter filter);
    Task<ApiResponse<List<FacilityBookingDto>>?> GetMemberUpcomingBookingsAsync(Guid memberId, int daysAhead = 7);
    Task<ApiResponse<MemberBookingHistoryDto>?> GetMemberBookingHistoryAsync(Guid memberId, DateTime? startDate = null, DateTime? endDate = null);
    
    // Member booking operations
    Task<ApiResponse<FacilityBookingDto>?> CreateMemberBookingAsync(Guid memberId, CreateMemberBookingRequest request);
    Task<ApiResponse<BookingCancellationResult>?> CancelMemberBookingAsync(Guid memberId, Guid bookingId, string? reason = null);
    Task<ApiResponse<FacilityBookingDto>?> ModifyMemberBookingAsync(Guid memberId, Guid bookingId, ModifyBookingRequest request);
    
    // Member booking recommendations and preferences
    Task<ApiResponse<List<RecommendedBookingSlot>>?> GetRecommendedBookingSlotsAsync(Guid memberId, Guid facilityId, DateTime? preferredDate = null);
    Task<ApiResponse<MemberFacilityPreferencesDto>?> GetMemberPreferencesAsync(Guid memberId);
    Task<ApiResponse<MemberFacilityPreferencesDto>?> UpdateMemberPreferencesAsync(Guid memberId, UpdateMemberPreferencesRequest request);
    
    // Member access and availability
    Task<ApiResponse<MemberAccessStatusDto>?> GetMemberAccessStatusAsync(Guid memberId);
    Task<ApiResponse<BookingAvailabilityResult>?> CheckBookingAvailabilityAsync(Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime);
    Task<ApiResponse<List<FacilityDto>>?> GetMemberFavoriteFacilitiesAsync(Guid memberId);
}