using System.ComponentModel.DataAnnotations;
using ClubManagement.Shared.Models;

namespace ClubManagement.Shared.DTOs;

public class FacilityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid FacilityTypeId { get; set; }
    public string FacilityTypeName { get; set; } = string.Empty;
    public string FacilityTypeIcon { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public FacilityStatus Status { get; set; }
    public int? Capacity { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool RequiresBooking { get; set; }
    public int MaxBookingDaysInAdvance { get; set; }
    public int MinBookingDurationMinutes { get; set; }
    public int MaxBookingDurationMinutes { get; set; }
    public TimeSpan? OperatingHoursStart { get; set; }
    public TimeSpan? OperatingHoursEnd { get; set; }
    public List<DayOfWeek> OperatingDays { get; set; } = new();
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    public List<string> RequiredCertifications { get; set; } = new();
    public int MemberConcurrentBookingLimit { get; set; }
    public bool RequiresMemberSupervision { get; set; }
    public decimal MemberHourlyRate { get; set; }
    public decimal NonMemberHourlyRate { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Computed properties
    public bool IsAvailableNow { get; set; }
    public bool IsCurrentlyBooked { get; set; }
    public int ActiveBookingsCount { get; set; }
    public FacilityBookingDto? CurrentBooking { get; set; }
    public bool CanMemberAccess { get; set; }
    public List<string> MissingCertifications { get; set; } = new();
}

public class FacilityTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public PropertySchema PropertySchema { get; set; } = new();
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    public List<string> RequiredCertifications { get; set; } = new();
    public bool RequiresSupervision { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Statistics
    public int FacilityCount { get; set; }
    public int AvailableCount { get; set; }
    public int BookedCount { get; set; }
    public int MaintenanceCount { get; set; }
}

public class FacilityBookingDto
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string? FacilityLocation { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public Guid? BookedByUserId { get; set; }
    public string? BookedByUserName { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus Status { get; set; }
    public BookingSource BookingSource { get; set; }
    public decimal? Cost { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Purpose { get; set; }
    public int? ParticipantCount { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? CheckedInByUserName { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public string? CheckedOutByUserName { get; set; }
    public bool NoShow { get; set; }
    public string? Notes { get; set; }
    public string? MemberNotes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUserName { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? RecurrenceGroupId { get; set; }
    public bool RequiresStaffSupervision { get; set; }
    public List<Guid> AdditionalParticipants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    
    // Computed properties
    public int DurationMinutes => (int)(EndDateTime - StartDateTime).TotalMinutes;
    public bool IsActive => Status == BookingStatus.Confirmed || Status == BookingStatus.CheckedIn;
    public bool CanBeCancelled => Status == BookingStatus.Confirmed && StartDateTime > DateTime.UtcNow;
    public bool CanBeModified => Status == BookingStatus.Confirmed && StartDateTime > DateTime.UtcNow.AddHours(2);
}

public class CreateFacilityRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public Guid FacilityTypeId { get; set; }
    
    public Dictionary<string, string> Properties { get; set; } = new();
    
    public int? Capacity { get; set; }
    
    public decimal? HourlyRate { get; set; }
    
    public bool RequiresBooking { get; set; } = true;
    
    public int MaxBookingDaysInAdvance { get; set; } = 30;
    
    public int MinBookingDurationMinutes { get; set; } = 60;
    
    public int MaxBookingDurationMinutes { get; set; } = 180;
    
    public TimeSpan? OperatingHoursStart { get; set; }
    
    public TimeSpan? OperatingHoursEnd { get; set; }
    
    public List<DayOfWeek> OperatingDays { get; set; } = new();
    
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    
    public List<string> RequiredCertifications { get; set; } = new();
    
    public int MemberConcurrentBookingLimit { get; set; } = 1;
    
    public bool RequiresMemberSupervision { get; set; } = false;
    
    public decimal MemberHourlyRate { get; set; }
    
    public decimal NonMemberHourlyRate { get; set; }
    
    public string? Location { get; set; }
    
    public string? Icon { get; set; }
}

public class UpdateFacilityRequest : CreateFacilityRequest
{
    public FacilityStatus Status { get; set; }
}

public class CreateFacilityTypeRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Icon { get; set; } = string.Empty;
    
    public PropertySchema PropertySchema { get; set; } = new();
    
    public int SortOrder { get; set; } = 0;
    
    public List<MembershipTier> AllowedMembershipTiers { get; set; } = new();
    
    public List<string> RequiredCertifications { get; set; } = new();
    
    public bool RequiresSupervision { get; set; } = false;
}

public class UpdateFacilityTypeRequest : CreateFacilityTypeRequest
{
    public bool IsActive { get; set; } = true;
}

public class CreateBookingRequest
{
    [Required]
    public Guid FacilityId { get; set; }
    
    [Required]
    public Guid MemberId { get; set; }
    
    [Required]
    public DateTime StartDateTime { get; set; }
    
    [Required]
    public DateTime EndDateTime { get; set; }
    
    public string? Purpose { get; set; }
    
    public int? ParticipantCount { get; set; }
    
    public string? Notes { get; set; }
    
    public string? MemberNotes { get; set; }
    
    public bool IsRecurring { get; set; } = false;
    
    public List<Guid> AdditionalParticipants { get; set; } = new();
}

public class MemberBookingRequest
{
    [Required]
    public Guid FacilityId { get; set; }
    
    [Required]
    public DateTime StartDateTime { get; set; }
    
    [Required]
    public DateTime EndDateTime { get; set; }
    
    public string? Purpose { get; set; }
    
    public int? ParticipantCount { get; set; }
    
    public string? MemberNotes { get; set; }
    
    public bool IsRecurring { get; set; } = false;
}

public class UpdateBookingRequest
{
    public DateTime? StartDateTime { get; set; }
    
    public DateTime? EndDateTime { get; set; }
    
    public string? Purpose { get; set; }
    
    public int? ParticipantCount { get; set; }
    
    public string? Notes { get; set; }
    
    public string? MemberNotes { get; set; }
}

public class CheckMemberAccessRequest
{
    [Required]
    public Guid MemberId { get; set; }
}

public class FacilityListFilter
{
    public string Search { get; set; } = string.Empty;
    public Guid? FacilityTypeId { get; set; }
    public FacilityStatus? Status { get; set; }
    public string? Location { get; set; }
    public bool? RequiresBooking { get; set; }
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }
    public MembershipTier? MembershipTier { get; set; }
    public bool? Available { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }
    public List<string> RequiredCertifications { get; set; } = new();
    public string SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class BookingListFilter
{
    public string Search { get; set; } = string.Empty;
    public Guid? FacilityId { get; set; }
    public Guid? MemberId { get; set; }
    public BookingStatus? Status { get; set; }
    public BookingSource? Source { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? NoShow { get; set; }
    public bool? RequiresSupervision { get; set; }
    public string SortBy { get; set; } = "startdatetime";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FacilityAccessResult
{
    public bool CanAccess { get; set; }
    public List<string> MissingCertifications { get; set; } = new();
    public MembershipTier? RequiredTier { get; set; }
    public string[] Restrictions { get; set; } = Array.Empty<string>();
}

public class AvailabilityResult
{
    public bool IsAvailable { get; set; }
    public List<string> ConflictReasons { get; set; } = new();
    public List<FacilityBookingDto> ConflictingBookings { get; set; } = new();
    public DateTime? NextAvailableSlot { get; set; }
}

public class MemberFacilityUsageDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int TotalHours { get; set; }
    public decimal TotalCost { get; set; }
    public int NoShowCount { get; set; }
    public int CancelledCount { get; set; }
    public List<FacilityUsageByTypeDto> UsageByType { get; set; } = new();
    public List<FacilityBookingDto> RecentBookings { get; set; } = new();
}

public class FacilityUsageByTypeDto
{
    public Guid FacilityTypeId { get; set; }
    public string FacilityTypeName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public int TotalHours { get; set; }
    public decimal TotalCost { get; set; }
}

public class FacilityDashboardDto
{
    public int TotalFacilities { get; set; }
    public int AvailableFacilities { get; set; }
    public int BookedFacilities { get; set; }
    public int MaintenanceFacilities { get; set; }
    public int OutOfOrderFacilities { get; set; }
    public int TodaysBookings { get; set; }
    public int ActiveBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public decimal TodaysRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public double AverageUtilizationRate { get; set; }
    public List<FacilityTypeUsageDto> TypeUsage { get; set; } = new();
}

public class FacilityTypeUsageDto
{
    public Guid FacilityTypeId { get; set; }
    public string FacilityTypeName { get; set; } = string.Empty;
    public int TotalFacilities { get; set; }
    public int BookedFacilities { get; set; }
    public double UtilizationRate { get; set; }
    public decimal Revenue { get; set; }
}

public class FacilityCertificationDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string CertificationType { get; set; } = string.Empty;
    public DateTime CertifiedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid CertifiedByUserId { get; set; }
    public string CertifiedByUserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Computed properties
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow.AddDays(30);
    public int? DaysUntilExpiry => ExpiryDate?.Subtract(DateTime.UtcNow).Days;
}

public class CreateCertificationRequest
{
    [Required]
    public Guid MemberId { get; set; }
    
    [Required]
    public string CertificationType { get; set; } = string.Empty;
    
    public DateTime? ExpiryDate { get; set; }
    
    public string? Notes { get; set; }
}

// Member Booking Limit DTOs
public class MemberBookingLimitDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid? FacilityTypeId { get; set; }
    public string? FacilityTypeName { get; set; }
    public MembershipTier? ApplicableTier { get; set; }
    
    // Booking Limits
    public int MaxConcurrentBookings { get; set; }
    public int MaxBookingsPerDay { get; set; }
    public int MaxBookingsPerWeek { get; set; }
    public int MaxBookingsPerMonth { get; set; }
    
    // Duration Limits
    public int MaxBookingDurationHours { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public int MinAdvanceBookingHours { get; set; }
    
    // Time Restrictions
    public TimeSpan? EarliestBookingTime { get; set; }
    public TimeSpan? LatestBookingTime { get; set; }
    public DayOfWeek[]? AllowedDays { get; set; }
    
    // Additional Restrictions
    public bool RequiresApproval { get; set; }
    public bool AllowRecurringBookings { get; set; }
    public int CancellationPenaltyHours { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMemberBookingLimitRequest
{
    [Required]
    public Guid MemberId { get; set; }
    
    public Guid? FacilityId { get; set; }
    public Guid? FacilityTypeId { get; set; }
    public MembershipTier? ApplicableTier { get; set; }
    
    [Range(1, 10)]
    public int MaxConcurrentBookings { get; set; } = 3;
    
    [Range(1, 20)]
    public int MaxBookingsPerDay { get; set; } = 2;
    
    [Range(1, 50)]
    public int MaxBookingsPerWeek { get; set; } = 5;
    
    [Range(1, 200)]
    public int MaxBookingsPerMonth { get; set; } = 20;
    
    [Range(1, 24)]
    public int MaxBookingDurationHours { get; set; } = 4;
    
    [Range(1, 365)]
    public int MaxAdvanceBookingDays { get; set; } = 30;
    
    [Range(0, 72)]
    public int MinAdvanceBookingHours { get; set; } = 2;
    
    public TimeSpan? EarliestBookingTime { get; set; }
    public TimeSpan? LatestBookingTime { get; set; }
    public DayOfWeek[]? AllowedDays { get; set; }
    
    public bool RequiresApproval { get; set; } = false;
    public bool AllowRecurringBookings { get; set; } = true;
    
    [Range(0, 168)]
    public int CancellationPenaltyHours { get; set; } = 24;
    
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateMemberBookingLimitRequest
{
    public Guid? FacilityId { get; set; }
    public Guid? FacilityTypeId { get; set; }
    public MembershipTier? ApplicableTier { get; set; }
    
    [Range(1, 10)]
    public int MaxConcurrentBookings { get; set; } = 3;
    
    [Range(1, 20)]
    public int MaxBookingsPerDay { get; set; } = 2;
    
    [Range(1, 50)]
    public int MaxBookingsPerWeek { get; set; } = 5;
    
    [Range(1, 200)]
    public int MaxBookingsPerMonth { get; set; } = 20;
    
    [Range(1, 24)]
    public int MaxBookingDurationHours { get; set; } = 4;
    
    [Range(1, 365)]
    public int MaxAdvanceBookingDays { get; set; } = 30;
    
    [Range(0, 72)]
    public int MinAdvanceBookingHours { get; set; } = 2;
    
    public TimeSpan? EarliestBookingTime { get; set; }
    public TimeSpan? LatestBookingTime { get; set; }
    public DayOfWeek[]? AllowedDays { get; set; }
    
    public bool RequiresApproval { get; set; }
    public bool AllowRecurringBookings { get; set; }
    
    [Range(0, 168)]
    public int CancellationPenaltyHours { get; set; } = 24;
    
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class MemberBookingUsageDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int BookingsToday { get; set; }
    public int BookingsThisWeek { get; set; }
    public int BookingsThisMonth { get; set; }
    public int ConcurrentBookings { get; set; }
    public decimal TotalHoursBooked { get; set; }
    public DateTime LastCalculated { get; set; }
    
    // Calculated fields for display
    public List<string> ActiveRestrictions { get; set; } = new();
    public bool HasReachedDailyLimit { get; set; }
    public bool HasReachedWeeklyLimit { get; set; }
    public bool HasReachedMonthlyLimit { get; set; }
    public bool HasReachedConcurrentLimit { get; set; }
}

public class BookingLimitValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Violations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public MemberBookingLimitDto? ApplicableLimit { get; set; }
    public MemberBookingUsageDto? CurrentUsage { get; set; }
    public int RemainingBookingsToday { get; set; }
    public int RemainingBookingsThisWeek { get; set; }
    public int RemainingBookingsThisMonth { get; set; }
    public int RemainingConcurrentSlots { get; set; }
}

public class ValidateBookingLimitsRequest
{
    [Required]
    public Guid MemberId { get; set; }
    
    [Required]
    public Guid FacilityId { get; set; }
    
    [Required]
    public DateTime StartDateTime { get; set; }
    
    [Required]
    public DateTime EndDateTime { get; set; }
    
    public Guid? ExcludeBookingId { get; set; }
}

// Certification Management DTOs
public class CertificationListFilter
{
    public string? Search { get; set; }
    public Guid? MemberId { get; set; }
    public string? CertificationType { get; set; }
    public bool? IsActive { get; set; }
    public int? ExpiringWithinDays { get; set; }
    public bool? ExpiredOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class DeactivateCertificationRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class CertificationStatsDto
{
    public int TotalCertifications { get; set; }
    public int ActiveCertifications { get; set; }
    public int ExpiringWithin30Days { get; set; }
    public int ExpiredCertifications { get; set; }
    public Dictionary<string, int> CertificationsByType { get; set; } = new();
    public Dictionary<string, int> CertificationsByMonth { get; set; } = new();
}

// Additional Booking DTOs
public class CreateFacilityBookingRequest : CreateBookingRequest
{
    public BookingSource BookingSource { get; set; } = BookingSource.StaffBooking;
    public Guid? BookedByUserId { get; set; }
    public decimal? Cost { get; set; }
    public string? PaymentMethod { get; set; }
}

public class CreateMemberFacilityBookingRequest : MemberBookingRequest
{
    // Member-specific booking request extending base MemberBookingRequest
}

public class UpdateFacilityBookingRequest : UpdateBookingRequest
{
    public BookingStatus? Status { get; set; }
    public string? CancellationReason { get; set; }
}

public class CheckInBookingRequest
{
    public int? ActualParticipantCount { get; set; }
    public string? Notes { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

public class CheckOutBookingRequest
{
    public string? Notes { get; set; }
    public bool NoShow { get; set; } = false;
    public int? ActualParticipantCount { get; set; }
    public DateTime? CheckedOutAt { get; set; }
}

// Availability and Conflict DTOs
public class FacilityAvailabilityDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public List<FacilityBookingDto> ConflictingBookings { get; set; } = new();
}

public class FacilityBookingConflictDto
{
    public Guid BookingId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus Status { get; set; }
    public string ConflictType { get; set; } = string.Empty; // "Overlap", "Adjacent", etc.
    public int OverlapMinutes { get; set; }
}

// Statistics and Reporting DTOs
public class FacilityBookingStatsDto
{
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int NoShowBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageBookingDuration { get; set; }
    public double UtilizationRate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<FacilityBookingStatsByTypeDto> BookingsByType { get; set; } = new();
    public List<FacilityBookingStatsByStatusDto> BookingsByStatus { get; set; } = new();
}

public class FacilityBookingStatsByTypeDto
{
    public Guid FacilityTypeId { get; set; }
    public string FacilityTypeName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public double UtilizationRate { get; set; }
}

public class FacilityBookingStatsByStatusDto
{
    public BookingStatus Status { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class FacilityUsageReportDto
{
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityTypeName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int TotalHours { get; set; }
    public decimal TotalRevenue { get; set; }
    public double UtilizationRate { get; set; }
    public int PeakUsageHour { get; set; }
    public DayOfWeek PeakUsageDay { get; set; }
    public List<FacilityUsageByTimeDto> HourlyUsage { get; set; } = new();
    public List<FacilityUsageByDayDto> DailyUsage { get; set; } = new();
}

public class FacilityUsageByTimeDto
{
    public int Hour { get; set; }
    public int BookingCount { get; set; }
    public double UtilizationRate { get; set; }
}

public class FacilityUsageByDayDto
{
    public DateTime Date { get; set; }
    public int BookingCount { get; set; }
    public int TotalHours { get; set; }
    public decimal Revenue { get; set; }
}

public class FacilityUsageHistoryDto
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public DateTime UsageDate { get; set; }
    public int BookingCount { get; set; }
    public int TotalMinutesBooked { get; set; }
    public int UniqueUsers { get; set; }
    public decimal Revenue { get; set; }
    public double UtilizationRate { get; set; }
    public string? Notes { get; set; }
}

// Member Facility Access DTOs
public class MemberFacilityAccessDto
{
    public bool CanAccess { get; set; }
    public string[] ReasonsDenied { get; set; } = Array.Empty<string>();
    public List<string> MissingCertifications { get; set; } = new();
    public string[] RequiredCertifications { get; set; } = Array.Empty<string>();
    public bool MembershipTierAllowed { get; set; }
    public bool CertificationsMet { get; set; }
    public MembershipTier? RequiredTier { get; set; }
    public DateTime? AccessExpiryDate { get; set; }
    public List<string> Warnings { get; set; } = new();
}

// Member Booking DTOs - moved from Infrastructure
public class MemberBookingFilter
{
    public BookingStatus? Status { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? FacilityTypeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IncludeRecurring { get; set; } = true;
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "startdatetime";
    public bool SortDescending { get; set; } = true;
}

public class MemberBookingHistoryDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int NoShowBookings { get; set; }
    public decimal TotalHoursBooked { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalSavings { get; set; } // Member discounts
    public double AverageBookingDuration { get; set; }
    public List<FacilityUsageStatsDto> FacilityUsage { get; set; } = new();
    public List<BookingPatternDto> BookingPatterns { get; set; } = new();
    public List<FacilityBookingDto> RecentBookings { get; set; } = new();
}

public class FacilityUsageStatsDto
{
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityTypeName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalCost { get; set; }
    public int Rank { get; set; } // Most used = 1
}

public class BookingPatternDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public int HourOfDay { get; set; }
    public int BookingCount { get; set; }
    public string FacilityTypeName { get; set; } = string.Empty;
}

public class CreateMemberBookingRequest
{
    public Guid FacilityId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Purpose { get; set; }
    public int? ParticipantCount { get; set; }
    public string? Notes { get; set; }
    public bool RequiresEquipment { get; set; } = false;
    public List<string> EquipmentNeeded { get; set; } = new();
    public bool SendReminders { get; set; } = true;
    public List<Guid> InvitedMembers { get; set; } = new();
}

public class BookingCancellationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public bool RefundIssued { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CancelledAt { get; set; }
    public int HoursBeforeStart { get; set; }
}

public class ModifyBookingRequest
{
    public DateTime? NewStartDateTime { get; set; }
    public DateTime? NewEndDateTime { get; set; }
    public Guid? NewFacilityId { get; set; }
    public string? NewPurpose { get; set; }
    public int? NewParticipantCount { get; set; }
    public string? ModificationReason { get; set; }
    public bool AcceptCharges { get; set; } = false;
}

public class RecommendedBookingSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; } // 0-1
    public bool IsAvailable { get; set; }
    public decimal? EstimatedCost { get; set; }
}

public class MemberFacilityPreferencesDto
{
    public Guid MemberId { get; set; }
    public List<Guid> PreferredFacilities { get; set; } = new();
    public List<TimeSlotPreference> PreferredTimeSlots { get; set; } = new();
    public int DefaultBookingDuration { get; set; } = 60; // minutes
    public bool AutoSelectBestTimes { get; set; } = true;
    public bool SendBookingReminders { get; set; } = true;
    public int ReminderMinutes { get; set; } = 30;
    public bool AllowWaitlist { get; set; } = true;
    public List<string> PreferredEquipment { get; set; } = new();
    public string? DefaultPurpose { get; set; }
    public bool ShareBookingsWithFriends { get; set; } = false;
    public NotificationPreferences Notifications { get; set; } = new();
}

public class TimeSlotPreference
{
    public DayOfWeek? DayOfWeek { get; set; } // null = any day
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Priority { get; set; } = 1; // 1 = highest
}

public class NotificationPreferences
{
    public bool BookingConfirmation { get; set; } = true;
    public bool BookingReminders { get; set; } = true;
    public bool FacilityAvailability { get; set; } = false;
    public bool MaintenanceNotices { get; set; } = true;
    public bool NewFacilities { get; set; } = false;
    public string PreferredMethod { get; set; } = "email"; // email, sms, push
}

public class UpdateMemberPreferencesRequest
{
    public List<Guid>? PreferredFacilities { get; set; }
    public List<TimeSlotPreference>? PreferredTimeSlots { get; set; }
    public int? DefaultBookingDuration { get; set; }
    public bool? AutoSelectBestTimes { get; set; }
    public bool? SendBookingReminders { get; set; }
    public int? ReminderMinutes { get; set; }
    public bool? AllowWaitlist { get; set; }
    public List<string>? PreferredEquipment { get; set; }
    public string? DefaultPurpose { get; set; }
    public bool? ShareBookingsWithFriends { get; set; }
    public NotificationPreferences? Notifications { get; set; }
}

public class MemberAccessStatusDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public MembershipTier Tier { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime? MembershipExpiry { get; set; }
    public bool HasActiveAccess { get; set; }
    public List<string> ActiveCertifications { get; set; } = new();
    public List<string> ExpiringSoonCertifications { get; set; } = new();
    public List<string> ExpiredCertifications { get; set; } = new();
    public int AccessibleFacilitiesCount { get; set; }
    public int RestrictedFacilitiesCount { get; set; }
    public MemberBookingLimitDto? CurrentLimits { get; set; }
    public MemberBookingUsageDto? CurrentUsage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class BookingAvailabilityResult
{
    public bool IsAvailable { get; set; }
    public List<string> BlockingReasons { get; set; } = new();
    public bool MeetsLimits { get; set; }
    public bool HasFacilityAccess { get; set; }
    public bool HasCertifications { get; set; }
    public decimal? EstimatedCost { get; set; }
    public List<AlternativeBookingSlot> Alternatives { get; set; } = new();
}

public class AlternativeBookingSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid? AlternativeFacilityId { get; set; }
    public string? AlternativeFacilityName { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}

// Recurring Booking DTOs
public class CreateRecurringBookingRequest
{
    public Guid FacilityId { get; set; }
    public DateTime FirstBookingStart { get; set; }
    public DateTime FirstBookingEnd { get; set; }
    public RecurrencePattern Pattern { get; set; } = new();
    public string? Purpose { get; set; }
    public int? ParticipantCount { get; set; }
    public string? Notes { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public bool AutoConfirm { get; set; } = false;
    public bool SendReminders { get; set; } = true;
}

public class RecurringBookingResult
{
    public bool Success { get; set; }
    public Guid? RecurringGroupId { get; set; }
    public string? Message { get; set; }
    public int BookingsCreated { get; set; }
    public int BookingsFailed { get; set; }
    public List<FacilityBookingDto> CreatedBookings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ModifyRecurringBookingRequest
{
    public RecurrenceUpdateType UpdateType { get; set; }
    public DateTime? NewStartTime { get; set; }
    public DateTime? NewEndTime { get; set; }
    public RecurrencePattern? NewPattern { get; set; }
    public DateTime? NewEndDate { get; set; }
    public string? Reason { get; set; }
}

public enum RecurrenceUpdateType
{
    ThisOccurrence,
    ThisAndFuture,
    AllOccurrences
}

public class RecurringBookingSummaryDto
{
    public Guid RecurringGroupId { get; set; }
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public RecurrencePattern Pattern { get; set; } = new();
    public DateTime FirstBooking { get; set; }
    public DateTime? LastBooking { get; set; }
    public int TotalOccurrences { get; set; }
    public int CompletedOccurrences { get; set; }
    public int UpcomingOccurrences { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}