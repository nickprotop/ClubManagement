using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ClubManagement.Infrastructure.Data;

namespace ClubManagement.Infrastructure.Services;

public interface ITenantDbContextFactory
{
    ClubManagementDbContext CreateDbContext(string schemaName);
}

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly string _connectionString;

    public TenantDbContextFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
    }

    public ClubManagementDbContext CreateDbContext(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClubManagementDbContext>();
        optionsBuilder.UseNpgsql(_connectionString, options =>
        {
            options.MigrationsHistoryTable("__EFMigrationsHistory", schemaName);
        });

        var context = new ClubManagementDbContext(optionsBuilder.Options);
        
        // Set the default schema for this context
        context.Database.ExecuteSqlRaw($"SET search_path TO \"{schemaName}\"");
        
        return context;
    }
}