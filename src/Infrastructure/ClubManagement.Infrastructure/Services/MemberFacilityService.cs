using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services.Interfaces;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services;

public class MemberFacilityService : IMemberFacilityService
{
    private readonly ClubManagementDbContext _context;

    public MemberFacilityService(ClubManagementDbContext context)
    {
        _context = context;
    }

    public async Task<MemberFacilityAccessDto> CheckMemberAccessAsync(Guid facilityId, Guid memberId)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        var facility = await _context.Facilities
            .Include(f => f.FacilityType)
            .FirstOrDefaultAsync(f => f.Id == facilityId);

        if (member == null || facility == null)
        {
            return new MemberFacilityAccessDto
            {
                CanAccess = false,
                ReasonsDenied = new[] { "Member or facility not found" }
            };
        }

        var reasons = new List<string>();
        var missingCertifications = new List<string>();

        // Check membership tier access
        if (facility.AllowedMembershipTiers?.Count > 0 && 
            !facility.AllowedMembershipTiers.Contains(member.Tier))
        {
            reasons.Add($"Requires {string.Join(" or ", facility.AllowedMembershipTiers)} membership tier");
        }

        // Check membership status
        if (member.Status != MembershipStatus.Active)
        {
            reasons.Add("Membership is not active");
        }

        // Check membership expiry
        if (member.MembershipExpiresAt.HasValue && member.MembershipExpiresAt.Value < DateTime.UtcNow)
        {
            reasons.Add("Membership has expired");
        }

        // Check required certifications
        if (facility.RequiredCertifications?.Count > 0)
        {
            var memberCertifications = await _context.MemberFacilityCertifications
                .Where(c => c.MemberId == memberId && c.IsActive)
                .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
                .Select(c => c.CertificationType)
                .ToListAsync();

            var missing = facility.RequiredCertifications
                .Where(cert => !memberCertifications.Contains(cert))
                .ToList();

            if (missing.Count > 0)
            {
                missingCertifications.AddRange(missing);
                reasons.Add($"Missing required certifications: {string.Join(", ", missing)}");
            }
        }

        // Check facility status
        if (facility.Status != FacilityStatus.Available)
        {
            reasons.Add("Facility is currently unavailable");
        }

        return new MemberFacilityAccessDto
        {
            CanAccess = reasons.Count == 0,
            ReasonsDenied = reasons.ToArray(),
            MissingCertifications = missingCertifications,
            RequiredCertifications = facility.RequiredCertifications?.ToArray() ?? Array.Empty<string>(),
            MembershipTierAllowed = facility.AllowedMembershipTiers?.Contains(member.Tier) ?? true,
            CertificationsMet = missingCertifications.Count == 0
        };
    }

    public async Task<MemberFacilityAccessDto> CheckMemberAccessAsync(ClubManagementDbContext tenantContext, Guid facilityId, Guid memberId)
    {
        var member = await tenantContext.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        var facility = await tenantContext.Facilities
            .Include(f => f.FacilityType)
            .FirstOrDefaultAsync(f => f.Id == facilityId);

        if (member == null || facility == null)
        {
            return new MemberFacilityAccessDto
            {
                CanAccess = false,
                ReasonsDenied = new[] { "Member or facility not found" }
            };
        }

        var reasons = new List<string>();
        var missingCertifications = new List<string>();

        // Check membership tier access
        if (facility.AllowedMembershipTiers?.Count > 0 && 
            !facility.AllowedMembershipTiers.Contains(member.Tier))
        {
            reasons.Add($"Requires {string.Join(" or ", facility.AllowedMembershipTiers)} membership tier");
        }

        // Check membership status
        if (member.Status != MembershipStatus.Active)
        {
            reasons.Add("Membership is not active");
        }

        // Check membership expiry
        if (member.MembershipExpiresAt.HasValue && member.MembershipExpiresAt.Value < DateTime.UtcNow)
        {
            reasons.Add("Membership has expired");
        }

        // Check required certifications
        if (facility.RequiredCertifications?.Count > 0)
        {
            var memberCertifications = await tenantContext.MemberFacilityCertifications
                .Where(c => c.MemberId == memberId && c.IsActive)
                .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
                .Select(c => c.CertificationType)
                .ToListAsync();

            var missing = facility.RequiredCertifications
                .Where(cert => !memberCertifications.Contains(cert))
                .ToList();

            if (missing.Count > 0)
            {
                missingCertifications.AddRange(missing);
                reasons.Add($"Missing required certifications: {string.Join(", ", missing)}");
            }
        }

        // Check facility status
        if (facility.Status != FacilityStatus.Available)
        {
            reasons.Add("Facility is currently unavailable");
        }

        return new MemberFacilityAccessDto
        {
            CanAccess = reasons.Count == 0,
            ReasonsDenied = reasons.ToArray(),
            MissingCertifications = missingCertifications,
            RequiredCertifications = facility.RequiredCertifications?.ToArray() ?? Array.Empty<string>(),
            MembershipTierAllowed = facility.AllowedMembershipTiers?.Contains(member.Tier) ?? true,
            CertificationsMet = missingCertifications.Count == 0
        };
    }

    public async Task<bool> ValidateMembershipTierAccessAsync(Guid facilityId, MembershipTier memberTier)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        
        if (facility?.AllowedMembershipTiers?.Count > 0)
        {
            return facility.AllowedMembershipTiers.Contains(memberTier);
        }
        
        return true; // No tier restrictions
    }

    public async Task<List<string>> GetMissingCertificationsAsync(Guid facilityId, Guid memberId)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility?.RequiredCertifications?.Count == 0)
            return new List<string>();

        var memberCertifications = await _context.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToListAsync();

        return facility!.RequiredCertifications!
            .Where(cert => !memberCertifications.Contains(cert))
            .ToList();
    }

    public async Task<bool> ValidateMembershipTierAccessAsync(ClubManagementDbContext tenantContext, Guid facilityId, MembershipTier memberTier)
    {
        var facility = await tenantContext.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        
        if (facility?.AllowedMembershipTiers?.Count > 0)
        {
            return facility.AllowedMembershipTiers.Contains(memberTier);
        }
        
        return true; // No tier restrictions
    }

    public async Task<List<string>> GetMissingCertificationsAsync(ClubManagementDbContext tenantContext, Guid facilityId, Guid memberId)
    {
        var facility = await tenantContext.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility?.RequiredCertifications?.Count == 0)
            return new List<string>();

        var memberCertifications = await tenantContext.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToListAsync();

        return facility!.RequiredCertifications!
            .Where(cert => !memberCertifications.Contains(cert))
            .ToList();
    }

    public async Task<BookingLimitValidationResult> ValidateBookingLimitsAsync(
        Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null)
        {
            return new BookingLimitValidationResult
            {
                IsValid = false,
                Violations = new List<string> { "Member not found" }
            };
        }

        // Get applicable booking limits (most specific first)
        var limits = await GetApplicableBookingLimitsAsync(memberId, facilityId);
        var applicableLimit = limits.FirstOrDefault();

        if (applicableLimit == null)
        {
            // Use default tier limits
            applicableLimit = await GetDefaultTierLimitsAsync(member.Tier);
        }

        var usage = await GetMemberBookingUsageAsync(memberId);
        var violations = new List<string>();
        var warnings = new List<string>();

        // Validate booking duration
        var duration = endTime - startTime;
        if (duration.TotalHours > applicableLimit.MaxBookingDurationHours)
        {
            violations.Add($"Booking duration ({duration.TotalHours:F1}h) exceeds maximum allowed ({applicableLimit.MaxBookingDurationHours}h)");
        }

        // Validate advance booking time
        var advanceTime = startTime - DateTime.UtcNow;
        if (advanceTime.TotalDays > applicableLimit.MaxAdvanceBookingDays)
        {
            violations.Add($"Cannot book more than {applicableLimit.MaxAdvanceBookingDays} days in advance");
        }

        if (advanceTime.TotalHours < applicableLimit.MinAdvanceBookingHours)
        {
            violations.Add($"Must book at least {applicableLimit.MinAdvanceBookingHours} hours in advance");
        }

        // Validate time restrictions
        if (applicableLimit.EarliestBookingTime.HasValue && 
            startTime.TimeOfDay < applicableLimit.EarliestBookingTime.Value)
        {
            violations.Add($"Cannot book before {applicableLimit.EarliestBookingTime.Value:hh\\:mm}");
        }

        if (applicableLimit.LatestBookingTime.HasValue && 
            endTime.TimeOfDay > applicableLimit.LatestBookingTime.Value)
        {
            violations.Add($"Cannot book after {applicableLimit.LatestBookingTime.Value:hh\\:mm}");
        }

        // Validate allowed days
        if (applicableLimit.AllowedDays?.Length > 0 && 
            !applicableLimit.AllowedDays.Contains(startTime.DayOfWeek))
        {
            violations.Add($"Bookings not allowed on {startTime.DayOfWeek}");
        }

        // Count current bookings excluding the one being updated
        var currentBookings = await GetMemberBookingsForValidationAsync(memberId, startTime, excludeBookingId);

        // Validate booking limits
        if (currentBookings.BookingsToday >= applicableLimit.MaxBookingsPerDay)
        {
            violations.Add($"Daily booking limit ({applicableLimit.MaxBookingsPerDay}) already reached");
        }

        if (currentBookings.BookingsThisWeek >= applicableLimit.MaxBookingsPerWeek)
        {
            violations.Add($"Weekly booking limit ({applicableLimit.MaxBookingsPerWeek}) already reached");
        }

        if (currentBookings.BookingsThisMonth >= applicableLimit.MaxBookingsPerMonth)
        {
            violations.Add($"Monthly booking limit ({applicableLimit.MaxBookingsPerMonth}) already reached");
        }

        if (currentBookings.ConcurrentBookings >= applicableLimit.MaxConcurrentBookings)
        {
            violations.Add($"Concurrent booking limit ({applicableLimit.MaxConcurrentBookings}) already reached");
        }

        // Add warnings for approaching limits
        if (currentBookings.BookingsToday >= applicableLimit.MaxBookingsPerDay * 0.8)
        {
            warnings.Add("Approaching daily booking limit");
        }

        return new BookingLimitValidationResult
        {
            IsValid = violations.Count == 0,
            Violations = violations,
            Warnings = warnings,
            ApplicableLimit = applicableLimit,
            CurrentUsage = usage,
            RemainingBookingsToday = Math.Max(0, applicableLimit.MaxBookingsPerDay - currentBookings.BookingsToday),
            RemainingBookingsThisWeek = Math.Max(0, applicableLimit.MaxBookingsPerWeek - currentBookings.BookingsThisWeek),
            RemainingBookingsThisMonth = Math.Max(0, applicableLimit.MaxBookingsPerMonth - currentBookings.BookingsThisMonth),
            RemainingConcurrentSlots = Math.Max(0, applicableLimit.MaxConcurrentBookings - currentBookings.ConcurrentBookings)
        };
    }

    public async Task<BookingLimitValidationResult> ValidateBookingLimitsAsync(
        ClubManagementDbContext tenantContext, Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null)
        {
            return new BookingLimitValidationResult
            {
                IsValid = false,
                Violations = new List<string> { "Member not found" }
            };
        }

        var facility = await tenantContext.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null)
        {
            return new BookingLimitValidationResult
            {
                IsValid = false,
                Violations = new List<string> { "Facility not found" }
            };
        }

        // For now, implement basic validation - the full complex limit system would require
        // migrating all the supporting methods to use tenant context as well
        var violations = new List<string>();
        var warnings = new List<string>();

        // Validate booking duration (basic check)
        var duration = endTime - startTime;
        if (duration.TotalHours > 8) // Basic 8-hour limit
        {
            violations.Add($"Booking duration ({duration.TotalHours:F1}h) exceeds maximum allowed (8h)");
        }

        if (duration.TotalMinutes < 30) // Minimum 30 minutes
        {
            violations.Add("Booking duration must be at least 30 minutes");
        }

        // Check for basic time conflicts using tenant context
        var conflictingBookings = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId)
            .Where(b => b.FacilityId == facilityId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Where(b => b.StartDateTime < endTime && b.EndDateTime > startTime)
            .Where(b => excludeBookingId == null || b.Id != excludeBookingId)
            .CountAsync();

        if (conflictingBookings > 0)
        {
            violations.Add("Member already has a conflicting booking for this time period");
        }

        // Basic daily limit check (count bookings for the same day)
        var bookingsToday = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId)
            .Where(b => b.StartDateTime.Date == startTime.Date)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Where(b => excludeBookingId == null || b.Id != excludeBookingId)
            .CountAsync();

        const int maxBookingsPerDay = 3; // Basic limit
        if (bookingsToday >= maxBookingsPerDay)
        {
            violations.Add($"Daily booking limit ({maxBookingsPerDay}) already reached");
        }

        return new BookingLimitValidationResult
        {
            IsValid = violations.Count == 0,
            Violations = violations,
            Warnings = warnings,
            ApplicableLimit = null, // TODO: Implement proper limits with tenant context
            CurrentUsage = null,    // TODO: Implement usage tracking with tenant context
            RemainingBookingsToday = Math.Max(0, maxBookingsPerDay - bookingsToday),
            RemainingBookingsThisWeek = 0,  // TODO: Implement weekly tracking
            RemainingBookingsThisMonth = 0, // TODO: Implement monthly tracking
            RemainingConcurrentSlots = 1    // TODO: Implement concurrent tracking
        };
    }

    private async Task<List<MemberBookingLimitDto>> GetApplicableBookingLimitsAsync(Guid memberId, Guid facilityId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<MemberBookingLimitDto>();

        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null) return new List<MemberBookingLimitDto>();

        var query = _context.MemberBookingLimits
            .Where(l => l.IsActive && 
                       l.EffectiveFrom <= DateTime.UtcNow && 
                       (!l.EffectiveTo.HasValue || l.EffectiveTo.Value > DateTime.UtcNow))
            .Where(l => l.MemberId == memberId || 
                       (l.ApplicableTier.HasValue && l.ApplicableTier.Value == member.Tier));

        // Order by specificity: Member-specific > Facility-specific > Facility-type-specific > Tier-specific
        return await query
            .OrderByDescending(l => l.MemberId == memberId ? 4 : 0)
            .ThenByDescending(l => l.FacilityId == facilityId ? 3 : 0)
            .ThenByDescending(l => l.FacilityTypeId == facility.FacilityTypeId ? 2 : 0)
            .ThenByDescending(l => l.ApplicableTier.HasValue ? 1 : 0)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                FacilityId = l.FacilityId,
                FacilityTypeId = l.FacilityTypeId,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours
            })
            .ToListAsync();
    }

    private async Task<MemberBookingUsage> GetMemberBookingsForValidationAsync(Guid memberId, DateTime referenceDate, Guid? excludeBookingId = null)
    {
        var startOfDay = referenceDate.Date;
        var startOfWeek = startOfDay.AddDays(-(int)startOfDay.DayOfWeek);
        var startOfMonth = new DateTime(startOfDay.Year, startOfDay.Month, 1);
        var now = DateTime.UtcNow;

        var bookingsQuery = _context.FacilityBookings
            .Where(b => b.MemberId == memberId && 
                       b.Status != BookingStatus.Cancelled &&
                       (!excludeBookingId.HasValue || b.Id != excludeBookingId.Value));

        var bookingsToday = await bookingsQuery
            .CountAsync(b => b.StartDateTime.Date == startOfDay);

        var bookingsThisWeek = await bookingsQuery
            .CountAsync(b => b.StartDateTime.Date >= startOfWeek && b.StartDateTime.Date < startOfWeek.AddDays(7));

        var bookingsThisMonth = await bookingsQuery
            .CountAsync(b => b.StartDateTime.Date >= startOfMonth && b.StartDateTime.Date < startOfMonth.AddMonths(1));

        var concurrentBookings = await bookingsQuery
            .CountAsync(b => b.StartDateTime <= now && b.EndDateTime > now);

        return new MemberBookingUsage
        {
            MemberId = memberId,
            Date = referenceDate,
            BookingsToday = bookingsToday,
            BookingsThisWeek = bookingsThisWeek,
            BookingsThisMonth = bookingsThisMonth,
            ConcurrentBookings = concurrentBookings,
            LastCalculated = DateTime.UtcNow
        };
    }

    public async Task<List<MemberBookingLimitDto>> GetMemberBookingLimitsAsync(Guid memberId)
    {
        return await _context.MemberBookingLimits
            .Where(l => l.MemberId == memberId && l.IsActive)
            .Include(l => l.Facility)
            .Include(l => l.FacilityType)
            .Include(l => l.Member)
            .ThenInclude(m => m.User)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                MemberName = l.Member.User.FirstName + " " + l.Member.User.LastName,
                FacilityId = l.FacilityId,
                FacilityName = l.Facility != null ? l.Facility.Name : null,
                FacilityTypeId = l.FacilityTypeId,
                FacilityTypeName = l.FacilityType != null ? l.FacilityType.Name : null,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours,
                EffectiveFrom = l.EffectiveFrom,
                EffectiveTo = l.EffectiveTo,
                IsActive = l.IsActive,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt ?? l.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<MemberBookingLimitDto>> GetMemberBookingLimitsAsync(ClubManagementDbContext tenantContext, Guid memberId)
    {
        return await tenantContext.MemberBookingLimits
            .Where(l => l.MemberId == memberId && l.IsActive)
            .Include(l => l.Facility)
            .Include(l => l.FacilityType)
            .Include(l => l.Member)
            .ThenInclude(m => m.User)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                MemberName = l.Member.User.FirstName + " " + l.Member.User.LastName,
                FacilityId = l.FacilityId,
                FacilityName = l.Facility != null ? l.Facility.Name : null,
                FacilityTypeId = l.FacilityTypeId,
                FacilityTypeName = l.FacilityType != null ? l.FacilityType.Name : null,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours,
                EffectiveFrom = l.EffectiveFrom,
                EffectiveTo = l.EffectiveTo,
                IsActive = l.IsActive,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt ?? l.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MemberBookingUsageDto> GetMemberBookingUsageAsync(Guid memberId, DateTime? date = null)
    {
        var referenceDate = date ?? DateTime.UtcNow;
        var usage = await GetMemberBookingsForValidationAsync(memberId, referenceDate);
        var member = await _context.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == memberId);

        return new MemberBookingUsageDto
        {
            MemberId = memberId,
            MemberName = member != null ? $"{member.User.FirstName} {member.User.LastName}" : "Unknown",
            Date = referenceDate,
            BookingsToday = usage.BookingsToday,
            BookingsThisWeek = usage.BookingsThisWeek,
            BookingsThisMonth = usage.BookingsThisMonth,
            ConcurrentBookings = usage.ConcurrentBookings,
            TotalHoursBooked = usage.TotalHoursBooked,
            LastCalculated = usage.LastCalculated
        };
    }

    public async Task<MemberBookingUsageDto> GetMemberBookingUsageAsync(ClubManagementDbContext tenantContext, Guid memberId, DateTime? date = null)
    {
        var referenceDate = date ?? DateTime.UtcNow;
        var usage = await GetMemberBookingsForValidationAsync(tenantContext, memberId, referenceDate);
        var member = await tenantContext.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == memberId);

        return new MemberBookingUsageDto
        {
            MemberId = memberId,
            MemberName = member != null ? $"{member.User.FirstName} {member.User.LastName}" : "Unknown",
            Date = referenceDate,
            BookingsToday = usage.BookingsToday,
            BookingsThisWeek = usage.BookingsThisWeek,
            BookingsThisMonth = usage.BookingsThisMonth,
            ConcurrentBookings = usage.ConcurrentBookings,
            TotalHoursBooked = usage.TotalHoursBooked,
            LastCalculated = usage.LastCalculated
        };
    }

    private async Task<MemberBookingUsage> GetMemberBookingsForValidationAsync(ClubManagementDbContext tenantContext, Guid memberId, DateTime referenceDate, Guid? excludeBookingId = null)
    {
        var startOfDay = referenceDate.Date;
        var startOfWeek = startOfDay.AddDays(-(int)startOfDay.DayOfWeek);
        var startOfMonth = new DateTime(startOfDay.Year, startOfDay.Month, 1);
        var now = DateTime.UtcNow;

        var bookingsQuery = tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && 
                       b.Status != BookingStatus.Cancelled &&
                       (!excludeBookingId.HasValue || b.Id != excludeBookingId.Value));

        var bookingsToday = await bookingsQuery
            .CountAsync(b => b.StartDateTime.Date == startOfDay);

        var bookingsThisWeek = await bookingsQuery
            .CountAsync(b => b.StartDateTime.Date >= startOfWeek && b.StartDateTime.Date < startOfWeek.AddDays(7));

        var bookingsThisMonth = await bookingsQuery
            .CountAsync(b => b.StartDateTime.Date >= startOfMonth && b.StartDateTime.Date < startOfMonth.AddMonths(1));

        var concurrentBookings = await bookingsQuery
            .CountAsync(b => b.StartDateTime <= now && b.EndDateTime > now);

        return new MemberBookingUsage
        {
            MemberId = memberId,
            Date = referenceDate,
            BookingsToday = bookingsToday,
            BookingsThisWeek = bookingsThisWeek,
            BookingsThisMonth = bookingsThisMonth,
            ConcurrentBookings = concurrentBookings,
            LastCalculated = DateTime.UtcNow
        };
    }

    public async Task<MemberBookingLimitDto> GetDefaultTierLimitsAsync(MembershipTier tier)
    {
        // Return default limits based on tier
        return tier switch
        {
            MembershipTier.Basic => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 2,
                MaxBookingsPerDay = 1,
                MaxBookingsPerWeek = 3,
                MaxBookingsPerMonth = 10,
                MaxBookingDurationHours = 2,
                MaxAdvanceBookingDays = 14,
                MinAdvanceBookingHours = 4,
                EarliestBookingTime = new TimeSpan(6, 0, 0),
                LatestBookingTime = new TimeSpan(22, 0, 0),
                RequiresApproval = false,
                AllowRecurringBookings = false,
                CancellationPenaltyHours = 24
            },
            MembershipTier.Premium => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 3,
                MaxBookingsPerDay = 2,
                MaxBookingsPerWeek = 5,
                MaxBookingsPerMonth = 20,
                MaxBookingDurationHours = 4,
                MaxAdvanceBookingDays = 30,
                MinAdvanceBookingHours = 2,
                EarliestBookingTime = new TimeSpan(5, 0, 0),
                LatestBookingTime = new TimeSpan(23, 0, 0),
                RequiresApproval = false,
                AllowRecurringBookings = true,
                CancellationPenaltyHours = 12
            },
            MembershipTier.VIP => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 5,
                MaxBookingsPerDay = 3,
                MaxBookingsPerWeek = 10,
                MaxBookingsPerMonth = 40,
                MaxBookingDurationHours = 8,
                MaxAdvanceBookingDays = 60,
                MinAdvanceBookingHours = 1,
                EarliestBookingTime = new TimeSpan(0, 0, 0),
                LatestBookingTime = new TimeSpan(23, 59, 59),
                RequiresApproval = false,
                AllowRecurringBookings = true,
                CancellationPenaltyHours = 6
            },
            MembershipTier.Family => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 4,
                MaxBookingsPerDay = 2,
                MaxBookingsPerWeek = 7,
                MaxBookingsPerMonth = 25,
                MaxBookingDurationHours = 6,
                MaxAdvanceBookingDays = 30,
                MinAdvanceBookingHours = 2,
                EarliestBookingTime = new TimeSpan(6, 0, 0),
                LatestBookingTime = new TimeSpan(22, 0, 0),
                RequiresApproval = false,
                AllowRecurringBookings = true,
                CancellationPenaltyHours = 24
            },
            _ => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 1,
                MaxBookingsPerDay = 1,
                MaxBookingsPerWeek = 2,
                MaxBookingsPerMonth = 5,
                MaxBookingDurationHours = 1,
                MaxAdvanceBookingDays = 7,
                MinAdvanceBookingHours = 24,
                RequiresApproval = true,
                AllowRecurringBookings = false,
                CancellationPenaltyHours = 48
            }
        };
    }

    public async Task<MemberBookingLimitDto> GetDefaultTierLimitsAsync(ClubManagementDbContext tenantContext, MembershipTier tier)
    {
        // This method doesn't need tenant context for logic, but keeping consistent interface
        await Task.CompletedTask; // Satisfy async requirement
        
        // Return default limits based on tier (same logic as non-tenant version)
        return tier switch
        {
            MembershipTier.Basic => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 2,
                MaxBookingsPerDay = 1,
                MaxBookingsPerWeek = 3,
                MaxBookingsPerMonth = 10,
                MaxBookingDurationHours = 2,
                MaxAdvanceBookingDays = 14,
                MinAdvanceBookingHours = 4,
                EarliestBookingTime = new TimeSpan(6, 0, 0),
                LatestBookingTime = new TimeSpan(22, 0, 0),
                RequiresApproval = false,
                AllowRecurringBookings = false,
                CancellationPenaltyHours = 24
            },
            MembershipTier.Premium => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 3,
                MaxBookingsPerDay = 2,
                MaxBookingsPerWeek = 5,
                MaxBookingsPerMonth = 20,
                MaxBookingDurationHours = 4,
                MaxAdvanceBookingDays = 30,
                MinAdvanceBookingHours = 2,
                EarliestBookingTime = new TimeSpan(5, 0, 0),
                LatestBookingTime = new TimeSpan(23, 0, 0),
                RequiresApproval = false,
                AllowRecurringBookings = true,
                CancellationPenaltyHours = 12
            },
            MembershipTier.VIP => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 5,
                MaxBookingsPerDay = 3,
                MaxBookingsPerWeek = 10,
                MaxBookingsPerMonth = 40,
                MaxBookingDurationHours = 8,
                MaxAdvanceBookingDays = 60,
                MinAdvanceBookingHours = 1,
                EarliestBookingTime = new TimeSpan(0, 0, 0),
                LatestBookingTime = new TimeSpan(23, 59, 59),
                RequiresApproval = false,
                AllowRecurringBookings = true,
                CancellationPenaltyHours = 6
            },
            MembershipTier.Family => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 4,
                MaxBookingsPerDay = 2,
                MaxBookingsPerWeek = 7,
                MaxBookingsPerMonth = 25,
                MaxBookingDurationHours = 6,
                MaxAdvanceBookingDays = 30,
                MinAdvanceBookingHours = 2,
                EarliestBookingTime = new TimeSpan(6, 0, 0),
                LatestBookingTime = new TimeSpan(22, 0, 0),
                RequiresApproval = false,
                AllowRecurringBookings = true,
                CancellationPenaltyHours = 24
            },
            _ => new MemberBookingLimitDto
            {
                MaxConcurrentBookings = 1,
                MaxBookingsPerDay = 1,
                MaxBookingsPerWeek = 2,
                MaxBookingsPerMonth = 5,
                MaxBookingDurationHours = 1,
                MaxAdvanceBookingDays = 7,
                MinAdvanceBookingHours = 24,
                RequiresApproval = true,
                AllowRecurringBookings = false,
                CancellationPenaltyHours = 48
            }
        };
    }

    public async Task<List<FacilityCertificationDto>> GetMemberCertificationsAsync(ClubManagementDbContext tenantContext, Guid memberId)
    {
        return await tenantContext.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId)
            .Include(c => c.Member)
                .ThenInclude(m => m.User)
            .Include(c => c.CertifiedByUser)
            .OrderByDescending(c => c.CertifiedDate)
            .Select(c => new FacilityCertificationDto
            {
                Id = c.Id,
                MemberId = c.MemberId,
                MemberName = c.Member.User.FirstName + " " + c.Member.User.LastName,
                CertificationType = c.CertificationType,
                CertifiedDate = c.CertifiedDate,
                ExpiryDate = c.ExpiryDate,
                CertifiedByUserId = c.CertifiedByUserId,
                CertifiedByUserName = c.CertifiedByUser.FirstName + " " + c.CertifiedByUser.LastName,
                IsActive = c.IsActive,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<FacilityCertificationDto> CreateCertificationAsync(ClubManagementDbContext tenantContext, CreateCertificationRequest request)
    {
        var certification = new MemberFacilityCertification
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            CertificationType = request.CertificationType,
            CertifiedDate = DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate,
            CertifiedByUserId = Guid.NewGuid(), // This should come from current user context
            IsActive = true,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // This should come from current user context
        };

        tenantContext.MemberFacilityCertifications.Add(certification);
        await tenantContext.SaveChangesAsync();

        return await tenantContext.MemberFacilityCertifications
            .Where(c => c.Id == certification.Id)
            .Include(c => c.Member)
                .ThenInclude(m => m.User)
            .Include(c => c.CertifiedByUser)
            .Select(c => new FacilityCertificationDto
            {
                Id = c.Id,
                MemberId = c.MemberId,
                MemberName = c.Member.User.FirstName + " " + c.Member.User.LastName,
                CertificationType = c.CertificationType,
                CertifiedDate = c.CertifiedDate,
                ExpiryDate = c.ExpiryDate,
                CertifiedByUserId = c.CertifiedByUserId,
                CertifiedByUserName = c.CertifiedByUser.FirstName + " " + c.CertifiedByUser.LastName,
                IsActive = c.IsActive,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt
            })
            .FirstAsync();
    }

    public async Task<bool> IsCertificationValidAsync(ClubManagementDbContext tenantContext, Guid memberId, string certificationType)
    {
        return await tenantContext.MemberFacilityCertifications
            .AnyAsync(c => c.MemberId == memberId && 
                          c.CertificationType == certificationType && 
                          c.IsActive &&
                          (!c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow));
    }

    public async Task<List<FacilityCertificationDto>> GetExpiringCertificationsAsync(ClubManagementDbContext tenantContext, int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await tenantContext.MemberFacilityCertifications
            .Where(c => c.IsActive && 
                       c.ExpiryDate.HasValue && 
                       c.ExpiryDate.Value <= cutoffDate &&
                       c.ExpiryDate.Value > DateTime.UtcNow)
            .Include(c => c.Member)
                .ThenInclude(m => m.User)
            .Include(c => c.CertifiedByUser)
            .OrderBy(c => c.ExpiryDate)
            .Select(c => new FacilityCertificationDto
            {
                Id = c.Id,
                MemberId = c.MemberId,
                MemberName = c.Member.User.FirstName + " " + c.Member.User.LastName,
                CertificationType = c.CertificationType,
                CertifiedDate = c.CertifiedDate,
                ExpiryDate = c.ExpiryDate,
                CertifiedByUserId = c.CertifiedByUserId,
                CertifiedByUserName = c.CertifiedByUser.FirstName + " " + c.CertifiedByUser.LastName,
                IsActive = c.IsActive,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<FacilityDto>> GetAccessibleFacilitiesAsync(Guid memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<FacilityDto>();

        var memberCertifications = await _context.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToListAsync();

        return await _context.Facilities
            .Where(f => f.Status == FacilityStatus.Available)
            .Where(f => !f.AllowedMembershipTiers.Any() || f.AllowedMembershipTiers.Contains(member.Tier))
            .Where(f => !f.RequiredCertifications.Any() || 
                       f.RequiredCertifications.All(cert => memberCertifications.Contains(cert)))
            .Include(f => f.FacilityType)
            .Select(f => new FacilityDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                FacilityTypeId = f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                FacilityTypeIcon = f.FacilityType.Icon,
                Icon = f.Icon,
                Status = f.Status,
                Capacity = f.Capacity,
                Location = f.Location,
                MemberHourlyRate = f.MemberHourlyRate,
                RequiredCertifications = f.RequiredCertifications.ToList(),
                AllowedMembershipTiers = f.AllowedMembershipTiers.ToList(),
                CanMemberAccess = true
            })
            .ToListAsync();
    }

    public async Task<List<FacilityDto>> GetRestrictedFacilitiesAsync(Guid memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<FacilityDto>();

        var memberCertifications = await _context.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToListAsync();

        return await _context.Facilities
            .Where(f => (f.AllowedMembershipTiers.Any() && !f.AllowedMembershipTiers.Contains(member.Tier)) ||
                       (f.RequiredCertifications.Any() && !f.RequiredCertifications.All(cert => memberCertifications.Contains(cert))))
            .Include(f => f.FacilityType)
            .Select(f => new FacilityDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                FacilityTypeId = f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                FacilityTypeIcon = f.FacilityType.Icon,
                Icon = f.Icon,
                Status = f.Status,
                Capacity = f.Capacity,
                Location = f.Location,
                RequiredCertifications = f.RequiredCertifications.ToList(),
                AllowedMembershipTiers = f.AllowedMembershipTiers.ToList(),
                CanMemberAccess = false,
                MissingCertifications = f.RequiredCertifications
                    .Where(cert => !memberCertifications.Contains(cert))
                    .ToList()
            })
            .ToListAsync();
    }

    // Stub implementations for remaining methods - would be fully implemented in production
    public async Task<MemberBookingLimitDto> CreateBookingLimitAsync(CreateMemberBookingLimitRequest request)
    {
        var limit = new MemberBookingLimit
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            FacilityId = request.FacilityId,
            FacilityTypeId = request.FacilityTypeId,
            ApplicableTier = request.ApplicableTier,
            MaxConcurrentBookings = request.MaxConcurrentBookings,
            MaxBookingsPerDay = request.MaxBookingsPerDay,
            MaxBookingsPerWeek = request.MaxBookingsPerWeek,
            MaxBookingsPerMonth = request.MaxBookingsPerMonth,
            MaxBookingDurationHours = request.MaxBookingDurationHours,
            MaxAdvanceBookingDays = request.MaxAdvanceBookingDays,
            MinAdvanceBookingHours = request.MinAdvanceBookingHours,
            EarliestBookingTime = request.EarliestBookingTime,
            LatestBookingTime = request.LatestBookingTime,
            AllowedDays = request.AllowedDays,
            RequiresApproval = request.RequiresApproval,
            AllowRecurringBookings = request.AllowRecurringBookings,
            CancellationPenaltyHours = request.CancellationPenaltyHours,
            EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.MemberBookingLimits.Add(limit);
        await _context.SaveChangesAsync();

        return await _context.MemberBookingLimits
            .Where(l => l.Id == limit.Id)
            .Include(l => l.Member)
                .ThenInclude(m => m.User)
            .Include(l => l.Facility)
            .Include(l => l.FacilityType)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                MemberName = l.Member != null ? l.Member.User.FirstName + " " + l.Member.User.LastName : null,
                FacilityId = l.FacilityId,
                FacilityName = l.Facility != null ? l.Facility.Name : null,
                FacilityTypeId = l.FacilityTypeId,
                FacilityTypeName = l.FacilityType != null ? l.FacilityType.Name : null,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours,
                EffectiveFrom = l.EffectiveFrom,
                EffectiveTo = l.EffectiveTo,
                IsActive = l.IsActive,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt ?? l.CreatedAt
            })
            .FirstAsync();
    }

    public async Task<MemberBookingLimitDto> UpdateBookingLimitAsync(Guid limitId, UpdateMemberBookingLimitRequest request)
    {
        var limit = await _context.MemberBookingLimits.FirstOrDefaultAsync(l => l.Id == limitId);
        if (limit == null)
            throw new InvalidOperationException("Booking limit not found");

        limit.MaxConcurrentBookings = request.MaxConcurrentBookings;
        limit.MaxBookingsPerDay = request.MaxBookingsPerDay;
        limit.MaxBookingsPerWeek = request.MaxBookingsPerWeek;
        limit.MaxBookingsPerMonth = request.MaxBookingsPerMonth;
        limit.MaxBookingDurationHours = request.MaxBookingDurationHours;
        limit.MaxAdvanceBookingDays = request.MaxAdvanceBookingDays;
        limit.MinAdvanceBookingHours = request.MinAdvanceBookingHours;
        limit.EarliestBookingTime = request.EarliestBookingTime;
        limit.LatestBookingTime = request.LatestBookingTime;
        limit.AllowedDays = request.AllowedDays;
        limit.RequiresApproval = request.RequiresApproval;
        limit.AllowRecurringBookings = request.AllowRecurringBookings;
        limit.CancellationPenaltyHours = request.CancellationPenaltyHours;
        limit.EffectiveTo = request.EffectiveTo;
        limit.Notes = request.Notes;
        limit.UpdatedAt = DateTime.UtcNow;
        limit.UpdatedBy = "System";

        await _context.SaveChangesAsync();

        return await _context.MemberBookingLimits
            .Where(l => l.Id == limitId)
            .Include(l => l.Member)
                .ThenInclude(m => m.User)
            .Include(l => l.Facility)
            .Include(l => l.FacilityType)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                MemberName = l.Member != null ? l.Member.User.FirstName + " " + l.Member.User.LastName : null,
                FacilityId = l.FacilityId,
                FacilityName = l.Facility != null ? l.Facility.Name : null,
                FacilityTypeId = l.FacilityTypeId,
                FacilityTypeName = l.FacilityType != null ? l.FacilityType.Name : null,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours,
                EffectiveFrom = l.EffectiveFrom,
                EffectiveTo = l.EffectiveTo,
                IsActive = l.IsActive,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt ?? l.CreatedAt
            })
            .FirstAsync();
    }

    public async Task DeleteBookingLimitAsync(Guid limitId)
    {
        var limit = await _context.MemberBookingLimits.FirstOrDefaultAsync(l => l.Id == limitId);
        if (limit == null)
            throw new InvalidOperationException("Booking limit not found");

        // Soft delete by marking as inactive
        limit.IsActive = false;
        limit.UpdatedAt = DateTime.UtcNow;
        limit.UpdatedBy = "System";

        await _context.SaveChangesAsync();
    }

    public async Task ApplyDefaultTierLimitsAsync(Guid memberId, MembershipTier tier)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null)
            throw new InvalidOperationException("Member not found");

        var defaultLimits = await GetDefaultTierLimitsAsync(tier);

        var limit = new MemberBookingLimit
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            ApplicableTier = tier,
            MaxConcurrentBookings = defaultLimits.MaxConcurrentBookings,
            MaxBookingsPerDay = defaultLimits.MaxBookingsPerDay,
            MaxBookingsPerWeek = defaultLimits.MaxBookingsPerWeek,
            MaxBookingsPerMonth = defaultLimits.MaxBookingsPerMonth,
            MaxBookingDurationHours = defaultLimits.MaxBookingDurationHours,
            MaxAdvanceBookingDays = defaultLimits.MaxAdvanceBookingDays,
            MinAdvanceBookingHours = defaultLimits.MinAdvanceBookingHours,
            EarliestBookingTime = defaultLimits.EarliestBookingTime,
            LatestBookingTime = defaultLimits.LatestBookingTime,
            RequiresApproval = defaultLimits.RequiresApproval,
            AllowRecurringBookings = defaultLimits.AllowRecurringBookings,
            CancellationPenaltyHours = defaultLimits.CancellationPenaltyHours,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            Notes = $"Default {tier} tier limits applied",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.MemberBookingLimits.Add(limit);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMemberUsageAsync(Guid memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null)
            throw new InvalidOperationException("Member not found");

        var today = DateTime.UtcNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        var bookingsToday = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date == today)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();

        var bookingsThisWeek = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date >= thisWeekStart)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();

        var bookingsThisMonth = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date >= thisMonthStart)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();

        var concurrentBookings = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime <= DateTime.UtcNow && b.EndDateTime > DateTime.UtcNow)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .CountAsync();

        // In a full implementation, this would update a MemberUsageCache table
        await Task.CompletedTask;
    }

    public async Task<List<MemberBookingUsageDto>> GetTierUsageStatsAsync(MembershipTier? tier = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var periodStart = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var periodEnd = endDate ?? DateTime.UtcNow;

        var query = _context.Members.Include(m => m.User).AsQueryable();
        if (tier.HasValue)
            query = query.Where(m => m.Tier == tier.Value);

        var members = await query.ToListAsync();
        var usageStats = new List<MemberBookingUsageDto>();

        foreach (var member in members)
        {
            var bookings = await _context.FacilityBookings
                .Where(b => b.MemberId == member.Id)
                .Where(b => b.StartDateTime >= periodStart && b.StartDateTime <= periodEnd)
                .Where(b => b.Status != BookingStatus.Cancelled)
                .ToListAsync();

            var completedBookings = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();

            usageStats.Add(new MemberBookingUsageDto
            {
                MemberId = member.Id,
                MemberName = $"{member.User.FirstName} {member.User.LastName}",
                Date = periodEnd,
                BookingsToday = bookings.Count(b => b.StartDateTime.Date == DateTime.UtcNow.Date),
                BookingsThisWeek = bookings.Count(b => b.StartDateTime.Date >= DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek)),
                BookingsThisMonth = bookings.Count(b => b.StartDateTime.Date >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)),
                ConcurrentBookings = bookings.Count(b => b.StartDateTime <= DateTime.UtcNow && b.EndDateTime > DateTime.UtcNow && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)),
                TotalHoursBooked = (decimal)completedBookings.Sum(b => (b.EndDateTime - b.StartDateTime).TotalHours),
                LastCalculated = DateTime.UtcNow
            });
        }

        return usageStats.OrderByDescending(u => u.TotalHoursBooked).ToList();
    }

    public async Task<List<DateTime>> GetAvailableBookingTimesAsync(Guid facilityId, Guid memberId, DateTime date, int durationMinutes = 60)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        
        if (facility == null || member == null)
            return new List<DateTime>();

        // Check member access to facility first
        var accessResult = await CheckMemberAccessAsync(facilityId, memberId);
        if (!accessResult.CanAccess)
            return new List<DateTime>();

        var limits = await GetDefaultTierLimitsAsync(member.Tier);
        var availableTimes = new List<DateTime>();
        var targetDate = date.Date;
        var duration = TimeSpan.FromMinutes(durationMinutes);

        var earliestTime = limits.EarliestBookingTime ?? new TimeSpan(6, 0, 0);
        var latestTime = limits.LatestBookingTime ?? new TimeSpan(22, 0, 0);

        var existingBookings = await _context.FacilityBookings
            .Where(b => b.FacilityId == facilityId)
            .Where(b => b.StartDateTime.Date == targetDate)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Select(b => new { b.StartDateTime, b.EndDateTime })
            .ToListAsync();

        var currentTime = targetDate.Add(earliestTime);
        var endOfDay = targetDate.Add(latestTime);

        while (currentTime.Add(duration) <= endOfDay)
        {
            var proposedEndTime = currentTime.Add(duration);

            var hasConflict = existingBookings.Any(b => 
                currentTime < b.EndDateTime && proposedEndTime > b.StartDateTime);

            if (!hasConflict)
            {
                var limitsResult = await ValidateBookingLimitsAsync(memberId, facilityId, currentTime, proposedEndTime);
                if (limitsResult.IsValid)
                {
                    availableTimes.Add(currentTime);
                }
            }

            currentTime = currentTime.AddMinutes(30);
        }

        return availableTimes;
    }

    public async Task<List<FacilityDto>> RecommendFacilitiesAsync(Guid memberId, DateTime? preferredTime = null, int? durationMinutes = null)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<FacilityDto>();

        var accessibleFacilities = await GetAccessibleFacilitiesAsync(memberId);
        if (!accessibleFacilities.Any())
            return new List<FacilityDto>();

        var bookingHistory = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId && b.Status == BookingStatus.Completed)
            .Include(b => b.Facility)
                .ThenInclude(f => f.FacilityType)
            .OrderByDescending(b => b.StartDateTime)
            .Take(50)
            .ToListAsync();

        var recommendations = new List<(FacilityDto facility, double score)>();

        foreach (var facility in accessibleFacilities)
        {
            double score = 0;

            var bookingCount = bookingHistory.Count(b => b.FacilityId == facility.Id);
            score += bookingCount * 10;

            var facilityTypeBookings = bookingHistory.Count(b => b.Facility.FacilityTypeId == facility.FacilityTypeId);
            score += facilityTypeBookings * 5;

            if (preferredTime.HasValue)
            {
                var availableTimes = await GetAvailableBookingTimesAsync(facility.Id, memberId, preferredTime.Value.Date, durationMinutes ?? 60);
                var timeWindow = TimeSpan.FromHours(2);
                var hasAvailabilityNearPreferredTime = availableTimes.Any(t => 
                    Math.Abs((t - preferredTime.Value).TotalMinutes) <= timeWindow.TotalMinutes);
                    
                if (hasAvailabilityNearPreferredTime)
                    score += 20;
            }

            if (member.Tier == MembershipTier.VIP || member.Tier == MembershipTier.Premium)
            {
                if (facility.AllowedMembershipTiers.Contains(MembershipTier.VIP) || 
                    facility.AllowedMembershipTiers.Contains(MembershipTier.Premium))
                {
                    score += 15;
                }
            }

            if (facility.RequiredCertifications.Any())
            {
                score -= 2;
            }

            recommendations.Add((facility, score));
        }

        return recommendations
            .OrderByDescending(r => r.score)
            .Take(5)
            .Select(r => r.facility)
            .ToList();
    }

    // ============ TENANT CONTEXT VERSIONS - STUB IMPLEMENTATIONS FOR COMPILATION ============

    public async Task<MemberBookingLimitDto> CreateBookingLimitAsync(ClubManagementDbContext tenantContext, CreateMemberBookingLimitRequest request)
    {
        var limit = new MemberBookingLimit
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            FacilityId = request.FacilityId,
            FacilityTypeId = request.FacilityTypeId,
            ApplicableTier = request.ApplicableTier,
            MaxConcurrentBookings = request.MaxConcurrentBookings,
            MaxBookingsPerDay = request.MaxBookingsPerDay,
            MaxBookingsPerWeek = request.MaxBookingsPerWeek,
            MaxBookingsPerMonth = request.MaxBookingsPerMonth,
            MaxBookingDurationHours = request.MaxBookingDurationHours,
            MaxAdvanceBookingDays = request.MaxAdvanceBookingDays,
            MinAdvanceBookingHours = request.MinAdvanceBookingHours,
            EarliestBookingTime = request.EarliestBookingTime,
            LatestBookingTime = request.LatestBookingTime,
            AllowedDays = request.AllowedDays,
            RequiresApproval = request.RequiresApproval,
            AllowRecurringBookings = request.AllowRecurringBookings,
            CancellationPenaltyHours = request.CancellationPenaltyHours,
            EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // Should come from current user context
        };

        tenantContext.MemberBookingLimits.Add(limit);
        await tenantContext.SaveChangesAsync();

        return await tenantContext.MemberBookingLimits
            .Where(l => l.Id == limit.Id)
            .Include(l => l.Member)
                .ThenInclude(m => m.User)
            .Include(l => l.Facility)
            .Include(l => l.FacilityType)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                MemberName = l.Member != null ? l.Member.User.FirstName + " " + l.Member.User.LastName : null,
                FacilityId = l.FacilityId,
                FacilityName = l.Facility != null ? l.Facility.Name : null,
                FacilityTypeId = l.FacilityTypeId,
                FacilityTypeName = l.FacilityType != null ? l.FacilityType.Name : null,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours,
                EffectiveFrom = l.EffectiveFrom,
                EffectiveTo = l.EffectiveTo,
                IsActive = l.IsActive,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt ?? l.CreatedAt
            })
            .FirstAsync();
    }

    public async Task<MemberBookingLimitDto> UpdateBookingLimitAsync(ClubManagementDbContext tenantContext, Guid limitId, UpdateMemberBookingLimitRequest request)
    {
        var limit = await tenantContext.MemberBookingLimits.FirstOrDefaultAsync(l => l.Id == limitId);
        if (limit == null)
            throw new InvalidOperationException("Booking limit not found");

        // Update properties
        limit.MaxConcurrentBookings = request.MaxConcurrentBookings;
        limit.MaxBookingsPerDay = request.MaxBookingsPerDay;
        limit.MaxBookingsPerWeek = request.MaxBookingsPerWeek;
        limit.MaxBookingsPerMonth = request.MaxBookingsPerMonth;
        limit.MaxBookingDurationHours = request.MaxBookingDurationHours;
        limit.MaxAdvanceBookingDays = request.MaxAdvanceBookingDays;
        limit.MinAdvanceBookingHours = request.MinAdvanceBookingHours;
        limit.EarliestBookingTime = request.EarliestBookingTime;
        limit.LatestBookingTime = request.LatestBookingTime;
        limit.AllowedDays = request.AllowedDays;
        limit.RequiresApproval = request.RequiresApproval;
        limit.AllowRecurringBookings = request.AllowRecurringBookings;
        limit.CancellationPenaltyHours = request.CancellationPenaltyHours;
        limit.EffectiveTo = request.EffectiveTo;
        limit.Notes = request.Notes;
        limit.UpdatedAt = DateTime.UtcNow;
        limit.UpdatedBy = "System"; // Should come from current user context

        await tenantContext.SaveChangesAsync();

        return await tenantContext.MemberBookingLimits
            .Where(l => l.Id == limitId)
            .Include(l => l.Member)
                .ThenInclude(m => m.User)
            .Include(l => l.Facility)
            .Include(l => l.FacilityType)
            .Select(l => new MemberBookingLimitDto
            {
                Id = l.Id,
                MemberId = l.MemberId,
                MemberName = l.Member != null ? l.Member.User.FirstName + " " + l.Member.User.LastName : null,
                FacilityId = l.FacilityId,
                FacilityName = l.Facility != null ? l.Facility.Name : null,
                FacilityTypeId = l.FacilityTypeId,
                FacilityTypeName = l.FacilityType != null ? l.FacilityType.Name : null,
                ApplicableTier = l.ApplicableTier,
                MaxConcurrentBookings = l.MaxConcurrentBookings,
                MaxBookingsPerDay = l.MaxBookingsPerDay,
                MaxBookingsPerWeek = l.MaxBookingsPerWeek,
                MaxBookingsPerMonth = l.MaxBookingsPerMonth,
                MaxBookingDurationHours = l.MaxBookingDurationHours,
                MaxAdvanceBookingDays = l.MaxAdvanceBookingDays,
                MinAdvanceBookingHours = l.MinAdvanceBookingHours,
                EarliestBookingTime = l.EarliestBookingTime,
                LatestBookingTime = l.LatestBookingTime,
                AllowedDays = l.AllowedDays,
                RequiresApproval = l.RequiresApproval,
                AllowRecurringBookings = l.AllowRecurringBookings,
                CancellationPenaltyHours = l.CancellationPenaltyHours,
                EffectiveFrom = l.EffectiveFrom,
                EffectiveTo = l.EffectiveTo,
                IsActive = l.IsActive,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt ?? l.CreatedAt
            })
            .FirstAsync();
    }

    public async Task DeleteBookingLimitAsync(ClubManagementDbContext tenantContext, Guid limitId)
    {
        var limit = await tenantContext.MemberBookingLimits.FirstOrDefaultAsync(l => l.Id == limitId);
        if (limit == null)
            throw new InvalidOperationException("Booking limit not found");

        // Soft delete by marking as inactive
        limit.IsActive = false;
        limit.UpdatedAt = DateTime.UtcNow;
        limit.UpdatedBy = "System"; // Should come from current user context

        await tenantContext.SaveChangesAsync();
    }

    public async Task ApplyDefaultTierLimitsAsync(ClubManagementDbContext tenantContext, Guid memberId, MembershipTier tier)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null)
            throw new InvalidOperationException("Member not found");

        var defaultLimits = await GetDefaultTierLimitsAsync(tenantContext, tier);

        // Create a new booking limit based on the tier defaults
        var limit = new MemberBookingLimit
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            ApplicableTier = tier,
            MaxConcurrentBookings = defaultLimits.MaxConcurrentBookings,
            MaxBookingsPerDay = defaultLimits.MaxBookingsPerDay,
            MaxBookingsPerWeek = defaultLimits.MaxBookingsPerWeek,
            MaxBookingsPerMonth = defaultLimits.MaxBookingsPerMonth,
            MaxBookingDurationHours = defaultLimits.MaxBookingDurationHours,
            MaxAdvanceBookingDays = defaultLimits.MaxAdvanceBookingDays,
            MinAdvanceBookingHours = defaultLimits.MinAdvanceBookingHours,
            EarliestBookingTime = defaultLimits.EarliestBookingTime,
            LatestBookingTime = defaultLimits.LatestBookingTime,
            RequiresApproval = defaultLimits.RequiresApproval,
            AllowRecurringBookings = defaultLimits.AllowRecurringBookings,
            CancellationPenaltyHours = defaultLimits.CancellationPenaltyHours,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            Notes = $"Default {tier} tier limits applied",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        tenantContext.MemberBookingLimits.Add(limit);
        await tenantContext.SaveChangesAsync();
    }

    public async Task UpdateMemberUsageAsync(ClubManagementDbContext tenantContext, Guid memberId)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null)
            throw new InvalidOperationException("Member not found");

        var today = DateTime.UtcNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        // Calculate current usage statistics
        var bookingsToday = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date == today)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();

        var bookingsThisWeek = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date >= thisWeekStart)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();

        var bookingsThisMonth = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date >= thisMonthStart)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();

        var concurrentBookings = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime <= DateTime.UtcNow && b.EndDateTime > DateTime.UtcNow)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .CountAsync();

        var totalHoursThisMonth = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && b.StartDateTime.Date >= thisMonthStart)
            .Where(b => b.Status == BookingStatus.Completed)
            .Select(b => (double)((b.EndDateTime - b.StartDateTime).TotalHours))
            .SumAsync();

        // In a full implementation, this would update a MemberUsageCache table
        // For now, we just log that usage has been recalculated
        // The actual usage data will be calculated on-demand
        await Task.CompletedTask;
    }

    public async Task<List<MemberBookingUsageDto>> GetTierUsageStatsAsync(ClubManagementDbContext tenantContext, MembershipTier? tier = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var periodStart = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var periodEnd = endDate ?? DateTime.UtcNow;

        var query = tenantContext.Members
            .Include(m => m.User)
            .AsQueryable();

        if (tier.HasValue)
            query = query.Where(m => m.Tier == tier.Value);

        var members = await query.ToListAsync();
        var usageStats = new List<MemberBookingUsageDto>();

        foreach (var member in members)
        {
            var bookings = await tenantContext.FacilityBookings
                .Where(b => b.MemberId == member.Id)
                .Where(b => b.StartDateTime >= periodStart && b.StartDateTime <= periodEnd)
                .Where(b => b.Status != BookingStatus.Cancelled)
                .ToListAsync();

            var completedBookings = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();

            usageStats.Add(new MemberBookingUsageDto
            {
                MemberId = member.Id,
                MemberName = $"{member.User.FirstName} {member.User.LastName}",
                Date = periodEnd,
                BookingsToday = bookings.Count(b => b.StartDateTime.Date == DateTime.UtcNow.Date),
                BookingsThisWeek = bookings.Count(b => b.StartDateTime.Date >= DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek)),
                BookingsThisMonth = bookings.Count(b => b.StartDateTime.Date >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)),
                ConcurrentBookings = bookings.Count(b => b.StartDateTime <= DateTime.UtcNow && b.EndDateTime > DateTime.UtcNow && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)),
                TotalHoursBooked = (decimal)completedBookings.Sum(b => (b.EndDateTime - b.StartDateTime).TotalHours),
                LastCalculated = DateTime.UtcNow
            });
        }

        return usageStats.OrderByDescending(u => u.TotalHoursBooked).ToList();
    }

    public async Task<List<FacilityDto>> GetAccessibleFacilitiesAsync(ClubManagementDbContext tenantContext, Guid memberId)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<FacilityDto>();

        var memberCertifications = await tenantContext.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToListAsync();

        return await tenantContext.Facilities
            .Where(f => f.Status == FacilityStatus.Available)
            .Where(f => !f.AllowedMembershipTiers.Any() || f.AllowedMembershipTiers.Contains(member.Tier))
            .Where(f => !f.RequiredCertifications.Any() || 
                       f.RequiredCertifications.All(cert => memberCertifications.Contains(cert)))
            .Include(f => f.FacilityType)
            .Select(f => new FacilityDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                FacilityTypeId = f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                FacilityTypeIcon = f.FacilityType.Icon,
                Icon = f.Icon,
                Status = f.Status,
                Capacity = f.Capacity,
                Location = f.Location,
                MemberHourlyRate = f.MemberHourlyRate,
                RequiredCertifications = f.RequiredCertifications.ToList(),
                AllowedMembershipTiers = f.AllowedMembershipTiers.ToList(),
                CanMemberAccess = true
            })
            .ToListAsync();
    }

    public async Task<List<FacilityDto>> GetRestrictedFacilitiesAsync(ClubManagementDbContext tenantContext, Guid memberId)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<FacilityDto>();

        var memberCertifications = await tenantContext.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToListAsync();

        return await tenantContext.Facilities
            .Where(f => (f.AllowedMembershipTiers.Any() && !f.AllowedMembershipTiers.Contains(member.Tier)) ||
                       (f.RequiredCertifications.Any() && !f.RequiredCertifications.All(cert => memberCertifications.Contains(cert))))
            .Include(f => f.FacilityType)
            .Select(f => new FacilityDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                FacilityTypeId = f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                FacilityTypeIcon = f.FacilityType.Icon,
                Icon = f.Icon,
                Status = f.Status,
                Capacity = f.Capacity,
                Location = f.Location,
                RequiredCertifications = f.RequiredCertifications.ToList(),
                AllowedMembershipTiers = f.AllowedMembershipTiers.ToList(),
                CanMemberAccess = false,
                MissingCertifications = f.RequiredCertifications
                    .Where(cert => !memberCertifications.Contains(cert))
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<List<DateTime>> GetAvailableBookingTimesAsync(ClubManagementDbContext tenantContext, Guid facilityId, Guid memberId, DateTime date, int durationMinutes = 60)
    {
        var facility = await tenantContext.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        
        if (facility == null || member == null)
            return new List<DateTime>();

        // Check member access to facility first
        var accessResult = await CheckMemberAccessAsync(tenantContext, facilityId, memberId);
        if (!accessResult.CanAccess)
            return new List<DateTime>();

        // Get applicable booking limits
        var limits = await GetDefaultTierLimitsAsync(tenantContext, member.Tier);
        
        var availableTimes = new List<DateTime>();
        var targetDate = date.Date;
        var duration = TimeSpan.FromMinutes(durationMinutes);

        // Define operating hours (6 AM to 10 PM in 30-minute slots)
        var earliestTime = limits.EarliestBookingTime ?? new TimeSpan(6, 0, 0);
        var latestTime = limits.LatestBookingTime ?? new TimeSpan(22, 0, 0);

        // Get all existing bookings for the facility on this date
        var existingBookings = await tenantContext.FacilityBookings
            .Where(b => b.FacilityId == facilityId)
            .Where(b => b.StartDateTime.Date == targetDate)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Select(b => new { b.StartDateTime, b.EndDateTime })
            .ToListAsync();

        // Generate 30-minute time slots throughout the day
        var currentTime = targetDate.Add(earliestTime);
        var endOfDay = targetDate.Add(latestTime);

        while (currentTime.Add(duration) <= endOfDay)
        {
            var proposedEndTime = currentTime.Add(duration);

            // Check if this slot conflicts with any existing booking
            var hasConflict = existingBookings.Any(b => 
                currentTime < b.EndDateTime && proposedEndTime > b.StartDateTime);

            if (!hasConflict)
            {
                // Check if the member can book at this time (limits validation)
                var limitsResult = await ValidateBookingLimitsAsync(
                    tenantContext, memberId, facilityId, currentTime, proposedEndTime);
                    
                if (limitsResult.IsValid)
                {
                    availableTimes.Add(currentTime);
                }
            }

            currentTime = currentTime.AddMinutes(30); // Move to next 30-minute slot
        }

        return availableTimes;
    }

    public async Task<List<FacilityDto>> RecommendFacilitiesAsync(ClubManagementDbContext tenantContext, Guid memberId, DateTime? preferredTime = null, int? durationMinutes = null)
    {
        var member = await tenantContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return new List<FacilityDto>();

        // Get accessible facilities first
        var accessibleFacilities = await GetAccessibleFacilitiesAsync(tenantContext, memberId);
        
        if (!accessibleFacilities.Any())
            return new List<FacilityDto>();

        // Get member's booking history to understand preferences
        var bookingHistory = await tenantContext.FacilityBookings
            .Where(b => b.MemberId == memberId && b.Status == BookingStatus.Completed)
            .Include(b => b.Facility)
                .ThenInclude(f => f.FacilityType)
            .OrderByDescending(b => b.StartDateTime)
            .Take(50) // Last 50 bookings for analysis
            .ToListAsync();

        var recommendations = new List<(FacilityDto facility, double score)>();

        foreach (var facility in accessibleFacilities)
        {
            double score = 0;

            // Score based on booking frequency (higher = better)
            var bookingCount = bookingHistory.Count(b => b.FacilityId == facility.Id);
            score += bookingCount * 10; // 10 points per previous booking

            // Score based on facility type preference
            var facilityTypeBookings = bookingHistory.Count(b => b.Facility.FacilityTypeId == facility.FacilityTypeId);
            score += facilityTypeBookings * 5; // 5 points per facility type booking

            // Score based on availability at preferred time
            if (preferredTime.HasValue)
            {
                var availableTimes = await GetAvailableBookingTimesAsync(
                    tenantContext, facility.Id, memberId, 
                    preferredTime.Value.Date, durationMinutes ?? 60);
                    
                var timeWindow = TimeSpan.FromHours(2); // 2-hour window around preferred time
                var hasAvailabilityNearPreferredTime = availableTimes.Any(t => 
                    Math.Abs((t - preferredTime.Value).TotalMinutes) <= timeWindow.TotalMinutes);
                    
                if (hasAvailabilityNearPreferredTime)
                    score += 20; // 20 points for availability at preferred time
            }

            // Bonus for VIP/Premium facilities if member has appropriate tier
            if (member.Tier == MembershipTier.VIP || member.Tier == MembershipTier.Premium)
            {
                if (facility.AllowedMembershipTiers.Contains(MembershipTier.VIP) || 
                    facility.AllowedMembershipTiers.Contains(MembershipTier.Premium))
                {
                    score += 15; // 15 points for premium facilities
                }
            }

            // Slight penalty for facilities requiring certifications (more complex)
            if (facility.RequiredCertifications.Any())
            {
                score -= 2;
            }

            recommendations.Add((facility, score));
        }

        // Return top 5 recommendations sorted by score
        return recommendations
            .OrderByDescending(r => r.score)
            .Take(5)
            .Select(r => r.facility)
            .ToList();
    }
}