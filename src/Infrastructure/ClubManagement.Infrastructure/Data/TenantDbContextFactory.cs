using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Data;

public interface ITenantDbContextFactory
{
    Task<ClubManagementDbContext> CreateTenantDbContextAsync(Guid tenantId);
    Task<ClubManagementDbContext> CreateTenantDbContextAsync(string tenantDomain);
    Task EnsureTenantDatabaseAsync(string tenantDomain, string databaseName);
}

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly CatalogDbContext _catalogContext;
    private readonly IConfiguration _configuration;
    private readonly string _baseConnectionString;

    public TenantDbContextFactory(CatalogDbContext catalogContext, IConfiguration configuration)
    {
        _catalogContext = catalogContext;
        _configuration = configuration;
        
        // Get base connection string
        _baseConnectionString = configuration.GetValue<string>("Database:ConnectionString") 
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    public async Task<ClubManagementDbContext> CreateTenantDbContextAsync(Guid tenantId)
    {
        var tenant = await _catalogContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == TenantStatus.Active);
        
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID {tenantId} not found or inactive");

        return await CreateTenantDbContextAsync(tenant.Domain);
    }

    public async Task<ClubManagementDbContext> CreateTenantDbContextAsync(string tenantDomain)
    {
        var tenant = await _catalogContext.Tenants
            .FirstOrDefaultAsync(t => t.Domain == tenantDomain && t.Status == TenantStatus.Active);
        
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with domain {tenantDomain} not found or inactive");

        var tenantDatabaseName = $"clubmanagement_{tenant.SchemaName}";
        await EnsureTenantDatabaseAsync(tenantDomain, tenantDatabaseName);

        // Create connection string for tenant database
        var tenantConnectionString = _baseConnectionString.Replace("clubmanagement", tenantDatabaseName);
        
        var optionsBuilder = new DbContextOptionsBuilder<ClubManagementDbContext>();
        optionsBuilder.UseNpgsql(tenantConnectionString);
        
        return new ClubManagementDbContext(optionsBuilder.Options);
    }

    public async Task EnsureTenantDatabaseAsync(string tenantDomain, string databaseName)
    {
        try
        {
            // Try to create database (must be done outside a transaction)
            var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(_baseConnectionString);
            connectionStringBuilder.Database = "postgres"; // Connect to postgres db to create new db
            
            using var connection = new Npgsql.NpgsqlConnection(connectionStringBuilder.ToString());
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\";";
            await command.ExecuteNonQueryAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P04") // Database already exists
        {
            // Database already exists, continue
        }

        // Apply migrations to the tenant database
        var tenantConnectionString = _baseConnectionString.Replace("clubmanagement", databaseName);
        var optionsBuilder = new DbContextOptionsBuilder<ClubManagementDbContext>();
        optionsBuilder.UseNpgsql(tenantConnectionString);
        
        using var tenantContext = new ClubManagementDbContext(optionsBuilder.Options);
        await tenantContext.Database.MigrateAsync();
    }
}