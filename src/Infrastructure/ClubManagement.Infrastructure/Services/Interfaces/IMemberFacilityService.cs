using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services.Interfaces;

public interface IMemberFacilityService
{
    // Member Access Validation
    Task<MemberFacilityAccessDto> CheckMemberAccessAsync(Guid facilityId, Guid memberId);
    Task<MemberFacilityAccessDto> CheckMemberAccessAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid facilityId, Guid memberId);
    Task<bool> ValidateMembershipTierAccessAsync(Guid facilityId, MembershipTier memberTier);
    Task<bool> ValidateMembershipTierAccessAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid facilityId, MembershipTier memberTier);
    Task<List<string>> GetMissingCertificationsAsync(Guid facilityId, Guid memberId);
    Task<List<string>> GetMissingCertificationsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid facilityId, Guid memberId);
    
    // Booking Limits Management
    Task<BookingLimitValidationResult> ValidateBookingLimitsAsync(Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null);
    Task<BookingLimitValidationResult> ValidateBookingLimitsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null);
    Task<List<MemberBookingLimitDto>> GetMemberBookingLimitsAsync(Guid memberId);
    Task<List<MemberBookingLimitDto>> GetMemberBookingLimitsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId);
    Task<MemberBookingUsageDto> GetMemberBookingUsageAsync(Guid memberId, DateTime? date = null);
    Task<MemberBookingUsageDto> GetMemberBookingUsageAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId, DateTime? date = null);
    Task<MemberBookingLimitDto> CreateBookingLimitAsync(CreateMemberBookingLimitRequest request);
    Task<MemberBookingLimitDto> CreateBookingLimitAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, CreateMemberBookingLimitRequest request);
    Task<MemberBookingLimitDto> UpdateBookingLimitAsync(Guid limitId, UpdateMemberBookingLimitRequest request);
    Task<MemberBookingLimitDto> UpdateBookingLimitAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid limitId, UpdateMemberBookingLimitRequest request);
    Task DeleteBookingLimitAsync(Guid limitId);
    Task DeleteBookingLimitAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid limitId);
    
    // Default Tier Limits
    Task<MemberBookingLimitDto> GetDefaultTierLimitsAsync(MembershipTier tier);
    Task<MemberBookingLimitDto> GetDefaultTierLimitsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, MembershipTier tier);
    Task ApplyDefaultTierLimitsAsync(Guid memberId, MembershipTier tier);
    Task ApplyDefaultTierLimitsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId, MembershipTier tier);
    
    // Usage Tracking
    Task UpdateMemberUsageAsync(Guid memberId);
    Task UpdateMemberUsageAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId);
    Task<List<MemberBookingUsageDto>> GetTierUsageStatsAsync(MembershipTier? tier = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<MemberBookingUsageDto>> GetTierUsageStatsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, MembershipTier? tier = null, DateTime? startDate = null, DateTime? endDate = null);
    
    // Certification Management
    Task<List<FacilityCertificationDto>> GetMemberCertificationsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId);
    Task<FacilityCertificationDto> CreateCertificationAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, CreateCertificationRequest request);
    Task<bool> IsCertificationValidAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId, string certificationType);
    Task<List<FacilityCertificationDto>> GetExpiringCertificationsAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, int daysAhead = 30);
    
    // Member-Specific Facility Lists
    Task<List<FacilityDto>> GetAccessibleFacilitiesAsync(Guid memberId);
    Task<List<FacilityDto>> GetAccessibleFacilitiesAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId);
    Task<List<FacilityDto>> GetRestrictedFacilitiesAsync(Guid memberId);
    Task<List<FacilityDto>> GetRestrictedFacilitiesAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId);
    
    // Booking Recommendations
    Task<List<DateTime>> GetAvailableBookingTimesAsync(Guid facilityId, Guid memberId, DateTime date, int durationMinutes = 60);
    Task<List<DateTime>> GetAvailableBookingTimesAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid facilityId, Guid memberId, DateTime date, int durationMinutes = 60);
    Task<List<FacilityDto>> RecommendFacilitiesAsync(Guid memberId, DateTime? preferredTime = null, int? durationMinutes = null);
    Task<List<FacilityDto>> RecommendFacilitiesAsync(ClubManagement.Infrastructure.Data.ClubManagementDbContext tenantContext, Guid memberId, DateTime? preferredTime = null, int? durationMinutes = null);
}