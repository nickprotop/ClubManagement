using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services.Interfaces;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services;

public class EventFacilityService : IEventFacilityService
{
    private readonly ClubManagementDbContext _context;
    private readonly IMemberFacilityService _memberFacilityService;

    public EventFacilityService(
        ClubManagementDbContext context,
        IMemberFacilityService memberFacilityService)
    {
        _context = context;
        _memberFacilityService = memberFacilityService;
    }

    public async Task<ClubManagement.Shared.DTOs.EventEligibilityResult> CheckMemberEligibilityAsync(Guid eventId, Guid memberId)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Facility)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (eventEntity == null || member == null)
        {
            return new ClubManagement.Shared.DTOs.EventEligibilityResult
            {
                IsEligible = false,
                Reasons = new List<string> { "Event or member not found" }
            };
        }

        var result = new ClubManagement.Shared.DTOs.EventEligibilityResult
        {
            IsEligible = true
        };

        var violations = new List<string>();
        var warnings = new List<string>();

        // Check facility access if event requires facility access
        if (eventEntity.RequiresFacilityAccess && eventEntity.FacilityId.HasValue)
        {
            var facilityAccess = await _memberFacilityService.CheckMemberAccessAsync(eventEntity.FacilityId.Value, memberId);
            result.HasFacilityAccess = facilityAccess.CanAccess;
            
            if (!facilityAccess.CanAccess)
            {
                violations.AddRange(facilityAccess.ReasonsDenied);
                result.MissingCertifications.AddRange(facilityAccess.MissingCertifications);
            }
        }
        else
        {
            result.HasFacilityAccess = true; // Not required
        }

        // Check membership tier requirements
        if (eventEntity.AllowedMembershipTiers?.Count > 0)
        {
            result.MeetsMembershipTierRequirement = eventEntity.AllowedMembershipTiers.Contains(member.Tier);
            if (!result.MeetsMembershipTierRequirement)
            {
                violations.Add($"Requires {string.Join(" or ", eventEntity.AllowedMembershipTiers)} membership tier");
            }
        }
        else
        {
            result.MeetsMembershipTierRequirement = true; // No tier restrictions
        }

        // Check certification requirements
        if (eventEntity.RequiredCertifications?.Count > 0)
        {
            var memberCertifications = await _context.MemberFacilityCertifications
                .Where(c => c.MemberId == memberId && c.IsActive)
                .Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value > DateTime.UtcNow)
                .Select(c => c.CertificationType)
                .ToListAsync();

            var missingCerts = eventEntity.RequiredCertifications
                .Where(cert => !memberCertifications.Contains(cert))
                .ToList();

            if (missingCerts.Count > 0)
            {
                violations.Add($"Missing required certifications: {string.Join(", ", missingCerts)}");
                result.MissingCertifications.AddRange(missingCerts);
            }
        }

        // Check age requirements
        if (member.User.DateOfBirth.HasValue)
        {
            var age = CalculateAge(member.User.DateOfBirth.Value);
            
            if (eventEntity.MinimumAge.HasValue && age < eventEntity.MinimumAge.Value)
            {
                violations.Add($"Minimum age requirement: {eventEntity.MinimumAge} years");
                result.MeetsAgeRequirement = false;
            }
            else if (eventEntity.MaximumAge.HasValue && age > eventEntity.MaximumAge.Value)
            {
                violations.Add($"Maximum age requirement: {eventEntity.MaximumAge} years");
                result.MeetsAgeRequirement = false;
            }
            else
            {
                result.MeetsAgeRequirement = true;
            }
        }
        else
        {
            result.MeetsAgeRequirement = true; // No age verification possible
            if (eventEntity.MinimumAge.HasValue || eventEntity.MaximumAge.HasValue)
            {
                warnings.Add("Age verification not possible - date of birth not provided");
            }
        }

        // Check membership status
        if (member.Status != MembershipStatus.Active)
        {
            violations.Add("Membership is not active");
        }

        // Check membership expiry
        if (member.MembershipExpiresAt.HasValue && member.MembershipExpiresAt.Value < DateTime.UtcNow)
        {
            violations.Add("Membership has expired");
        }

        result.Reasons = violations;
        // Note: Shared DTO doesn't have Warnings property
        result.IsEligible = violations.Count == 0;

        return result;
    }

    public async Task<ClubManagement.Shared.DTOs.EventFacilityRequirementsDto> GetEventFacilityRequirementsAsync(Guid eventId)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Facility)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found");
        }

        var result = new ClubManagement.Shared.DTOs.EventFacilityRequirementsDto
        {
            EventId = eventEntity.Id,
            EventTitle = eventEntity.Title,
            FacilityId = eventEntity.FacilityId,
            FacilityName = eventEntity.Facility?.Name,
            RequiredCertifications = eventEntity.RequiredCertifications?.ToList() ?? new List<string>(),
            AllowedMembershipTiers = eventEntity.AllowedMembershipTiers?.ToList() ?? new List<MembershipTier>(),
            RequiresFacilityAccess = eventEntity.RequiresFacilityAccess,
            MinimumAge = eventEntity.MinimumAge,
            MaximumAge = eventEntity.MaximumAge
        };

        // Check for facility conflicts if facility is specified
        if (eventEntity.FacilityId.HasValue)
        {
            var availabilityResult = await CheckFacilityAvailabilityAsync(
                eventEntity.FacilityId.Value, 
                eventEntity.StartDateTime, 
                eventEntity.EndDateTime, 
                eventEntity.Id);

            // Note: HasFacilityConflicts and ConflictDetails not available in Shared DTO
            // Information available through RequiresFacilityAccess property
        }

        return result;
    }

    public async Task<ClubManagement.Shared.DTOs.FacilityAvailabilityResult> CheckFacilityAvailabilityAsync(Guid facilityId, DateTime startTime, DateTime endTime, Guid? excludeEventId = null)
    {
        var result = new ClubManagement.Shared.DTOs.FacilityAvailabilityResult();

        // Check facility status
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null)
        {
            result.IsAvailable = false;
            result.FacilityName = "Unknown";
            return result;
        }

        result.FacilityId = facility.Id;
        result.FacilityName = facility.Name;
        result.MaxCapacity = facility.Capacity ?? 0;
        result.IsUnderMaintenance = facility.Status == FacilityStatus.Maintenance;

        if (facility.Status != FacilityStatus.Available)
        {
            result.IsAvailable = false;
        }

        // Check for conflicting bookings
        var conflictingBookings = await _context.FacilityBookings
            .Where(b => b.FacilityId == facilityId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            .Where(b => b.StartDateTime < endTime && b.EndDateTime > startTime)
            .Include(b => b.Member)
                .ThenInclude(m => m.User)
            .ToListAsync();

        if (conflictingBookings.Any())
        {
            result.IsAvailable = false;
            result.ConflictingBookings = conflictingBookings.Select(b => new ConflictingBookingDto
            {
                BookingId = b.Id,
                MemberName = b.Member.User.FirstName + " " + b.Member.User.LastName,
                StartTime = b.StartDateTime,
                EndTime = b.EndDateTime,
                Purpose = b.Purpose ?? ""
            }).ToList();
        }

        // Check for conflicting events - Note: ConflictingEvents not in Shared DTO
        var conflictingEvents = await _context.Events
            .Where(e => e.FacilityId == facilityId)
            .Where(e => e.Status == EventStatus.Scheduled || e.Status == EventStatus.InProgress)
            .Where(e => e.StartDateTime < endTime && e.EndDateTime > startTime)
            .Where(e => !excludeEventId.HasValue || e.Id != excludeEventId.Value)
            .CountAsync();

        if (conflictingEvents > 0)
        {
            result.IsAvailable = false;
            // Note: Shared DTO doesn't have ConflictingEvents property
        }

        // Set availability based on conflicts
        result.IsAvailable = result.ConflictingBookings.Count == 0 && conflictingEvents == 0;

        // Note: NextAvailableSlot not in Shared DTO, removing this functionality

        return result;
    }

    public async Task<FacilityBookingDto> BookFacilityForEventAsync(Guid eventId, Guid facilityId, string notes = "")
    {
        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found");
        }

        // Check if facility is available
        var availability = await CheckFacilityAvailabilityAsync(facilityId, eventEntity.StartDateTime, eventEntity.EndDateTime, eventId);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException($"Facility not available - has {availability.ConflictingBookings.Count} conflicting bookings");
        }

        // Create facility booking for the event
        var booking = new FacilityBooking
        {
            Id = Guid.NewGuid(),
            FacilityId = facilityId,
            StartDateTime = eventEntity.StartDateTime,
            EndDateTime = eventEntity.EndDateTime,
            Status = BookingStatus.Confirmed,
            BookingSource = BookingSource.EventBooking,
            Purpose = $"Event: {eventEntity.Title}",
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // This should come from current user context
        };

        // For events, we might not have a specific member, so we'll use the instructor or system
        if (eventEntity.InstructorId.HasValue)
        {
            var instructor = await _context.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == eventEntity.InstructorId.Value);
            if (instructor?.Member != null)
            {
                booking.MemberId = instructor.Member.Id;
            }
        }

        _context.FacilityBookings.Add(booking);
        await _context.SaveChangesAsync();

        return new FacilityBookingDto
        {
            Id = booking.Id,
            FacilityId = booking.FacilityId,
            StartDateTime = booking.StartDateTime,
            EndDateTime = booking.EndDateTime,
            Status = booking.Status,
            BookingSource = booking.BookingSource,
            Purpose = booking.Purpose,
            Notes = booking.Notes
        };
    }

    public async Task CancelFacilityBookingForEventAsync(Guid eventId)
    {
        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventEntity == null) return;

        var booking = await _context.FacilityBookings
            .FirstOrDefaultAsync(b => b.Purpose != null && b.Purpose.Contains($"Event: {eventEntity.Title}") && 
                                     b.BookingSource == BookingSource.EventBooking);

        if (booking != null)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = "Event cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<EventListDto>> GetEventsByCertificationRequirementAsync(string certificationType)
    {
        return await _context.Events
            .Where(e => e.RequiredCertifications.Contains(certificationType))
            .Where(e => e.Status == EventStatus.Scheduled)
            .Include(e => e.Facility)
            .Include(e => e.Instructor)
            .Select(e => new EventListDto
            {
                Id = e.Id,
                Title = e.Title,
                Type = e.Type,
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                FacilityName = e.Facility != null ? e.Facility.Name : null,
                InstructorName = e.Instructor != null ? e.Instructor.FirstName + " " + e.Instructor.LastName : null,
                MaxCapacity = e.MaxCapacity,
                CurrentEnrollment = e.CurrentEnrollment,
                Status = e.Status
            })
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();
    }

    public async Task<List<EventListDto>> GetEventsForMembershipTierAsync(MembershipTier tier)
    {
        return await _context.Events
            .Where(e => e.AllowedMembershipTiers.Count == 0 || e.AllowedMembershipTiers.Contains(tier))
            .Where(e => e.Status == EventStatus.Scheduled)
            .Include(e => e.Facility)
            .Include(e => e.Instructor)
            .Select(e => new EventListDto
            {
                Id = e.Id,
                Title = e.Title,
                Type = e.Type,
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                FacilityName = e.Facility != null ? e.Facility.Name : null,
                InstructorName = e.Instructor != null ? e.Instructor.FirstName + " " + e.Instructor.LastName : null,
                MaxCapacity = e.MaxCapacity,
                CurrentEnrollment = e.CurrentEnrollment,
                Status = e.Status
            })
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();
    }

    public async Task<ClubManagement.Shared.DTOs.ValidationResult> ValidateEventFacilityRequirementsAsync(CreateEventRequest request)
    {
        var result = new ClubManagement.Shared.DTOs.ValidationResult { IsValid = true };

        // Validate facility exists and is available if specified
        if (request.FacilityId.HasValue)
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == request.FacilityId.Value);
            if (facility == null)
            {
                result.Errors.Add("Selected facility does not exist");
                result.IsValid = false;
            }
            else if (facility.Status != FacilityStatus.Available)
            {
                result.Errors.Add($"Selected facility is {facility.Status}");
                result.IsValid = false;
            }
            else
            {
                // Check facility availability for the time slot
                var availability = await CheckFacilityAvailabilityAsync(facility.Id, request.StartDateTime, request.EndDateTime);
                if (!availability.IsAvailable)
                {
                    result.Errors.Add($"Facility has {availability.ConflictingBookings.Count} conflicting bookings");
                    result.IsValid = false;
                }
            }
        }

        // Validate certification requirements exist
        if (request.RequiredCertifications?.Count > 0)
        {
            var existingTypes = await _context.Facilities
                .SelectMany(f => f.RequiredCertifications)
                .Union(_context.MemberFacilityCertifications.Select(c => c.CertificationType))
                .Distinct()
                .ToListAsync();

            var invalidCerts = request.RequiredCertifications
                .Where(cert => !existingTypes.Contains(cert))
                .ToList();

            if (invalidCerts.Count > 0)
            {
                result.Warnings.AddRange(invalidCerts.Select(cert => $"Certification type '{cert}' is not currently used in the system"));
            }
        }

        // Validate age requirements
        if (request.MinimumAge.HasValue && request.MaximumAge.HasValue)
        {
            if (request.MinimumAge.Value >= request.MaximumAge.Value)
            {
                result.Errors.Add("Minimum age must be less than maximum age");
                result.IsValid = false;
            }
        }

        return result;
    }

    public async Task<ClubManagement.Shared.DTOs.ValidationResult> ValidateEventFacilityRequirementsAsync(Guid eventId, UpdateEventRequest request)
    {
        var result = await ValidateEventFacilityRequirementsAsync((CreateEventRequest)request);

        // Additional validation for updates - check if changing facility affects existing registrations
        var existingEvent = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (existingEvent != null && existingEvent.Registrations.Any())
        {
            // Check if new requirements would affect existing registrants
            if (request.RequiredCertifications?.Count > 0 || 
                request.AllowedMembershipTiers?.Count > 0 ||
                request.MinimumAge.HasValue ||
                request.MaximumAge.HasValue)
            {
                result.Warnings.Add($"Event has {existingEvent.Registrations.Count} existing registrations. Changing requirements may affect participant eligibility.");
            }
        }

        return result;
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    private async Task<DateTime?> FindNextAvailableSlotAsync(Guid facilityId, DateTime startFrom, TimeSpan duration)
    {
        // Simple implementation - in practice this would be more sophisticated
        var checkTime = startFrom;
        var maxChecks = 48; // Check up to 48 hours ahead
        var checkInterval = TimeSpan.FromHours(1);

        for (int i = 0; i < maxChecks; i++)
        {
            var availability = await CheckFacilityAvailabilityAsync(facilityId, checkTime, checkTime.Add(duration));
            if (availability.IsAvailable)
            {
                return checkTime;
            }
            checkTime = checkTime.Add(checkInterval);
        }

        return null;
    }
}