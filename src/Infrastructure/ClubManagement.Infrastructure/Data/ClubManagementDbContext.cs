using Microsoft.EntityFrameworkCore;
using ClubManagement.Shared.Models;
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
    public DbSet<Event> Events { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    public DbSet<FacilityBooking> FacilityBookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Communication> Communications { get; set; }
    public DbSet<CommunicationDelivery> CommunicationDeliveries { get; set; }

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

        // Configure relationships
        modelBuilder.Entity<Member>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
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
    }
}