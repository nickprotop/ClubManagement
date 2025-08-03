using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClubManagement.Infrastructure.Data;

public class ClubManagementDbContextFactory : IDesignTimeDbContextFactory<ClubManagementDbContext>
{
    public ClubManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClubManagementDbContext>();

        // Build configuration from multiple sources
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Try to get connection string from different sources
        var connectionString = configuration.GetValue<string>("Database:ConnectionString")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? BuildConnectionStringFromConfig(configuration)
            ?? BuildDefaultConnectionString();

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("ClubManagement.Infrastructure");
        });

        return new ClubManagementDbContext(optionsBuilder.Options);
    }

    private static string? BuildConnectionStringFromConfig(IConfiguration configuration)
    {
        var host = configuration["Database:Host"] ?? configuration["POSTGRES_HOST"];
        var port = configuration["Database:Port"] ?? configuration["POSTGRES_PORT"];
        var database = configuration["Database:Database"] ?? configuration["POSTGRES_DB"];
        var username = configuration["Database:Username"] ?? configuration["POSTGRES_USER"];
        var password = configuration["Database:Password"] ?? configuration["POSTGRES_PASSWORD"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database) || 
            string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        return $"Host={host};Port={port ?? "5432"};Database={database};Username={username};Password={password};Include Error Detail=true";
    }

    private static string BuildDefaultConnectionString()
    {
        // Default connection string for development
        return "Host=localhost;Port=4004;Database=clubmanagement;Username=clubadmin;Password=clubpassword;Include Error Detail=true";
    }
}