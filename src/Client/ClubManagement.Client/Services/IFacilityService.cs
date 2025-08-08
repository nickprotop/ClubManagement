using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IFacilityService
{
    // Facilities
    Task<ApiResponse<List<FacilityDto>>> GetFacilitiesAsync();
    Task<ApiResponse<FacilityDto>> GetFacilityByIdAsync(Guid id);
    Task<ApiResponse<FacilityDto>> CreateFacilityAsync(CreateFacilityRequest request);
    Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(Guid id, UpdateFacilityRequest request);
    Task<ApiResponse<object>> DeleteFacilityAsync(Guid id);
    Task<ApiResponse<FacilityPermissions>> GetFacilityPermissionsAsync(Guid? id = null);
    Task<ApiResponse<List<FacilityDto>>> SearchFacilitiesAsync(string? searchTerm = null, Guid? facilityTypeId = null, FacilityStatus? status = null);
    Task<ApiResponse<List<FacilityDto>>> GetFacilitiesByTypeAsync(Guid facilityTypeId);
    Task<ApiResponse<List<FacilityUsageHistoryDto>>> GetFacilityUsageHistoryAsync(Guid facilityId);
    Task<ApiResponse<List<FacilityDto>>> GetAvailableFacilitiesForMemberAsync(Guid memberId, DateTime? startDateTime = null, DateTime? endDateTime = null);
    Task<ApiResponse<MemberFacilityAccessDto>> CheckMemberAccessAsync(Guid facilityId, Guid memberId);

    // Facility Types
    Task<ApiResponse<List<FacilityTypeDto>>> GetFacilityTypesAsync();
    Task<ApiResponse<FacilityTypeDto>> GetFacilityTypeByIdAsync(Guid id);
    Task<ApiResponse<FacilityTypeDto>> CreateFacilityTypeAsync(CreateFacilityTypeRequest request);
    Task<ApiResponse<FacilityTypeDto>> UpdateFacilityTypeAsync(Guid id, UpdateFacilityTypeRequest request);
    Task<ApiResponse<object>> DeleteFacilityTypeAsync(Guid id);
    Task<ApiResponse<object>> ActivateFacilityTypeAsync(Guid id);
    Task<ApiResponse<object>> DeactivateFacilityTypeAsync(Guid id);

    // Facility Bookings
    Task<ApiResponse<List<FacilityBookingDto>>> GetFacilityBookingsAsync(Guid? facilityId = null, Guid? memberId = null, DateTime? startDate = null, DateTime? endDate = null, BookingStatus? status = null);
    Task<ApiResponse<FacilityBookingDto>> GetFacilityBookingByIdAsync(Guid id);
    Task<ApiResponse<FacilityBookingDto>> CreateFacilityBookingAsync(CreateFacilityBookingRequest request);
    Task<ApiResponse<FacilityBookingDto>> UpdateFacilityBookingAsync(Guid id, UpdateFacilityBookingRequest request);
    Task<ApiResponse<object>> CancelFacilityBookingAsync(Guid id);
    Task<ApiResponse<FacilityBookingDto>> CheckInFacilityBookingAsync(Guid id, CheckInBookingRequest request);
    Task<ApiResponse<FacilityBookingDto>> CheckOutFacilityBookingAsync(Guid id, CheckOutBookingRequest request);
    Task<ApiResponse<List<FacilityAvailabilityDto>>> CheckFacilityAvailabilityAsync(Guid facilityId, DateTime startDateTime, DateTime endDateTime);
    Task<ApiResponse<List<FacilityBookingConflictDto>>> CheckBookingConflictsAsync(Guid facilityId, DateTime startDateTime, DateTime endDateTime, Guid? excludeBookingId = null);
    Task<ApiResponse<List<FacilityBookingDto>>> GetMemberBookingsAsync(Guid memberId, DateTime? startDate = null, DateTime? endDate = null, BookingStatus? status = null);
    Task<ApiResponse<FacilityBookingDto>> CreateMemberBookingAsync(CreateMemberFacilityBookingRequest request);
    Task<ApiResponse<FacilityBookingStatsDto>> GetBookingStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<List<FacilityUsageReportDto>>> GetFacilityUsageReportAsync(DateTime startDate, DateTime endDate);

    // Certification Management
    Task<ApiResponse<PagedResult<FacilityCertificationDto>>> GetCertificationsAsync(CertificationListFilter filter);
    Task<ApiResponse<FacilityCertificationDto>> GetCertificationByIdAsync(Guid id);
    Task<ApiResponse<List<FacilityCertificationDto>>> GetMemberCertificationsAsync(Guid memberId);
    Task<ApiResponse<FacilityCertificationDto>> CreateCertificationAsync(CreateCertificationRequest request);
    Task<ApiResponse<FacilityCertificationDto>> DeactivateCertificationAsync(Guid id, DeactivateCertificationRequest request);
    Task<ApiResponse<List<FacilityCertificationDto>>> GetExpiringCertificationsAsync(int daysAhead = 30);
    Task<ApiResponse<List<string>>> GetCertificationTypesAsync();

    // Member Facility Access
    Task<ApiResponse<List<FacilityDto>>> GetAccessibleFacilitiesForMemberAsync(Guid memberId);
    Task<ApiResponse<List<FacilityDto>>> GetRestrictedFacilitiesForMemberAsync(Guid memberId);
    Task<ApiResponse<BookingLimitValidationResult>> ValidateBookingLimitsAsync(ValidateBookingLimitsRequest request);
}