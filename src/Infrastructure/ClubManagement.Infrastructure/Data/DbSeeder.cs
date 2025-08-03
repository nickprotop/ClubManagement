using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Data;

public class DbSeeder
{
    private readonly ClubManagementDbContext _context;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(ClubManagementDbContext context, ILogger<DbSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Ensure database is created and migrations are applied
            await _context.Database.MigrateAsync();

            // Seed demo tenant if it doesn't exist
            await SeedDemoTenantAsync();

            // Save all changes
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedDemoTenantAsync()
    {
        // Check if demo tenant already exists
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Domain == "demo.localhost");

        if (existingTenant != null)
        {
            _logger.LogInformation("Demo tenant already exists, skipping seed");
            return;
        }

        _logger.LogInformation("Creating demo tenant...");

        // Create demo tenant
        var demoTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Demo Sports Club",
            SchemaName = "demo_club",
            Domain = "demo.localhost",
            Status = TenantStatus.Active,
            Branding = new BrandingSettings
            {
                PrimaryColor = "#1976d2",
                SecondaryColor = "#dc004e",
                CompanyName = "Demo Sports Club",
                LogoUrl = string.Empty
            },
            Plan = SubscriptionPlan.Premium,
            TrialEndsAt = null,
            MaxMembers = 500,
            MaxFacilities = 25,
            MaxStaff = 15,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Tenants.Add(demoTenant);
        await _context.SaveChangesAsync(); // Save tenant first to get the ID

        // Create demo admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = demoTenant.Id,
            Email = "admin@demo.localhost",
            FirstName = "Demo",
            LastName = "Admin",
            PhoneNumber = "+1-555-0123",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            ProfilePhotoUrl = null,
            LastLoginAt = null,
            EmailVerified = true,
            CustomFields = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Users.Add(adminUser);

        // Create facility types
        var facilityTypes = new List<FacilityType>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Tennis Court",
                Description = "Professional tennis courts for singles and doubles play",
                Icon = "sports_tennis",
                PropertySchema = new PropertySchema
                {
                    Properties = new List<PropertyDefinition>
                    {
                        new() { Key = "surface_type", Label = "Surface Type", Type = PropertyType.Select, Required = true, Options = new List<string> { "Clay", "Hard Court", "Grass", "Synthetic" } },
                        new() { Key = "lighting", Label = "Has Lighting", Type = PropertyType.Boolean, Required = false },
                        new() { Key = "net_height", Label = "Net Height (cm)", Type = PropertyType.Number, Required = false, DefaultValue = "91.4" }
                    }
                },
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Swimming Pool",
                Description = "Indoor and outdoor swimming facilities",
                Icon = "pool",
                PropertySchema = new PropertySchema
                {
                    Properties = new List<PropertyDefinition>
                    {
                        new() { Key = "pool_type", Label = "Pool Type", Type = PropertyType.Select, Required = true, Options = new List<string> { "Lap Pool", "Recreation Pool", "Kids Pool", "Diving Pool" } },
                        new() { Key = "temperature", Label = "Temperature (°C)", Type = PropertyType.Number, Required = false },
                        new() { Key = "depth", Label = "Depth (m)", Type = PropertyType.Number, Required = true },
                        new() { Key = "heated", Label = "Heated", Type = PropertyType.Boolean, Required = false }
                    }
                },
                IsActive = true,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Fitness Center",
                Description = "Modern fitness and gym facilities",
                Icon = "fitness_center",
                PropertySchema = new PropertySchema
                {
                    Properties = new List<PropertyDefinition>
                    {
                        new() { Key = "area_sqm", Label = "Area (sq m)", Type = PropertyType.Number, Required = false },
                        new() { Key = "equipment_types", Label = "Available Equipment", Type = PropertyType.MultiSelect, Required = false, Options = new List<string> { "Cardio", "Weights", "Functional Training", "Group Fitness" } },
                        new() { Key = "air_conditioning", Label = "Air Conditioning", Type = PropertyType.Boolean, Required = false }
                    }
                },
                IsActive = true,
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            }
        };

        _context.FacilityTypes.AddRange(facilityTypes);
        await _context.SaveChangesAsync(); // Save facility types to get IDs

        // Create sample facilities
        var facilities = new List<Facility>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Center Court",
                Description = "Main tennis court with premium surface and lighting",
                FacilityTypeId = facilityTypes[0].Id, // Tennis Court
                Properties = new Dictionary<string, object>
                {
                    { "surface_type", "Clay" },
                    { "lighting", true },
                    { "net_height", 91.4 }
                },
                Status = FacilityStatus.Available,
                Capacity = 4,
                HourlyRate = 25.00m,
                RequiresBooking = true,
                MaxBookingDaysInAdvance = 30,
                MinBookingDurationMinutes = 60,
                MaxBookingDurationMinutes = 180,
                OperatingHoursStart = new TimeSpan(6, 0, 0),
                OperatingHoursEnd = new TimeSpan(22, 0, 0),
                OperatingDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Court 2",
                Description = "Standard tennis court for regular play",
                FacilityTypeId = facilityTypes[0].Id, // Tennis Court
                Properties = new Dictionary<string, object>
                {
                    { "surface_type", "Hard Court" },
                    { "lighting", true },
                    { "net_height", 91.4 }
                },
                Status = FacilityStatus.Available,
                Capacity = 4,
                HourlyRate = 20.00m,
                RequiresBooking = true,
                MaxBookingDaysInAdvance = 30,
                MinBookingDurationMinutes = 60,
                MaxBookingDurationMinutes = 180,
                OperatingHoursStart = new TimeSpan(6, 0, 0),
                OperatingHoursEnd = new TimeSpan(22, 0, 0),
                OperatingDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Olympic Pool",
                Description = "50m Olympic-size swimming pool",
                FacilityTypeId = facilityTypes[1].Id, // Swimming Pool
                Properties = new Dictionary<string, object>
                {
                    { "pool_type", "Lap Pool" },
                    { "temperature", 26.5 },
                    { "depth", 2.0 },
                    { "heated", true }
                },
                Status = FacilityStatus.Available,
                Capacity = 50,
                HourlyRate = 15.00m,
                RequiresBooking = true,
                MaxBookingDaysInAdvance = 14,
                MinBookingDurationMinutes = 60,
                MaxBookingDurationMinutes = 240,
                OperatingHoursStart = new TimeSpan(5, 0, 0),
                OperatingHoursEnd = new TimeSpan(23, 0, 0),
                OperatingDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Main Gym",
                Description = "Fully equipped fitness center with modern equipment",
                FacilityTypeId = facilityTypes[2].Id, // Fitness Center
                Properties = new Dictionary<string, object>
                {
                    { "area_sqm", 500 },
                    { "equipment_types", new[] { "Cardio", "Weights", "Functional Training" } },
                    { "air_conditioning", true }
                },
                Status = FacilityStatus.Available,
                Capacity = 75,
                HourlyRate = 10.00m,
                RequiresBooking = false,
                MaxBookingDaysInAdvance = 7,
                MinBookingDurationMinutes = 60,
                MaxBookingDurationMinutes = 480,
                OperatingHoursStart = new TimeSpan(5, 0, 0),
                OperatingHoursEnd = new TimeSpan(0, 0, 0), // 24 hours (midnight)
                OperatingDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            }
        };

        _context.Facilities.AddRange(facilities);

        _logger.LogInformation("Demo tenant, admin user, facility types, and facilities created successfully");
    }
}