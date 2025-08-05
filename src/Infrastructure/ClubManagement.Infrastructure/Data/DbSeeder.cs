using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClubManagement.Shared.Models;
using ClubManagement.Infrastructure.Authentication;

namespace ClubManagement.Infrastructure.Data;

public class DbSeeder
{
    private readonly ClubManagementDbContext _context;
    private readonly ILogger<DbSeeder> _logger;
    private readonly IPasswordService _passwordService;

    public DbSeeder(ClubManagementDbContext context, ILogger<DbSeeder> logger, IPasswordService passwordService)
    {
        _context = context;
        _logger = logger;
        _passwordService = passwordService;
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

        // Create demo admin user with proper password hashing
        var adminPassword = "Admin123!"; // Default demo password
        var (passwordHash, passwordSalt) = _passwordService.HashPasswordWithSeparateSalt(adminPassword);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = demoTenant.Id,
            Email = "admin@demo.localhost",
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            FirstName = "Demo",
            LastName = "Admin",
            PhoneNumber = "+1-555-0123",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            ProfilePhotoUrl = null,
            LastLoginAt = null,
            EmailVerified = true,
            PasswordChangedAt = DateTime.UtcNow,
            CustomFields = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Users.Add(adminUser);

        // Create demo member user with proper password hashing
        var memberPassword = "Member123!"; // Default demo password
        var (memberPasswordHash, memberPasswordSalt) = _passwordService.HashPasswordWithSeparateSalt(memberPassword);

        var memberUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = demoTenant.Id,
            Email = "member@demo.localhost",
            PasswordHash = memberPasswordHash,
            PasswordSalt = memberPasswordSalt,
            FirstName = "Demo",
            LastName = "Member",
            PhoneNumber = "+1-555-0456",
            Role = UserRole.Member,
            Status = UserStatus.Active,
            ProfilePhotoUrl = null,
            LastLoginAt = null,
            EmailVerified = true,
            PasswordChangedAt = DateTime.UtcNow,
            CustomFields = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Users.Add(memberUser);
        await _context.SaveChangesAsync(); // Save users to get IDs

        // Create member profile for the demo member user
        var demoMember = new Member
        {
            Id = Guid.NewGuid(),
            UserId = memberUser.Id,
            MembershipNumber = $"MB{DateTime.UtcNow.Year}000001",
            Tier = MembershipTier.Basic,
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow.AddDays(-30), // Joined 30 days ago
            MembershipExpiresAt = DateTime.UtcNow.AddYears(1),
            Balance = 0,
            EmergencyContact = new EmergencyContact
            {
                Name = "Emergency Contact",
                PhoneNumber = "+1-555-0999",
                Relationship = "Friend"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = new List<string>(),
                MedicalConditions = new List<string>()
            },
            CustomFields = new Dictionary<string, object>(),
            TenantId = demoTenant.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Members.Add(demoMember);

        // Create demo coach user with proper password hashing
        var coachPassword = "Coach123!"; // Default demo password
        var (coachPasswordHash, coachPasswordSalt) = _passwordService.HashPasswordWithSeparateSalt(coachPassword);

        var coachUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = demoTenant.Id,
            Email = "coach@demo.localhost",
            PasswordHash = coachPasswordHash,
            PasswordSalt = coachPasswordSalt,
            FirstName = "Demo",
            LastName = "Coach",
            PhoneNumber = "+1-555-0789",
            Role = UserRole.Coach,
            Status = UserStatus.Active,
            ProfilePhotoUrl = null,
            LastLoginAt = null,
            EmailVerified = true,
            PasswordChangedAt = DateTime.UtcNow,
            CustomFields = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Users.Add(coachUser);
        await _context.SaveChangesAsync(); // Save all users

        // Create member profile for the demo coach user so they can register for events
        var demoCoachMember = new Member
        {
            Id = Guid.NewGuid(),
            UserId = coachUser.Id,
            MembershipNumber = $"MB{DateTime.UtcNow.Year}000002",
            Tier = MembershipTier.Premium, // Coaches get premium membership
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow.AddDays(-60), // Joined 60 days ago
            MembershipExpiresAt = DateTime.UtcNow.AddYears(2), // Extended membership
            Balance = 0,
            EmergencyContact = new EmergencyContact
            {
                Name = "Coach Emergency Contact",
                PhoneNumber = "+1-555-0888",
                Relationship = "Spouse"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = new List<string>(),
                MedicalConditions = new List<string>()
            },
            CustomFields = new Dictionary<string, object>
            {
                { "staff_member", true },
                { "coaching_specialties", new List<string> { "Tennis", "Fitness Training" } }
            },
            TenantId = demoTenant.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Members.Add(demoCoachMember);

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
        await _context.SaveChangesAsync(); // Save facilities to get IDs

        // Create demo recurring master events
        var recurringMasterEvents = new List<Event>
        {
            // Weekly Yoga Classes - Monday/Wednesday/Friday 6 PM
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Weekly Yoga Classes",
                Description = "Relaxing yoga sessions for all skill levels focusing on flexibility and mindfulness",
                Type = EventType.Class,
                StartDateTime = GetUtcDate(GetNextWeekday(DateTime.UtcNow, DayOfWeek.Monday)).AddHours(18), // Next Monday 6 PM
                EndDateTime = GetUtcDate(GetNextWeekday(DateTime.UtcNow, DayOfWeek.Monday)).AddHours(19), // Next Monday 7 PM
                FacilityId = facilities[3].Id, // Main Gym
                InstructorId = coachUser.Id,
                MaxCapacity = 20,
                Price = 15.00m,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = null, // Can register up to start time
                AllowWaitlist = true,
                SpecialInstructions = "Bring a yoga mat and water bottle. Modifications available for all levels.",
                RequiredEquipment = new List<string> { "Yoga mat", "Comfortable clothes", "Water bottle" },
                IsRecurringMaster = true,
                Recurrence = new RecurrencePattern
                {
                    Type = RecurrenceType.Weekly,
                    Interval = 1,
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                    EndDate = DateTime.UtcNow.Date.AddMonths(6) // 6 months of classes
                },
                RecurrenceStatus = RecurrenceStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },

            // Weekly Tennis Training - Tuesdays and Thursdays 8 AM
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Morning Tennis Training",
                Description = "Beginner to intermediate tennis training focusing on basic techniques and footwork",
                Type = EventType.Class,
                StartDateTime = GetUtcDate(GetNextWeekday(DateTime.UtcNow, DayOfWeek.Tuesday)).AddHours(8), // Next Tuesday 8 AM
                EndDateTime = GetUtcDate(GetNextWeekday(DateTime.UtcNow, DayOfWeek.Tuesday)).AddHours(10), // Next Tuesday 10 AM
                FacilityId = facilities[0].Id, // Center Court
                InstructorId = coachUser.Id,
                MaxCapacity = 8,
                Price = 25.00m,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = null,
                AllowWaitlist = true,
                SpecialInstructions = "Please bring your own racket and wear appropriate tennis shoes",
                RequiredEquipment = new List<string> { "Tennis racket", "Tennis shoes", "Water bottle" },
                IsRecurringMaster = true,
                Recurrence = new RecurrencePattern
                {
                    Type = RecurrenceType.Weekly,
                    Interval = 1,
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday },
                    EndDate = DateTime.UtcNow.Date.AddMonths(4) // 4 months of training
                },
                RecurrenceStatus = RecurrenceStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },

            // Monthly Board Meeting - First Monday of each month
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Monthly Board Meeting",
                Description = "Monthly club board meeting to discuss operations, finances, and member feedback",
                Type = EventType.Event,
                StartDateTime = GetFirstMondayOfNextMonth().AddHours(19), // First Monday 7 PM
                EndDateTime = GetFirstMondayOfNextMonth().AddHours(21), // First Monday 9 PM
                FacilityId = null, // No specific facility
                InstructorId = null,
                MaxCapacity = 15,
                Price = 0m, // Free for board members
                Status = EventStatus.Scheduled,
                RegistrationDeadline = null,
                AllowWaitlist = false,
                SpecialInstructions = "Board members and interested members welcome. Light refreshments provided.",
                RequiredEquipment = new List<string>(),
                IsRecurringMaster = true,
                Recurrence = new RecurrencePattern
                {
                    Type = RecurrenceType.Monthly,
                    Interval = 1,
                    MaxOccurrences = 12 // One year of meetings
                },
                RecurrenceStatus = RecurrenceStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            }
        };

        // Create regular (non-recurring) demo events
        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Advanced Tennis Workshop",
                Description = "Intensive workshop for experienced players focusing on advanced strategies and techniques",
                Type = EventType.Workshop,
                StartDateTime = GetUtcDate(DateTime.UtcNow).AddDays(3).AddHours(14), // 3 days from now, 2 PM
                EndDateTime = GetUtcDate(DateTime.UtcNow).AddDays(3).AddHours(17), // 3 days from now, 5 PM
                FacilityId = facilities[1].Id, // Court 2
                InstructorId = coachUser.Id,
                MaxCapacity = 6,
                CurrentEnrollment = 1,
                Price = 75.00m,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = DateTime.UtcNow.Date.AddDays(2),
                AllowWaitlist = true,
                SpecialInstructions = "Intermediate to advanced skill level required. Video analysis included.",
                RequiredEquipment = new List<string> { "Tennis racket", "Tennis shoes", "Towel" },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Weekly Swimming Tournament",
                Description = "Friendly swimming competition for all skill levels with prizes for winners",
                Type = EventType.Tournament,
                StartDateTime = GetUtcDate(DateTime.UtcNow).AddDays(7).AddHours(10), // Next week, 10 AM
                EndDateTime = GetUtcDate(DateTime.UtcNow).AddDays(7).AddHours(16), // Next week, 4 PM
                FacilityId = facilities[2].Id, // Olympic Pool
                InstructorId = null, // No specific instructor
                MaxCapacity = 30,
                CurrentEnrollment = 12,
                Price = 15.00m,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = DateTime.UtcNow.Date.AddDays(5),
                AllowWaitlist = false,
                SpecialInstructions = "Registration includes light refreshments. Categories for all ages.",
                RequiredEquipment = new List<string> { "Swimwear", "Swim cap", "Goggles" },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Evening Fitness Class",
                Description = "High-intensity interval training session for all fitness levels",
                Type = EventType.Class,
                StartDateTime = GetUtcDate(DateTime.UtcNow).AddHours(18), // Today 6 PM
                EndDateTime = GetUtcDate(DateTime.UtcNow).AddHours(19), // Today 7 PM
                FacilityId = facilities[3].Id, // Main Gym
                InstructorId = coachUser.Id,
                MaxCapacity = 20,
                CurrentEnrollment = 15,
                Price = 12.00m,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = GetUtcDate(DateTime.UtcNow).AddHours(16), // 2 hours before
                AllowWaitlist = true,
                SpecialInstructions = "Bring a water bottle and towel. Modifications available for all fitness levels.",
                RequiredEquipment = new List<string> { "Workout clothes", "Athletic shoes", "Water bottle", "Towel" },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Club Social Event",
                Description = "Monthly social gathering for all members - food, drinks, and networking",
                Type = EventType.Event,
                StartDateTime = GetUtcDate(DateTime.UtcNow).AddDays(14).AddHours(18), // 2 weeks from now, 6 PM
                EndDateTime = GetUtcDate(DateTime.UtcNow).AddDays(14).AddHours(21), // 2 weeks from now, 9 PM
                FacilityId = null, // No specific facility
                InstructorId = null,
                MaxCapacity = 100,
                CurrentEnrollment = 25,
                Price = 0m, // Free event
                Status = EventStatus.Scheduled,
                RegistrationDeadline = DateTime.UtcNow.Date.AddDays(12),
                AllowWaitlist = false,
                SpecialInstructions = "Complimentary refreshments provided. Dress code: smart casual.",
                RequiredEquipment = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Pool Maintenance",
                Description = "Scheduled maintenance and cleaning of the Olympic pool facility",
                Type = EventType.Maintenance,
                StartDateTime = GetUtcDate(DateTime.UtcNow).AddDays(2).AddHours(6), // Day after tomorrow, 6 AM
                EndDateTime = GetUtcDate(DateTime.UtcNow).AddDays(2).AddHours(10), // Day after tomorrow, 10 AM
                FacilityId = facilities[2].Id, // Olympic Pool
                InstructorId = null,
                MaxCapacity = 0,
                CurrentEnrollment = 0,
                Price = null,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = null,
                AllowWaitlist = false,
                SpecialInstructions = "Pool will be closed during maintenance. Normal operations resume at 10 AM.",
                RequiredEquipment = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Title = "Personal Tennis Coaching",
                Description = "One-on-one tennis coaching session with professional instructor",
                Type = EventType.Private,
                StartDateTime = GetUtcDate(DateTime.UtcNow).AddDays(5).AddHours(16), // 5 days from now, 4 PM
                EndDateTime = GetUtcDate(DateTime.UtcNow).AddDays(5).AddHours(17), // 5 days from now, 5 PM
                FacilityId = facilities[0].Id, // Center Court
                InstructorId = coachUser.Id,
                MaxCapacity = 1,
                CurrentEnrollment = 1,
                Price = 60.00m,
                Status = EventStatus.Scheduled,
                RegistrationDeadline = DateTime.UtcNow.Date.AddDays(4),
                AllowWaitlist = false,
                SpecialInstructions = "Personalized coaching based on individual skill assessment.",
                RequiredEquipment = new List<string> { "Tennis racket", "Tennis shoes" },
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            }
        };

        // Add regular events
        _context.Events.AddRange(events);

        // Add recurring master events and generate their occurrences
        _context.Events.AddRange(recurringMasterEvents);
        await _context.SaveChangesAsync(); // Save masters first to get IDs

        // Generate occurrences for each recurring master event
        foreach (var masterEvent in recurringMasterEvents)
        {
            var occurrences = await GenerateEventOccurrences(masterEvent);
            _context.Events.AddRange(occurrences);
            
            // Update the master event with generation tracking
            masterEvent.LastGeneratedUntil = DateTime.UtcNow.Date.AddMonths(6);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Demo tenant, users, member, facility types, facilities, and events created successfully");
        _logger.LogInformation("Created {RegularCount} regular events and {RecurringCount} recurring series", 
            events.Count, recurringMasterEvents.Count);
        _logger.LogInformation("=========== DEMO CREDENTIALS ===========");
        _logger.LogInformation("Tenant Domain: demo.localhost");
        _logger.LogInformation("Admin Email: admin@demo.localhost");
        _logger.LogInformation("Admin Password: {AdminPassword}", adminPassword);
        _logger.LogInformation("Member Email: member@demo.localhost");
        _logger.LogInformation("Member Password: {MemberPassword}", memberPassword);
        _logger.LogInformation("Coach Email: coach@demo.localhost");
        _logger.LogInformation("Coach Password: {CoachPassword}", coachPassword);
        _logger.LogInformation("======================================");
    }

    private static DateTime GetNextWeekday(DateTime start, DayOfWeek targetDay)
    {
        var daysUntilTarget = ((int)targetDay - (int)start.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0) daysUntilTarget = 7; // Next occurrence, not today
        return start.AddDays(daysUntilTarget);
    }

    private static DateTime GetUtcDate(DateTime dateTime)
    {
        return DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
    }

    private static DateTime GetFirstMondayOfNextMonth()
    {
        var nextMonth = DateTime.UtcNow.Date.AddMonths(1);
        var firstOfMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstOfMonth.DayOfWeek + 7) % 7;
        return firstOfMonth.AddDays(daysUntilMonday);
    }

    private async Task<List<Event>> GenerateEventOccurrences(Event masterEvent)
    {
        if (masterEvent.Recurrence?.Type == RecurrenceType.None)
            return new List<Event>();

        var occurrences = new List<Event>();
        var currentDate = masterEvent.StartDateTime;
        var endDate = DateTime.UtcNow.Date.AddMonths(6); // Generate 6 months of occurrences
        var occurrenceNumber = 1;

        // Override end date if master event has specific end date
        if (masterEvent.Recurrence.EndDate.HasValue && masterEvent.Recurrence.EndDate.Value < endDate)
            endDate = masterEvent.Recurrence.EndDate.Value;

        while (currentDate <= endDate && occurrenceNumber <= (masterEvent.Recurrence.MaxOccurrences ?? 500))
        {
            // Create occurrence
            var occurrence = CreateOccurrence(masterEvent, currentDate, occurrenceNumber);
            occurrences.Add(occurrence);

            // Move to next occurrence date
            currentDate = CalculateNextOccurrence(currentDate, masterEvent.Recurrence);
            occurrenceNumber++;

            // Safety limit
            if (occurrences.Count >= 100) break;
        }

        return occurrences;
    }

    private Event CreateOccurrence(Event masterEvent, DateTime occurrenceDate, int occurrenceNumber)
    {
        var duration = masterEvent.EndDateTime - masterEvent.StartDateTime;
        
        return new Event
        {
            Id = Guid.NewGuid(),
            TenantId = masterEvent.TenantId,
            Title = masterEvent.Title,
            Description = masterEvent.Description,
            Type = masterEvent.Type,
            StartDateTime = occurrenceDate,
            EndDateTime = occurrenceDate.Add(duration),
            FacilityId = masterEvent.FacilityId,
            InstructorId = masterEvent.InstructorId,
            MaxCapacity = masterEvent.MaxCapacity,
            CurrentEnrollment = 0, // Start with no enrollments
            Price = masterEvent.Price,
            Status = EventStatus.Scheduled,
            RegistrationDeadline = masterEvent.RegistrationDeadline.HasValue ? 
                occurrenceDate.Add(masterEvent.RegistrationDeadline.Value - masterEvent.StartDateTime) : null,
            CancellationDeadline = masterEvent.CancellationDeadline.HasValue ? 
                occurrenceDate.Add(masterEvent.CancellationDeadline.Value - masterEvent.StartDateTime) : null,
            CancellationPolicy = masterEvent.CancellationPolicy,
            AllowWaitlist = masterEvent.AllowWaitlist,
            SpecialInstructions = masterEvent.SpecialInstructions,
            RequiredEquipment = masterEvent.RequiredEquipment,
            
            // Recurrence-specific properties
            MasterEventId = masterEvent.Id,
            IsRecurringMaster = false,
            OccurrenceNumber = occurrenceNumber,
            Recurrence = null, // Occurrences don't store recurrence pattern
            
            // Audit properties
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
    }

    private DateTime CalculateNextOccurrence(DateTime currentDate, RecurrencePattern recurrence)
    {
        return recurrence.Type switch
        {
            RecurrenceType.Daily => currentDate.AddDays(recurrence.Interval),
            RecurrenceType.Weekly => CalculateNextWeeklyOccurrence(currentDate, recurrence),
            RecurrenceType.Monthly => currentDate.AddMonths(recurrence.Interval),
            RecurrenceType.Yearly => currentDate.AddYears(recurrence.Interval),
            _ => currentDate.AddDays(1) // Fallback
        };
    }

    private DateTime CalculateNextWeeklyOccurrence(DateTime currentDate, RecurrencePattern recurrence)
    {
        if (!recurrence.DaysOfWeek.Any())
        {
            return currentDate.AddDays(7 * recurrence.Interval);
        }

        // Find next occurrence based on specified days of week
        var nextDate = currentDate.AddDays(1);
        
        for (int dayOffset = 0; dayOffset < 14; dayOffset++) // Check up to 2 weeks ahead
        {
            var candidateDate = nextDate.AddDays(dayOffset);
            if (recurrence.DaysOfWeek.Contains(candidateDate.DayOfWeek))
            {
                return candidateDate;
            }
        }

        // Fallback
        return currentDate.AddDays(7 * recurrence.Interval);
    }
}