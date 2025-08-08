using Microsoft.EntityFrameworkCore;
using ClubManagement.Shared.Models;
using ClubManagement.Domain.Entities;
using System.Text.Json;

namespace ClubManagement.Infrastructure.Data;

public class ClubManagementDbContext : DbContext
{
    public ClubManagementDbContext(DbContextOptions<ClubManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<FacilityType> FacilityTypes { get; set; }
    public DbSet<Facility> Facilities { get; set; }
    public DbSet<HardwareType> HardwareTypes { get; set; }
    public DbSet<Hardware> Hardware { get; set; }
    public DbSet<HardwareAssignment> HardwareAssignments { get; set; }
    public DbSet<EventEquipmentRequirement> EventEquipmentRequirements { get; set; }
    public DbSet<EventEquipmentAssignment> EventEquipmentAssignments { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    public DbSet<FacilityBooking> FacilityBookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Communication> Communications { get; set; }
    public DbSet<CommunicationDelivery> CommunicationDeliveries { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ImpersonationSession> ImpersonationSessions { get; set; }
    public DbSet<MemberAuditLog> MemberAuditLogs { get; set; }
    public DbSet<MemberFacilityCertification> MemberFacilityCertifications { get; set; }
    public DbSet<MemberBookingLimit> MemberBookingLimits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure JSON columns for PostgreSQL
        modelBuilder.Entity<Tenant>()
            .Property(e => e.Branding)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<BrandingSettings>(v, (JsonSerializerOptions?)null) ?? new BrandingSettings())
            .HasColumnType("jsonb");

        modelBuilder.Entity<User>()
            .Property(e => e.CustomFields)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Member>()
            .Property(e => e.CustomFields)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Member>()
            .Property(e => e.EmergencyContact)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<EmergencyContact>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");

        modelBuilder.Entity<Member>()
            .Property(e => e.MedicalInfo)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MedicalInfo>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");

        modelBuilder.Entity<FacilityType>()
            .Property(e => e.PropertySchema)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<PropertySchema>(v, (JsonSerializerOptions?)null) ?? new PropertySchema())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Facility>()
            .Property(e => e.Properties)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Facility>()
            .Property(e => e.OperatingDays)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<DayOfWeek>>(v, (JsonSerializerOptions?)null) ?? new List<DayOfWeek>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Facility>()
            .Property(e => e.AllowedMembershipTiers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<MembershipTier>>(v, (JsonSerializerOptions?)null) ?? new List<MembershipTier>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Facility>()
            .Property(e => e.RequiredCertifications)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<FacilityType>()
            .Property(e => e.AllowedMembershipTiers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<MembershipTier>>(v, (JsonSerializerOptions?)null) ?? new List<MembershipTier>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<FacilityType>()
            .Property(e => e.RequiredCertifications)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<FacilityBooking>()
            .Property(e => e.AdditionalParticipants)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<HardwareType>()
            .Property(e => e.PropertySchema)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<PropertySchema>(v, (JsonSerializerOptions?)null) ?? new PropertySchema())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Hardware>()
            .Property(e => e.Properties)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Event>()
            .Property(e => e.Recurrence)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<RecurrencePattern>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");

        modelBuilder.Entity<Event>()
            .Property(e => e.RequiredEquipment)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        // Event recurrence relationships
        modelBuilder.Entity<Event>()
            .HasOne(e => e.MasterEvent)
            .WithMany(e => e.Occurrences)
            .HasForeignKey(e => e.MasterEventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Event recurrence indexes
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.MasterEventId);

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.IsRecurringMaster);

        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.MasterEventId, e.StartDateTime });

        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.RecurrenceStatus, e.LastGeneratedUntil });

        modelBuilder.Entity<Payment>()
            .Property(e => e.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Subscription>()
            .Property(e => e.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Communication>()
            .Property(e => e.RecipientMemberIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Communication>()
            .Property(e => e.RecipientRoles)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<UserRole>>(v, (JsonSerializerOptions?)null) ?? new List<UserRole>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Communication>()
            .Property(e => e.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        // Configure indexes
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Domain)
            .IsUnique();

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.SchemaName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Email, u.TenantId })
            .IsUnique();

        modelBuilder.Entity<Member>()
            .HasIndex(m => new { m.MembershipNumber, m.TenantId })
            .IsUnique();

        // Configure relationships - User-Member one-to-one relationship
        modelBuilder.Entity<Member>()
            .HasOne(m => m.User)
            .WithOne(u => u.Member)
            .HasForeignKey<Member>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Facility>()
            .HasOne(f => f.FacilityType)
            .WithMany()
            .HasForeignKey(f => f.FacilityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hardware>()
            .HasOne(h => h.HardwareType)
            .WithMany()
            .HasForeignKey(h => h.HardwareTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HardwareAssignment>()
            .HasOne(ha => ha.Hardware)
            .WithMany(h => h.Assignments)
            .HasForeignKey(ha => ha.HardwareId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HardwareAssignment>()
            .HasOne(ha => ha.Member)
            .WithMany()
            .HasForeignKey(ha => ha.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.IsRevoked })
            .HasFilter("\"IsRevoked\" = false");

        // Configure ImpersonationSession
        modelBuilder.Entity<ImpersonationSession>()
            .Property(e => e.ActionsPerformed)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<ImpersonationSession>()
            .HasOne(s => s.AdminUser)
            .WithMany()
            .HasForeignKey(s => s.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImpersonationSession>()
            .HasOne(s => s.TargetMember)
            .WithMany()
            .HasForeignKey(s => s.TargetMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImpersonationSession>()
            .HasIndex(s => new { s.AdminUserId, s.IsActive });

        modelBuilder.Entity<ImpersonationSession>()
            .HasIndex(s => new { s.TargetMemberId, s.IsActive });

        modelBuilder.Entity<ImpersonationSession>()
            .HasIndex(s => s.ExpiresAt);

        // Configure MemberAuditLog
        modelBuilder.Entity<MemberAuditLog>()
            .Property(e => e.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<MemberAuditLog>()
            .HasOne(log => log.PerformedByUser)
            .WithMany()
            .HasForeignKey(log => log.PerformedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MemberAuditLog>()
            .HasOne(log => log.TargetMember)
            .WithMany()
            .HasForeignKey(log => log.TargetMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MemberAuditLog>()
            .HasIndex(log => new { log.PerformedBy, log.Timestamp });

        modelBuilder.Entity<MemberAuditLog>()
            .HasIndex(log => new { log.TargetMemberId, log.Timestamp });

        modelBuilder.Entity<MemberAuditLog>()
            .HasIndex(log => new { log.Action, log.Timestamp });

        // Configure Facility relationships
        modelBuilder.Entity<FacilityBooking>()
            .HasOne(fb => fb.Facility)
            .WithMany(f => f.Bookings)
            .HasForeignKey(fb => fb.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FacilityBooking>()
            .HasOne(fb => fb.Member)
            .WithMany()
            .HasForeignKey(fb => fb.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FacilityBooking>()
            .HasOne(fb => fb.BookedByUser)
            .WithMany()
            .HasForeignKey(fb => fb.BookedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FacilityBooking>()
            .HasOne(fb => fb.CheckedInByUser)
            .WithMany()
            .HasForeignKey(fb => fb.CheckedInByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FacilityBooking>()
            .HasOne(fb => fb.CheckedOutByUser)
            .WithMany()
            .HasForeignKey(fb => fb.CheckedOutByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FacilityBooking>()
            .HasOne(fb => fb.CancelledByUser)
            .WithMany()
            .HasForeignKey(fb => fb.CancelledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure MemberFacilityCertification relationships
        modelBuilder.Entity<MemberFacilityCertification>()
            .HasOne(mfc => mfc.Member)
            .WithMany()
            .HasForeignKey(mfc => mfc.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemberFacilityCertification>()
            .HasOne(mfc => mfc.CertifiedByUser)
            .WithMany()
            .HasForeignKey(mfc => mfc.CertifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure FacilityType relationship
        modelBuilder.Entity<FacilityType>()
            .HasMany(ft => ft.Facilities)
            .WithOne(f => f.FacilityType)
            .HasForeignKey(f => f.FacilityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Facility indexes
        modelBuilder.Entity<Facility>()
            .HasIndex(f => new { f.Status, f.TenantId });

        modelBuilder.Entity<Facility>()
            .HasIndex(f => new { f.FacilityTypeId, f.Status });

        modelBuilder.Entity<Facility>()
            .HasIndex(f => f.Location);

        // FacilityBooking indexes
        modelBuilder.Entity<FacilityBooking>()
            .HasIndex(fb => new { fb.FacilityId, fb.StartDateTime, fb.EndDateTime });

        modelBuilder.Entity<FacilityBooking>()
            .HasIndex(fb => new { fb.MemberId, fb.Status });

        modelBuilder.Entity<FacilityBooking>()
            .HasIndex(fb => new { fb.Status, fb.StartDateTime });

        modelBuilder.Entity<FacilityBooking>()
            .HasIndex(fb => fb.BookingSource);

        modelBuilder.Entity<FacilityBooking>()
            .HasIndex(fb => fb.RecurrenceGroupId)
            .HasFilter("\"RecurrenceGroupId\" IS NOT NULL");

        // MemberFacilityCertification indexes
        modelBuilder.Entity<MemberFacilityCertification>()
            .HasIndex(mfc => new { mfc.MemberId, mfc.CertificationType, mfc.IsActive });

        modelBuilder.Entity<MemberFacilityCertification>()
            .HasIndex(mfc => new { mfc.ExpiryDate, mfc.IsActive })
            .HasFilter("\"ExpiryDate\" IS NOT NULL AND \"IsActive\" = true");

        // MemberBookingLimit JSON columns
        modelBuilder.Entity<MemberBookingLimit>()
            .Property(e => e.AllowedDays)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, (JsonSerializerOptions?)null) : null,
                v => !string.IsNullOrEmpty(v) ? JsonSerializer.Deserialize<DayOfWeek[]>(v, (JsonSerializerOptions?)null) : null)
            .HasColumnType("jsonb");

        // MemberBookingLimit indexes
        modelBuilder.Entity<MemberBookingLimit>()
            .HasIndex(mbl => new { mbl.MemberId, mbl.IsActive });

        modelBuilder.Entity<MemberBookingLimit>()
            .HasIndex(mbl => new { mbl.FacilityId, mbl.IsActive });

        modelBuilder.Entity<MemberBookingLimit>()
            .HasIndex(mbl => new { mbl.FacilityTypeId, mbl.IsActive });

        modelBuilder.Entity<MemberBookingLimit>()
            .HasIndex(mbl => new { mbl.ApplicableTier, mbl.IsActive });

        // Configure Event JSON columns
        modelBuilder.Entity<Event>()
            .Property(e => e.RequiredCertifications)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Event>()
            .Property(e => e.AllowedMembershipTiers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<MembershipTier>>(v, (JsonSerializerOptions?)null) ?? new List<MembershipTier>())
            .HasColumnType("jsonb");

        modelBuilder.Entity<Event>()
            .Property(e => e.RequiredEquipment)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        // Event indexes for facility integration
        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.FacilityId, e.StartDateTime, e.EndDateTime })
            .HasFilter("\"FacilityId\" IS NOT NULL");

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.RequiresFacilityAccess)
            .HasFilter("\"RequiresFacilityAccess\" = true");

        modelBuilder.Entity<MemberBookingLimit>()
            .HasIndex(mbl => new { mbl.EffectiveFrom, mbl.EffectiveTo, mbl.IsActive });
    }
}