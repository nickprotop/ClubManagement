using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services;

public interface ITenantService
{
    Task<Tenant?> GetTenantByDomainAsync(string domain);
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId);
    Task<string> GetTenantSchemaAsync(Guid tenantId);
    Task EnsureTenantSchemaExistsAsync(string schemaName);
}

public class TenantService : ITenantService
{
    private readonly ClubManagementDbContext _context;

    public TenantService(ClubManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetTenantByDomainAsync(string domain)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Domain == domain && t.Status == TenantStatus.Active);
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == TenantStatus.Active);
    }

    public async Task<string> GetTenantSchemaAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        return tenant?.SchemaName ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
    }

    public async Task EnsureTenantSchemaExistsAsync(string schemaName)
    {
        var sql = $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"";
        await _context.Database.ExecuteSqlRawAsync(sql);
    }
}