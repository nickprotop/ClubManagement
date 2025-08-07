using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Shared.Models;

namespace ClubManagement.Infrastructure.Services;

public interface ITenantService
{
    Task<Tenant?> GetTenantByDomainAsync(string domain);
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId);
    Task<string> GetTenantDatabaseNameAsync(Guid tenantId);
    Task<string> GetTenantDatabaseNameAsync(string domain);
    Task<List<Tenant>> GetAllTenantsAsync();
}

public class TenantService : ITenantService
{
    private readonly CatalogDbContext _catalogContext;

    public TenantService(CatalogDbContext catalogContext)
    {
        _catalogContext = catalogContext;
    }

    public async Task<Tenant?> GetTenantByDomainAsync(string domain)
    {
        return await _catalogContext.Tenants
            .FirstOrDefaultAsync(t => t.Domain == domain && t.Status == TenantStatus.Active);
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
    {
        return await _catalogContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == TenantStatus.Active);
    }

    public async Task<string> GetTenantDatabaseNameAsync(Guid tenantId)
    {
        var tenant = await _catalogContext.Tenants.FindAsync(tenantId);
        return tenant?.SchemaName ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
    }

    public async Task<string> GetTenantDatabaseNameAsync(string domain)
    {
        var tenant = await _catalogContext.Tenants
            .FirstOrDefaultAsync(t => t.Domain == domain && t.Status == TenantStatus.Active);
        return tenant?.SchemaName ?? throw new InvalidOperationException($"Tenant with domain {domain} not found");
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        return await _catalogContext.Tenants
            .Where(t => t.Status == TenantStatus.Active)
            .ToListAsync();
    }
}