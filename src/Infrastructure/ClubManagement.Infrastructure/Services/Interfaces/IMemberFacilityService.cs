using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services.Interfaces;

public interface IMemberFacilityService
{
    // Member Access Validation
    Task<MemberFacilityAccessDto> CheckMemberAccessAsync(Guid facilityId, Guid memberId);
    Task<bool> ValidateMembershipTierAccessAsync(Guid facilityId, MembershipTier memberTier);
    Task<List<string>> GetMissingCertificationsAsync(Guid facilityId, Guid memberId);
    
    // Booking Limits Management
    Task<BookingLimitValidationResult> ValidateBookingLimitsAsync(Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null);
    Task<List<MemberBookingLimitDto>> GetMemberBookingLimitsAsync(Guid memberId);
    Task<MemberBookingUsageDto> GetMemberBookingUsageAsync(Guid memberId, DateTime? date = null);
    Task<MemberBookingLimitDto> CreateBookingLimitAsync(CreateMemberBookingLimitRequest request);
    Task<MemberBookingLimitDto> UpdateBookingLimitAsync(Guid limitId, UpdateMemberBookingLimitRequest request);
    Task DeleteBookingLimitAsync(Guid limitId);
    
    // Default Tier Limits
    Task<MemberBookingLimitDto> GetDefaultTierLimitsAsync(MembershipTier tier);
    Task ApplyDefaultTierLimitsAsync(Guid memberId, MembershipTier tier);
    
    // Usage Tracking
    Task UpdateMemberUsageAsync(Guid memberId);
    Task<List<MemberBookingUsageDto>> GetTierUsageStatsAsync(MembershipTier? tier = null, DateTime? startDate = null, DateTime? endDate = null);
    
    // Certification Management
    Task<List<FacilityCertificationDto>> GetMemberCertificationsAsync(Guid memberId);
    Task<FacilityCertificationDto> CreateCertificationAsync(CreateCertificationRequest request);
    Task<bool> IsCertificationValidAsync(Guid memberId, string certificationType);
    Task<List<FacilityCertificationDto>> GetExpiringCertificationsAsync(int daysAhead = 30);
    
    // Member-Specific Facility Lists
    Task<List<FacilityDto>> GetAccessibleFacilitiesAsync(Guid memberId);
    Task<List<FacilityDto>> GetRestrictedFacilitiesAsync(Guid memberId);
    
    // Booking Recommendations
    Task<List<DateTime>> GetAvailableBookingTimesAsync(Guid facilityId, Guid memberId, DateTime date, int durationMinutes = 60);
    Task<List<FacilityDto>> RecommendFacilitiesAsync(Guid memberId, DateTime? preferredTime = null, int? durationMinutes = null);
}