using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services.Interfaces;

public interface IMemberBookingService
{
    /// <summary>
    /// Gets all bookings for a member with filtering and sorting
    /// </summary>
    Task<PagedResult<FacilityBookingDto>> GetMemberBookingsAsync(ClubManagementDbContext tenantContext, Guid memberId, MemberBookingFilter filter);
    
    /// <summary>
    /// Gets member's upcoming bookings (next 7 days by default)
    /// </summary>
    Task<List<FacilityBookingDto>> GetMemberUpcomingBookingsAsync(ClubManagementDbContext tenantContext, Guid memberId, int daysAhead = 7);
    
    /// <summary>
    /// Gets member's booking history with statistics
    /// </summary>
    Task<MemberBookingHistoryDto> GetMemberBookingHistoryAsync(ClubManagementDbContext tenantContext, Guid memberId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Creates a booking for a member with validation
    /// </summary>
    Task<FacilityBookingDto> CreateMemberBookingAsync(ClubManagementDbContext tenantContext, Guid memberId, CreateMemberBookingRequest request);
    
    /// <summary>
    /// Cancels a member's booking with penalty calculation
    /// </summary>
    Task<BookingCancellationResult> CancelMemberBookingAsync(ClubManagementDbContext tenantContext, Guid memberId, Guid bookingId, string? reason = null);
    
    /// <summary>
    /// Modifies an existing member booking
    /// </summary>
    Task<FacilityBookingDto> ModifyMemberBookingAsync(ClubManagementDbContext tenantContext, Guid memberId, Guid bookingId, ModifyBookingRequest request);
    
    /// <summary>
    /// Gets recommended booking times based on member's history and preferences
    /// </summary>
    Task<List<RecommendedBookingSlot>> GetRecommendedBookingSlotsAsync(ClubManagementDbContext tenantContext, Guid memberId, Guid facilityId, DateTime? preferredDate = null);
    
    /// <summary>
    /// Gets member's facility preferences and settings
    /// </summary>
    Task<MemberFacilityPreferencesDto> GetMemberPreferencesAsync(ClubManagementDbContext tenantContext, Guid memberId);
    
    /// <summary>
    /// Updates member's facility preferences
    /// </summary>
    Task<MemberFacilityPreferencesDto> UpdateMemberPreferencesAsync(ClubManagementDbContext tenantContext, Guid memberId, UpdateMemberPreferencesRequest request);
    
    /// <summary>
    /// Gets member's current facility access status
    /// </summary>
    Task<MemberAccessStatusDto> GetMemberAccessStatusAsync(ClubManagementDbContext tenantContext, Guid memberId);
    
    /// <summary>
    /// Checks if a member can book a facility at specific times
    /// </summary>
    Task<BookingAvailabilityResult> CheckBookingAvailabilityAsync(ClubManagementDbContext tenantContext, Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Gets member's favorite facilities based on booking history
    /// </summary>
    Task<List<FacilityDto>> GetMemberFavoriteFacilitiesAsync(ClubManagementDbContext tenantContext, Guid memberId);
    
    /// <summary>
    /// Sets up recurring bookings for a member
    /// </summary>
    Task<RecurringBookingResult> CreateRecurringBookingAsync(ClubManagementDbContext tenantContext, Guid memberId, CreateRecurringBookingRequest request);
    
    /// <summary>
    /// Manages recurring booking series
    /// </summary>
    Task<RecurringBookingResult> ModifyRecurringBookingAsync(ClubManagementDbContext tenantContext, Guid memberId, Guid recurringGroupId, ModifyRecurringBookingRequest request);
    
    /// <summary>
    /// Gets member's active recurring bookings
    /// </summary>
    Task<List<RecurringBookingSummaryDto>> GetMemberRecurringBookingsAsync(ClubManagementDbContext tenantContext, Guid memberId);
}