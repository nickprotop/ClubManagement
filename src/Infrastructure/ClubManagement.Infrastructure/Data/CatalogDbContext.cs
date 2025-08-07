using Microsoft.EntityFrameworkCore;
using ClubManagement.Shared.Models;
using System.Text.Json;

namespace ClubManagement.Infrastructure.Data;

/// <summary>
/// Catalog database context - only contains the Tenants table for tenant registry
/// </summary>
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }

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
    }
}