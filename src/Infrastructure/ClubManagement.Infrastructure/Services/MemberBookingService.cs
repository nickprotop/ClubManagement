using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services.Interfaces;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services;

public class MemberBookingService : IMemberBookingService
{
    private readonly ClubManagementDbContext _context;
    private readonly IMemberFacilityService _memberFacilityService;

    public MemberBookingService(
        ClubManagementDbContext context,
        IMemberFacilityService memberFacilityService)
    {
        _context = context;
        _memberFacilityService = memberFacilityService;
    }

    public async Task<PagedResult<FacilityBookingDto>> GetMemberBookingsAsync(Guid memberId, MemberBookingFilter filter)
    {
        var query = _context.FacilityBookings
            .Where(b => b.MemberId == memberId)
            .Include(b => b.Facility)
                .ThenInclude(f => f.FacilityType)
            .Include(b => b.Member)
                .ThenInclude(m => m.User)
            .AsQueryable();

        // Apply filters
        if (filter.Status.HasValue)
            query = query.Where(b => b.Status == filter.Status.Value);

        if (filter.FacilityId.HasValue)
            query = query.Where(b => b.FacilityId == filter.FacilityId.Value);

        if (filter.FacilityTypeId.HasValue)
            query = query.Where(b => b.Facility.FacilityTypeId == filter.FacilityTypeId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(b => b.StartDateTime >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(b => b.StartDateTime <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(b => 
                b.Facility.Name.ToLower().Contains(searchLower) ||
                (b.Purpose != null && b.Purpose.ToLower().Contains(searchLower)) ||
                (b.Notes != null && b.Notes.ToLower().Contains(searchLower)));
        }

        if (filter.IncludeRecurring == false)
            query = query.Where(b => b.RecurrenceGroupId == null);

        // Apply sorting
        query = filter.SortBy.ToLower() switch
        {
            "facility" => filter.SortDescending ? query.OrderByDescending(b => b.Facility.Name) : query.OrderBy(b => b.Facility.Name),
            "status" => filter.SortDescending ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
            "duration" => filter.SortDescending ? query.OrderByDescending(b => b.EndDateTime - b.StartDateTime) : query.OrderBy(b => b.EndDateTime - b.StartDateTime),
            _ => filter.SortDescending ? query.OrderByDescending(b => b.StartDateTime) : query.OrderBy(b => b.StartDateTime)
        };

        var totalCount = await query.CountAsync();
        var bookings = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new FacilityBookingDto
            {
                Id = b.Id,
                FacilityId = b.FacilityId,
                FacilityName = b.Facility.Name,
                FacilityLocation = b.Facility.Location,
                MemberId = b.MemberId,
                MemberName = b.Member.User.FirstName + " " + b.Member.User.LastName,
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Status = b.Status,
                BookingSource = b.BookingSource,
                Cost = b.Cost,
                Purpose = b.Purpose,
                ParticipantCount = b.ParticipantCount,
                CheckedInAt = b.CheckedInAt,
                CheckedOutAt = b.CheckedOutAt,
                NoShow = b.NoShow,
                Notes = b.Notes,
                MemberNotes = b.MemberNotes,
                CancellationReason = b.CancellationReason,
                CancelledAt = b.CancelledAt,
                IsRecurring = b.RecurrenceGroupId.HasValue,
                RecurrenceGroupId = b.RecurrenceGroupId,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<FacilityBookingDto>
        {
            Items = bookings,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<FacilityBookingDto>> GetMemberUpcomingBookingsAsync(Guid memberId, int daysAhead = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await _context.FacilityBookings
            .Where(b => b.MemberId == memberId)
            .Where(b => b.StartDateTime >= DateTime.UtcNow && b.StartDateTime <= cutoffDate)
            .Where(b => b.Status == BookingStatus.Confirmed)
            .Include(b => b.Facility)
            .OrderBy(b => b.StartDateTime)
            .Select(b => new FacilityBookingDto
            {
                Id = b.Id,
                FacilityId = b.FacilityId,
                FacilityName = b.Facility.Name,
                FacilityLocation = b.Facility.Location,
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Status = b.Status,
                Purpose = b.Purpose,
                ParticipantCount = b.ParticipantCount,
                Notes = b.Notes,
                MemberNotes = b.MemberNotes,
                IsRecurring = b.RecurrenceGroupId.HasValue
            })
            .ToListAsync();
    }

    public async Task<MemberBookingHistoryDto> GetMemberBookingHistoryAsync(Guid memberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
            throw new InvalidOperationException("Member not found");

        var periodStart = startDate ?? DateTime.UtcNow.AddMonths(-6);
        var periodEnd = endDate ?? DateTime.UtcNow;

        var bookings = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId)
            .Where(b => b.StartDateTime >= periodStart && b.StartDateTime <= periodEnd)
            .Include(b => b.Facility)
                .ThenInclude(f => f.FacilityType)
            .ToListAsync();

        var facilityUsage = bookings
            .GroupBy(b => new { b.FacilityId, b.Facility.Name, FacilityTypeName = b.Facility.FacilityType.Name })
            .Select((g, index) => new FacilityUsageStatsDto
            {
                FacilityId = g.Key.FacilityId,
                FacilityName = g.Key.Name,
                FacilityTypeName = g.Key.FacilityTypeName,
                BookingCount = g.Count(),
                TotalHours = (decimal)g.Sum(b => (b.EndDateTime - b.StartDateTime).TotalHours),
                TotalCost = g.Sum(b => b.Cost ?? 0),
                Rank = index + 1
            })
            .OrderByDescending(f => f.BookingCount)
            .ToList();

        var bookingPatterns = bookings
            .GroupBy(b => new { b.StartDateTime.DayOfWeek, HourOfDay = b.StartDateTime.Hour, FacilityTypeName = b.Facility.FacilityType.Name })
            .Select(g => new BookingPatternDto
            {
                DayOfWeek = g.Key.DayOfWeek,
                HourOfDay = g.Key.HourOfDay,
                BookingCount = g.Count(),
                FacilityTypeName = g.Key.FacilityTypeName
            })
            .OrderByDescending(p => p.BookingCount)
            .Take(10)
            .ToList();

        var recentBookings = bookings
            .OrderByDescending(b => b.StartDateTime)
            .Take(10)
            .Select(b => new FacilityBookingDto
            {
                Id = b.Id,
                FacilityId = b.FacilityId,
                FacilityName = b.Facility.Name,
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Status = b.Status,
                Cost = b.Cost,
                Purpose = b.Purpose
            })
            .ToList();

        return new MemberBookingHistoryDto
        {
            MemberId = memberId,
            MemberName = $"{member.User.FirstName} {member.User.LastName}",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalBookings = bookings.Count,
            CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
            CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
            NoShowBookings = bookings.Count(b => b.NoShow),
            TotalHoursBooked = (decimal)bookings.Sum(b => (b.EndDateTime - b.StartDateTime).TotalHours),
            TotalCost = bookings.Sum(b => b.Cost ?? 0),
            TotalSavings = CalculateMemberSavings(bookings, member.Tier),
            AverageBookingDuration = bookings.Count > 0 ? bookings.Average(b => (b.EndDateTime - b.StartDateTime).TotalHours) : 0,
            FacilityUsage = facilityUsage,
            BookingPatterns = bookingPatterns,
            RecentBookings = recentBookings
        };
    }

    public async Task<FacilityBookingDto> CreateMemberBookingAsync(Guid memberId, CreateMemberBookingRequest request)
    {
        // Validate member access
        var accessResult = await _memberFacilityService.CheckMemberAccessAsync(request.FacilityId, memberId);
        if (!accessResult.CanAccess)
        {
            throw new InvalidOperationException($"Member access denied: {string.Join(", ", accessResult.ReasonsDenied)}");
        }

        // Validate booking limits
        var limitsResult = await _memberFacilityService.ValidateBookingLimitsAsync(
            memberId, request.FacilityId, request.StartDateTime, request.EndDateTime);
        
        if (!limitsResult.IsValid)
        {
            throw new InvalidOperationException($"Booking limit violation: {string.Join(", ", limitsResult.Violations)}");
        }

        // Check facility availability
        var conflictingBookings = await _context.FacilityBookings
            .Where(b => b.FacilityId == request.FacilityId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Where(b => b.StartDateTime < request.EndDateTime && b.EndDateTime > request.StartDateTime)
            .CountAsync();

        if (conflictingBookings > 0)
        {
            throw new InvalidOperationException("Facility is not available at the requested time");
        }

        // Calculate cost
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == request.FacilityId);
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        var duration = (request.EndDateTime - request.StartDateTime).TotalHours;
        var cost = CalculateBookingCost(facility, member, duration);

        // Create booking
        var booking = new FacilityBooking
        {
            Id = Guid.NewGuid(),
            FacilityId = request.FacilityId,
            MemberId = memberId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Status = BookingStatus.Confirmed,
            BookingSource = BookingSource.MemberPortal,
            Cost = cost,
            Purpose = request.Purpose,
            ParticipantCount = request.ParticipantCount,
            MemberNotes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Member" // This should come from current user context
        };

        _context.FacilityBookings.Add(booking);
        await _context.SaveChangesAsync();

        // Return DTO
        return new FacilityBookingDto
        {
            Id = booking.Id,
            FacilityId = booking.FacilityId,
            FacilityName = facility?.Name ?? "Unknown",
            MemberId = booking.MemberId,
            StartDateTime = booking.StartDateTime,
            EndDateTime = booking.EndDateTime,
            Status = booking.Status,
            BookingSource = booking.BookingSource,
            Cost = booking.Cost,
            Purpose = booking.Purpose,
            ParticipantCount = booking.ParticipantCount,
            MemberNotes = booking.MemberNotes,
            CreatedAt = booking.CreatedAt
        };
    }

    public async Task<BookingCancellationResult> CancelMemberBookingAsync(Guid memberId, Guid bookingId, string? reason = null)
    {
        var booking = await _context.FacilityBookings
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.MemberId == memberId);

        if (booking == null)
        {
            return new BookingCancellationResult
            {
                Success = false,
                Message = "Booking not found or you don't have permission to cancel it"
            };
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return new BookingCancellationResult
            {
                Success = false,
                Message = "Booking is already cancelled"
            };
        }

        if (booking.Status == BookingStatus.Completed)
        {
            return new BookingCancellationResult
            {
                Success = false,
                Message = "Cannot cancel a completed booking"
            };
        }

        var hoursBeforeStart = (int)(booking.StartDateTime - DateTime.UtcNow).TotalHours;
        var memberLimits = await _memberFacilityService.GetMemberBookingLimitsAsync(memberId);
        var applicableLimit = memberLimits.FirstOrDefault() ?? await _memberFacilityService.GetDefaultTierLimitsAsync(booking.Member.Tier);

        // Calculate penalty
        decimal penaltyAmount = 0;
        decimal refundAmount = booking.Cost ?? 0;
        bool refundIssued = true;

        if (hoursBeforeStart < applicableLimit.CancellationPenaltyHours)
        {
            penaltyAmount = (booking.Cost ?? 0) * 0.5m; // 50% penalty
            refundAmount = (booking.Cost ?? 0) - penaltyAmount;
        }

        // Update booking
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason ?? "Cancelled by member";
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new BookingCancellationResult
        {
            Success = true,
            Message = "Booking cancelled successfully",
            PenaltyAmount = penaltyAmount,
            RefundIssued = refundIssued,
            RefundAmount = refundAmount,
            CancelledAt = booking.CancelledAt.Value,
            HoursBeforeStart = hoursBeforeStart
        };
    }

    public async Task<FacilityBookingDto> ModifyMemberBookingAsync(Guid memberId, Guid bookingId, ModifyBookingRequest request)
    {
        var booking = await _context.FacilityBookings
            .Include(b => b.Facility)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.MemberId == memberId);

        if (booking == null)
            throw new InvalidOperationException("Booking not found");

        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be modified");

        // Check if modification is allowed (at least 2 hours before start)
        if (booking.StartDateTime <= DateTime.UtcNow.AddHours(2))
            throw new InvalidOperationException("Booking cannot be modified less than 2 hours before start time");

        // Apply modifications
        if (request.NewStartDateTime.HasValue)
            booking.StartDateTime = request.NewStartDateTime.Value;

        if (request.NewEndDateTime.HasValue)
            booking.EndDateTime = request.NewEndDateTime.Value;

        if (request.NewFacilityId.HasValue)
        {
            // Validate access to new facility
            var accessResult = await _memberFacilityService.CheckMemberAccessAsync(request.NewFacilityId.Value, memberId);
            if (!accessResult.CanAccess)
                throw new InvalidOperationException("No access to the requested facility");

            booking.FacilityId = request.NewFacilityId.Value;
        }

        if (!string.IsNullOrEmpty(request.NewPurpose))
            booking.Purpose = request.NewPurpose;

        if (request.NewParticipantCount.HasValue)
            booking.ParticipantCount = request.NewParticipantCount.Value;

        // Add modification note
        var modificationNote = $"[MODIFIED {DateTime.UtcNow:yyyy-MM-dd HH:mm}]: {request.ModificationReason}";
        booking.Notes = string.IsNullOrEmpty(booking.Notes) ? modificationNote : $"{booking.Notes}\n{modificationNote}";

        booking.UpdatedAt = DateTime.UtcNow;

        // Recalculate cost if needed
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == booking.FacilityId);
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        var duration = (booking.EndDateTime - booking.StartDateTime).TotalHours;
        booking.Cost = CalculateBookingCost(facility, member, duration);

        await _context.SaveChangesAsync();

        return new FacilityBookingDto
        {
            Id = booking.Id,
            FacilityId = booking.FacilityId,
            FacilityName = facility?.Name ?? "Unknown",
            StartDateTime = booking.StartDateTime,
            EndDateTime = booking.EndDateTime,
            Status = booking.Status,
            Cost = booking.Cost,
            Purpose = booking.Purpose,
            ParticipantCount = booking.ParticipantCount,
            Notes = booking.Notes
        };
    }

    public async Task<List<RecommendedBookingSlot>> GetRecommendedBookingSlotsAsync(Guid memberId, Guid facilityId, DateTime? preferredDate = null)
    {
        var targetDate = preferredDate ?? DateTime.Today.AddDays(1);
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        
        if (member == null)
            return new List<RecommendedBookingSlot>();

        // Get member's booking history for this facility type
        var historicalBookings = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId)
            .Include(b => b.Facility)
            .Where(b => b.Facility.FacilityTypeId == (
                _context.Facilities.Where(f => f.Id == facilityId).Select(f => f.FacilityTypeId).FirstOrDefault()
            ))
            .OrderByDescending(b => b.StartDateTime)
            .Take(20)
            .ToListAsync();

        var recommendations = new List<RecommendedBookingSlot>();

        // Find most common booking patterns
        var commonTimes = historicalBookings
            .GroupBy(b => b.StartDateTime.Hour)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToList();

        foreach (var timeGroup in commonTimes)
        {
            var hour = timeGroup.Key;
            var startTime = targetDate.AddHours(hour);
            var endTime = startTime.AddHours(1); // Default 1 hour

            // Check availability
            var isAvailable = await IsFacilityAvailableAsync(facilityId, startTime, endTime);

            recommendations.Add(new RecommendedBookingSlot
            {
                StartTime = startTime,
                EndTime = endTime,
                RecommendationReason = $"You frequently book at {hour}:00",
                ConfidenceScore = Math.Min(0.9, timeGroup.Count() / 10.0),
                IsAvailable = isAvailable,
                EstimatedCost = await EstimateBookingCostAsync(facilityId, member.Tier, 1)
            });
        }

        // Add some general good time slots if we don't have enough recommendations
        if (recommendations.Count < 3)
        {
            var generalTimes = new[] { 7, 12, 17 }; // 7 AM, 12 PM, 5 PM
            foreach (var hour in generalTimes)
            {
                if (!recommendations.Any(r => r.StartTime.Hour == hour))
                {
                    var startTime = targetDate.AddHours(hour);
                    var endTime = startTime.AddHours(1);
                    var isAvailable = await IsFacilityAvailableAsync(facilityId, startTime, endTime);

                    recommendations.Add(new RecommendedBookingSlot
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        RecommendationReason = "Popular booking time",
                        ConfidenceScore = 0.5,
                        IsAvailable = isAvailable,
                        EstimatedCost = await EstimateBookingCostAsync(facilityId, member.Tier, 1)
                    });
                }
            }
        }

        return recommendations.OrderByDescending(r => r.ConfidenceScore).ToList();
    }

    public async Task<MemberFacilityPreferencesDto> GetMemberPreferencesAsync(Guid memberId)
    {
        // For now, return default preferences - in a full implementation, 
        // this would be stored in a MemberPreferences table
        return new MemberFacilityPreferencesDto
        {
            MemberId = memberId,
            DefaultBookingDuration = 60,
            AutoSelectBestTimes = true,
            SendBookingReminders = true,
            ReminderMinutes = 30,
            AllowWaitlist = true,
            ShareBookingsWithFriends = false,
            Notifications = new NotificationPreferences
            {
                BookingConfirmation = true,
                BookingReminders = true,
                FacilityAvailability = false,
                MaintenanceNotices = true,
                NewFacilities = false,
                PreferredMethod = "email"
            }
        };
    }

    public async Task<MemberFacilityPreferencesDto> UpdateMemberPreferencesAsync(Guid memberId, UpdateMemberPreferencesRequest request)
    {
        // In a full implementation, this would update the MemberPreferences table
        // For now, return the updated preferences
        await Task.CompletedTask;
        
        return await GetMemberPreferencesAsync(memberId);
    }

    public async Task<MemberAccessStatusDto> GetMemberAccessStatusAsync(Guid memberId)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
            throw new InvalidOperationException("Member not found");

        // Get certifications
        var certifications = await _context.MemberFacilityCertifications
            .Where(c => c.MemberId == memberId && c.IsActive)
            .ToListAsync();

        var activeCerts = certifications
            .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToList();

        var expiringSoon = certifications
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30) && c.ExpiryDate.Value > DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToList();

        var expired = certifications
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value <= DateTime.UtcNow)
            .Select(c => c.CertificationType)
            .ToList();

        // Count accessible facilities
        var accessibleFacilities = await _memberFacilityService.GetAccessibleFacilitiesAsync(memberId);
        var restrictedFacilities = await _memberFacilityService.GetRestrictedFacilitiesAsync(memberId);

        // Get current limits and usage
        var limits = await _memberFacilityService.GetMemberBookingLimitsAsync(memberId);
        var usage = await _memberFacilityService.GetMemberBookingUsageAsync(memberId);

        var warnings = new List<string>();
        if (member.MembershipExpiresAt.HasValue && member.MembershipExpiresAt.Value <= DateTime.UtcNow.AddDays(30))
            warnings.Add("Membership expires soon");
        if (expiringSoon.Count > 0)
            warnings.Add($"{expiringSoon.Count} certification(s) expiring soon");

        return new MemberAccessStatusDto
        {
            MemberId = memberId,
            MemberName = $"{member.User.FirstName} {member.User.LastName}",
            Tier = member.Tier,
            Status = member.Status,
            MembershipExpiry = member.MembershipExpiresAt,
            HasActiveAccess = member.Status == MembershipStatus.Active && (!member.MembershipExpiresAt.HasValue || member.MembershipExpiresAt.Value > DateTime.UtcNow),
            ActiveCertifications = activeCerts,
            ExpiringSoonCertifications = expiringSoon,
            ExpiredCertifications = expired,
            AccessibleFacilitiesCount = accessibleFacilities.Count,
            RestrictedFacilitiesCount = restrictedFacilities.Count,
            CurrentLimits = limits.FirstOrDefault(),
            CurrentUsage = usage,
            Warnings = warnings
        };
    }

    public async Task<BookingAvailabilityResult> CheckBookingAvailabilityAsync(Guid memberId, Guid facilityId, DateTime startTime, DateTime endTime)
    {
        var result = new BookingAvailabilityResult();
        var blockingReasons = new List<string>();

        // Check member access
        var accessResult = await _memberFacilityService.CheckMemberAccessAsync(facilityId, memberId);
        result.HasFacilityAccess = accessResult.CanAccess;
        result.HasCertifications = accessResult.CertificationsMet;

        if (!accessResult.CanAccess)
        {
            blockingReasons.AddRange(accessResult.ReasonsDenied);
        }

        // Check booking limits
        var limitsResult = await _memberFacilityService.ValidateBookingLimitsAsync(memberId, facilityId, startTime, endTime);
        result.MeetsLimits = limitsResult.IsValid;

        if (!limitsResult.IsValid)
        {
            blockingReasons.AddRange(limitsResult.Violations);
        }

        // Check facility availability
        var conflictingBookings = await _context.FacilityBookings
            .Where(b => b.FacilityId == facilityId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Where(b => b.StartDateTime < endTime && b.EndDateTime > startTime)
            .CountAsync();

        if (conflictingBookings > 0)
        {
            blockingReasons.Add("Facility is already booked for this time");
        }

        result.IsAvailable = blockingReasons.Count == 0;
        result.BlockingReasons = blockingReasons;

        // Calculate estimated cost
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        if (facility != null && member != null)
        {
            var duration = (endTime - startTime).TotalHours;
            result.EstimatedCost = CalculateBookingCost(facility, member, duration);
        }

        // Generate alternatives if not available
        if (!result.IsAvailable)
        {
            result.Alternatives = await GenerateAlternativeBookingSlotsAsync(facilityId, startTime, endTime);
        }

        return result;
    }

    public async Task<List<FacilityDto>> GetMemberFavoriteFacilitiesAsync(Guid memberId)
    {
        var favoriteIds = await _context.FacilityBookings
            .Where(b => b.MemberId == memberId && b.Status == BookingStatus.Completed)
            .GroupBy(b => b.FacilityId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync();

        return await _context.Facilities
            .Where(f => favoriteIds.Contains(f.Id))
            .Include(f => f.FacilityType)
            .Select(f => new FacilityDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                FacilityTypeId = f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                FacilityTypeIcon = f.FacilityType.Icon,
                Status = f.Status,
                Location = f.Location,
                MemberHourlyRate = f.MemberHourlyRate
            })
            .ToListAsync();
    }

    // Stub implementations for recurring bookings - would be fully implemented in production
    public async Task<RecurringBookingResult> CreateRecurringBookingAsync(Guid memberId, CreateRecurringBookingRequest request)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("Recurring booking creation would be implemented in full system");
    }

    public async Task<RecurringBookingResult> ModifyRecurringBookingAsync(Guid memberId, Guid recurringGroupId, ModifyRecurringBookingRequest request)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("Recurring booking modification would be implemented in full system");
    }

    public async Task<List<RecurringBookingSummaryDto>> GetMemberRecurringBookingsAsync(Guid memberId)
    {
        await Task.CompletedTask;
        return new List<RecurringBookingSummaryDto>();
    }

    // Helper methods
    private decimal CalculateBookingCost(Facility? facility, Member? member, double hours)
    {
        if (facility == null || member == null) return 0;

        var baseRate = facility.MemberHourlyRate;
        var memberDiscount = member.Tier switch
        {
            MembershipTier.VIP => 0.8m,
            MembershipTier.Premium => 0.9m,
            _ => 1.0m
        };

        return (decimal)hours * baseRate * memberDiscount;
    }

    private decimal CalculateMemberSavings(List<FacilityBooking> bookings, MembershipTier tier)
    {
        var discount = tier switch
        {
            MembershipTier.VIP => 0.2m,
            MembershipTier.Premium => 0.1m,
            _ => 0m
        };

        return bookings.Sum(b => (b.Cost ?? 0) * discount / (1 - discount));
    }

    private async Task<bool> IsFacilityAvailableAsync(Guid facilityId, DateTime startTime, DateTime endTime)
    {
        var conflicts = await _context.FacilityBookings
            .Where(b => b.FacilityId == facilityId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Where(b => b.StartDateTime < endTime && b.EndDateTime > startTime)
            .CountAsync();

        return conflicts == 0;
    }

    private async Task<decimal?> EstimateBookingCostAsync(Guid facilityId, MembershipTier tier, double hours)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null) return null;

        var memberDiscount = tier switch
        {
            MembershipTier.VIP => 0.8m,
            MembershipTier.Premium => 0.9m,
            _ => 1.0m
        };

        return (decimal)hours * facility.MemberHourlyRate * memberDiscount;
    }

    private async Task<List<AlternativeBookingSlot>> GenerateAlternativeBookingSlotsAsync(Guid facilityId, DateTime startTime, DateTime endTime)
    {
        var duration = endTime - startTime;
        var alternatives = new List<AlternativeBookingSlot>();

        // Check same day, different times
        var sameDay = startTime.Date;
        for (int hour = 7; hour <= 21; hour++)
        {
            var altStart = sameDay.AddHours(hour);
            var altEnd = altStart.Add(duration);
            
            if (altStart != startTime && await IsFacilityAvailableAsync(facilityId, altStart, altEnd))
            {
                alternatives.Add(new AlternativeBookingSlot
                {
                    StartTime = altStart,
                    EndTime = altEnd,
                    RecommendationReason = "Same day, different time"
                });
                
                if (alternatives.Count >= 3) break;
            }
        }

        return alternatives;
    }
}