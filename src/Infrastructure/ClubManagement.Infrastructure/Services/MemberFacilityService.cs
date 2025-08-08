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

    public async Task<List<FacilityCertificationDto>> GetMemberCertificationsAsync(Guid memberId)
    {
        return await _context.MemberFacilityCertifications
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

    public async Task<FacilityCertificationDto> CreateCertificationAsync(CreateCertificationRequest request)
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

        _context.MemberFacilityCertifications.Add(certification);
        await _context.SaveChangesAsync();

        return await _context.MemberFacilityCertifications
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

    public async Task<bool> IsCertificationValidAsync(Guid memberId, string certificationType)
    {
        return await _context.MemberFacilityCertifications
            .AnyAsync(c => c.MemberId == memberId && 
                          c.CertificationType == certificationType && 
                          c.IsActive &&
                          (!c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow));
    }

    public async Task<List<FacilityCertificationDto>> GetExpiringCertificationsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await _context.MemberFacilityCertifications
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
        // Implementation would create a new MemberBookingLimit entity
        await Task.CompletedTask;
        throw new NotImplementedException("Would create new booking limit in full implementation");
    }

    public async Task<MemberBookingLimitDto> UpdateBookingLimitAsync(Guid limitId, UpdateMemberBookingLimitRequest request)
    {
        // Implementation would update existing MemberBookingLimit entity
        await Task.CompletedTask;
        throw new NotImplementedException("Would update booking limit in full implementation");
    }

    public async Task DeleteBookingLimitAsync(Guid limitId)
    {
        // Implementation would soft-delete or remove MemberBookingLimit entity
        await Task.CompletedTask;
        throw new NotImplementedException("Would delete booking limit in full implementation");
    }

    public async Task ApplyDefaultTierLimitsAsync(Guid memberId, MembershipTier tier)
    {
        // Implementation would create default booking limits based on tier
        await Task.CompletedTask;
        throw new NotImplementedException("Would apply default tier limits in full implementation");
    }

    public async Task UpdateMemberUsageAsync(Guid memberId)
    {
        // Implementation would recalculate and cache member booking usage statistics
        await Task.CompletedTask;
        throw new NotImplementedException("Would update usage stats in full implementation");
    }

    public async Task<List<MemberBookingUsageDto>> GetTierUsageStatsAsync(MembershipTier? tier = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Implementation would aggregate usage statistics by tier
        await Task.CompletedTask;
        return new List<MemberBookingUsageDto>();
    }

    public async Task<List<DateTime>> GetAvailableBookingTimesAsync(Guid facilityId, Guid memberId, DateTime date, int durationMinutes = 60)
    {
        // Implementation would find available booking slots for the member
        await Task.CompletedTask;
        return new List<DateTime>();
    }

    public async Task<List<FacilityDto>> RecommendFacilitiesAsync(Guid memberId, DateTime? preferredTime = null, int? durationMinutes = null)
    {
        // Implementation would recommend facilities based on member preferences and access
        await Task.CompletedTask;
        return new List<FacilityDto>();
    }
}