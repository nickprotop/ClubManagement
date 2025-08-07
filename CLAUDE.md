# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Commands

### Build and Run
```bash
# Build the entire solution
dotnet build

# Start infrastructure only (PostgreSQL, Redis, MinIO)
./scripts/start-infra.sh

# Run API (Terminal 1)
cd src/Api/ClubManagement.Api
dotnet run

# Run Client (Terminal 2)
cd src/Client/ClubManagement.Client
dotnet run

# Full Docker stack
docker-compose up -d
```

### Database Operations
```bash
# Add migration
dotnet ef migrations add MigrationName --project src/Infrastructure/ClubManagement.Infrastructure --startup-project src/Api/ClubManagement.Api

# Update database
dotnet ef database update --project src/Infrastructure/ClubManagement.Infrastructure --startup-project src/Api/ClubManagement.Api

# Drop database (development only)
dotnet ef database drop --project src/Infrastructure/ClubManagement.Infrastructure --startup-project src/Api/ClubManagement.Api
```

### Setup and Configuration
```bash
# Interactive setup (prompts for configuration)
./scripts/setup.sh

# Quick setup with defaults (development)
./scripts/setup.sh --quick

# Generate secure JWT secret
./scripts/generate-jwt-secret.sh
```

## Architecture Overview

### Multi-Tenant Database-per-Tenant Design
- **Catalog Database**: Contains `Tenants` table and shared infrastructure (`clubmanagement`)
- **Tenant Databases**: Each tenant gets isolated database (e.g., `clubmanagement_demo_club`, `clubmanagement_tenant_abc`)
- All tenant-specific data (users, members, facilities, events) lives in separate tenant database
- Database switching happens at request level using `TenantDbContextFactory`

### Clean Architecture Layers
```
Client (Blazor WASM) → API (Controllers) → Application (CQRS) → Infrastructure (Data) → Domain (Entities)
```

**Domain** (`src/Domain/`): Core business entities and interfaces
**Shared** (`src/Shared/`): DTOs and models shared between API and Client
**Application** (`src/Application/`): Business logic (currently minimal, using services pattern)
**Infrastructure** (`src/Infrastructure/`): Data access, authentication, external services
**API** (`src/Api/`): ASP.NET Core Web API controllers
**Client** (`src/Client/`): Blazor WASM frontend with MudBlazor UI

### Key Services and Patterns

**Multi-Tenancy Implementation:**
- `TenantService`: Resolves tenant by domain, provides tenant metadata
- `TenantDbContextFactory`: Creates DbContext instances for specific tenant databases
- Controllers use tenant resolution and database context switching in each request

**Authentication System:**
- JWT-based with separate access/refresh tokens
- `JwtService`: Token generation and validation
- `PasswordService`: PBKDF2 hashing with separate salt storage (100k iterations, SHA256)
- All API controllers use `[Authorize]` attribute

**Data Access Pattern:**
- Entity Framework Core with PostgreSQL
- Repository pattern via DbContext
- JSON columns for dynamic/flexible data (PropertySchema, CustomFields, etc.)
- Audit fields on BaseEntity (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)

### Dynamic Property System
Facilities and Hardware use dynamic property schemas:
- `PropertySchema` defines available properties per type
- `Properties` dictionary stores actual values per instance
- Supports validation, required fields, select options, multi-select, etc.

### Demo Data and Seeding
- `DbSeeder` runs on startup, creates demo tenant with sample data
- Demo credentials: `demo.localhost` / `admin@demo.localhost` / `Admin123!`
- Creates sample facility types, facilities, and admin user with proper password hashing

## Configuration System

**Flow:** `.env.sample` → `.env` (user edits) → `config.json` (generated) → API loads config

**Key Files:**
- `.env.sample`: Template with all environment variables
- `.env`: User's actual configuration (git-ignored)
- `src/Api/ClubManagement.Api/config.sample.json`: JSON template with `{{PLACEHOLDER}}` variables
- `src/Api/ClubManagement.Api/config.json`: Generated JSON config (git-ignored)

**Configuration Loading:**
- API requires `config.json` to exist or exits with helpful error message
- Docker vs local mode detection affects URL binding
- Tenant database schema switching happens per request

## Security Implementation

**Password Security:**
- PBKDF2 with SHA256, 100,000 iterations
- 32-byte random salt stored separately
- `PasswordService.HashPasswordWithSeparateSalt()` for new passwords
- `PasswordService.VerifyPassword()` with constant-time comparison

**Multi-Tenant Security:**
- Complete data isolation via separate databases
- No shared data between tenants except catalog database tenant registry
- Database name validation prevents injection attacks

**API Security:**
- All controllers require `[Authorize]`
- JWT tokens contain tenant information
- CORS configured for specific client origins

## Development Workflow

### Adding New Features
1. **Models**: Add to `src/Shared/Models/` if shared, or `src/Domain/` for domain entities
2. **DTOs**: Add request/response DTOs to `src/Shared/DTOs/`
3. **Database**: Add DbSet to `ClubManagementDbContext`, create migration
4. **API**: Create controller in `src/Api/Controllers/` with `[Authorize]`
5. **Client**: Add service in `src/Client/Services/`, create pages/components
6. **Navigation**: Update `NavMenu.razor` if needed

### Database Changes
- Always create migrations for schema changes
- Use `dotnet ef migrations add` from project root
- Test with demo data seeding
- Consider multi-tenant impact (schema isolation)

### Client-Side Services Pattern
- Services inject `IApiService` for HTTP calls
- Authentication handled by `AuthService` with JWT storage
- Services return `ApiResponse<T>` for consistent error handling
- MudBlazor components used throughout for Material Design

### Important Implementation Details

**Multi-Tenant Request Flow:**
1. Client sends request with tenant domain
2. API resolves tenant via `TenantService.GetTenantByDomainAsync()`
3. Controller creates tenant database context via `TenantDbContextFactory.CreateTenantDbContextAsync()`
4. All subsequent DB operations use tenant-specific database context
5. Response includes tenant-isolated data only

**Password Management:**
- Never store plain text passwords
- Use `PasswordService.HashPasswordWithSeparateSalt()` for new users
- Use `PasswordService.VerifyPassword(password, hash, salt)` for login
- Set `PasswordChangedAt` timestamp on password updates

**Dynamic Properties System:**
- `FacilityType` and `HardwareType` define `PropertySchema`
- `Facility` and `Hardware` store values in `Properties` JSON column
- Client dynamically renders forms based on schema definitions
- Supports text, number, boolean, select, multi-select property types

## Ports and Services
- **API**: http://localhost:4000, https://localhost:4001
- **Client**: http://localhost:4002, https://localhost:4003
- **PostgreSQL**: localhost:4004
- **MinIO**: localhost:4005 (API), localhost:4006 (Console)
- **Redis**: localhost:4007

## Demo Access
- **Domain**: `demo.localhost`
- **Database**: `clubmanagement_demo_club`

**Demo Accounts:**
- **Admin**: `admin@demo.localhost` / `Admin123!` (full system access)
- **Member**: `member@demo.localhost` / `Member123!` (member portal access, Basic tier)
- **Coach**: `coach@demo.localhost` / `Coach123!` (coaching and training access, Premium membership, can register for events)

## CRITICAL: Multi-Tenant Database Context Requirements

⚠️ **SECURITY CRITICAL** ⚠️

**EVERY API endpoint that accesses tenant-specific data MUST implement proper tenant database context switching. Failure to do this will result in:**
- Data leakage between tenants
- Authentication failures  
- Authorization bypasses
- Complete security breach

### **MANDATORY Pattern for ALL Tenant-Specific Endpoints**

```csharp
[HttpGet]
[Authorize] // All tenant endpoints MUST have [Authorize]
public async Task<ActionResult<ApiResponse<T>>> YourEndpoint()
{
    try
    {
        // STEP 1: Extract user and tenant from JWT token
        var userId = this.GetCurrentUserId();
        var tenantId = this.GetCurrentTenantId();
        
        // STEP 2: Resolve tenant and validate
        var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
        if (tenant == null)
            return BadRequest(ApiResponse<T>.ErrorResult("Invalid tenant"));
            
        // STEP 3: CRITICAL - Create tenant-specific database context BEFORE any DB operations
        using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);
        
        // STEP 4: Now safe to perform tenant-specific operations using tenantContext
        var result = await tenantContext.YourTenantTable.ToListAsync();
        return Ok(ApiResponse<T>.SuccessResult(result));
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(ApiResponse<T>.ErrorResult($"Unauthorized: {ex.Message}"));
    }
    catch (Exception ex)
    {
        return StatusCode(500, ApiResponse<T>.ErrorResult($"Error: {ex.Message}"));
    }
}
```

### **CRITICAL Rules - ALWAYS Follow These:**

1. **NEVER access shared `_context` (DbContext) for tenant-specific data - always use tenant context**
2. **ALWAYS use `this.GetCurrentTenantId()` to get tenant from JWT**
3. **ALWAYS validate tenant exists before creating tenant context** 
4. **ALWAYS apply `[Authorize]` attribute to tenant-specific endpoints**
5. **ALWAYS create tenant context BEFORE any database queries**
6. **ALWAYS use `using` statement for tenant context to ensure proper disposal**

### **Endpoints That MUST Have Tenant Context Switching:**

- Any endpoint accessing: Users, Members, Events, Facilities, Hardware, Registrations, etc.
- Any endpoint with `[Authorize]` attribute
- Any endpoint that queries tenant-specific data

### **Endpoints That DON'T Need Tenant Context Switching:**

- Health checks (`/health`)
- Public authentication endpoints (login - but these handle tenants differently)
- Endpoints accessing only catalog database data (Tenants table)

### **Required Services in Controller:**

```csharp
public class YourController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory; // REQUIRED for tenant context creation
    private readonly ITenantService _tenantService; // REQUIRED for tenant resolution
    
    public YourController(ITenantDbContextFactory tenantDbContextFactory, ITenantService tenantService)
    {
        _tenantDbContextFactory = tenantDbContextFactory; // MUST inject this
        _tenantService = tenantService; // MUST inject this
    }
}
```

### **Testing Tenant Isolation:**

When implementing new endpoints, ALWAYS test:
1. User from Tenant A cannot access Tenant B's data
2. Database context switching works correctly
3. JWT token contains valid tenant_id claim
4. Proper error handling for invalid tenants
5. Tenant context disposal works properly

**⚠️ REMEMBER: Multi-tenant security is CRITICAL. One missing database context switch can expose ALL tenant data. ALWAYS review tenant context switching in code reviews.**

## Authorization System (Hybrid Approach)

**Use this approach for ALL new controllers and features requiring authorization.**

### Core Components

**1. Authorization Service Interface:**
```csharp
public interface I[Feature]AuthorizationService
{
    Task<[Feature]Permissions> GetPermissionsAsync(Guid userId, Guid? resourceId = null);
    Task<bool> CanPerformActionAsync(Guid userId, [Feature]Action action, Guid? resourceId = null);
    Task<AuthorizationResult> CheckAuthorizationAsync(Guid userId, [Feature]Action action, Guid? resourceId = null);
}
```

**2. Permission Result Object:**
```csharp
public class [Feature]Permissions
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    // Add feature-specific permissions
    public string[] Restrictions { get; set; } = Array.Empty<string>();
    public string[] ReasonsDenied { get; set; } = Array.Empty<string>();
}
```

**3. Actions Enum:**
```csharp
public enum [Feature]Action
{
    View, Create, Edit, Delete
    // Add feature-specific actions
}
```

**4. Authorization Result:**
```csharp
public class AuthorizationResult
{
    public bool Succeeded { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
    public static AuthorizationResult Success() => new() { Succeeded = true };
    public static AuthorizationResult Failed(params string[] reasons) => new() { Succeeded = false, Reasons = reasons };
}
```

### Implementation Pattern

**Controller Integration (Hybrid Approach):**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Basic authentication required
public class [Feature]Controller : ControllerBase
{
    private readonly I[Feature]AuthorizationService _authService;
    
    // GET permissions endpoint for frontend
    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<[Feature]Permissions>> GetPermissions(Guid id)
    {
        var userId = GetCurrentUserId();
        var permissions = await _authService.GetPermissionsAsync(userId, id);
        return Ok(permissions);
    }
    
    // Hybrid approach: Attribute + Service
    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff,Admin,SuperAdmin")] // Quick role filter
    public async Task<ActionResult> Delete[Feature](Guid id)
    {
        var userId = GetCurrentUserId();
        var authResult = await _authService.CheckAuthorizationAsync(userId, [Feature]Action.Delete, id);
        
        if (!authResult.Succeeded)
            return Forbid(string.Join(", ", authResult.Reasons));
            
        // Proceed with operation...
    }
}
```

**Service Implementation Strategy:**
```csharp
public class [Feature]AuthorizationService : I[Feature]AuthorizationService
{
    public async Task<[Feature]Permissions> GetPermissionsAsync(Guid userId, Guid? resourceId = null)
    {
        var user = await GetUserWithRoleAsync(userId);
        var resource = resourceId.HasValue ? await GetResourceAsync(resourceId.Value) : null;
        
        return user.Role switch
        {
            UserRole.Member => GetMemberPermissions(user, resource),
            UserRole.Staff => GetStaffPermissions(user, resource),
            UserRole.Instructor => GetInstructorPermissions(user, resource),
            UserRole.Coach => GetCoachPermissions(user, resource),
            UserRole.Admin => GetAdminPermissions(user, resource),
            UserRole.SuperAdmin => GetSuperAdminPermissions(user, resource),
            _ => new [Feature]Permissions() // No permissions
        };
    }
    
    private [Feature]Permissions GetCoachPermissions(User user, [Resource]? resource)
    {
        var isOwner = resource?.CreatedBy == user.Id.ToString();
        var canModify = isOwner && IsModificationAllowed(resource);
        
        return new [Feature]Permissions
        {
            CanView = true,
            CanCreate = true,
            CanEdit = canModify,
            CanDelete = canModify && !HasDependencies(resource),
            Restrictions = GetRestrictions(user, resource)
        };
    }
}
```

**Frontend Integration:**
```typescript
// Get permissions upfront
const permissions = await api.get(`/[feature]/${id}/permissions`);

// Show/hide UI elements based on permissions
{permissions.canEdit && <EditButton />}
{permissions.canDelete && <DeleteButton />}
```

### Key Benefits
1. **Performance**: Frontend gets all permissions in one call
2. **User Experience**: UI shows only available actions  
3. **Security**: Double-checked (attribute + service)
4. **Maintainability**: Business logic centralized in service
5. **Testability**: Service can be unit tested easily
6. **Flexibility**: Handles complex context-aware scenarios
7. **Scalability**: Easy to extend for new roles and permissions

### Authorization Service Registration
```csharp
// In Program.cs or Startup.cs
builder.Services.AddScoped<I[Feature]AuthorizationService, [Feature]AuthorizationService>();
```

**Always implement this pattern when adding new controllers or features requiring role-based access control.**

## MudBlazor Component Binding Patterns

**IMPORTANT**: Use these correct binding patterns for MudBlazor 8+ components.

### Radio Groups
```razor
<!-- CORRECT -->
<MudRadioGroup @bind-Value="selectedOption">
    <MudRadio Value="OptionEnum.Option1">Option 1</MudRadio>
    <MudRadio Value="OptionEnum.Option2">Option 2</MudRadio>
</MudRadioGroup>

<!-- WRONG - Do not use these patterns -->
<MudRadioGroup SelectedOption="..." SelectedOptionChanged="...">
<MudRadio Option="..." T="...">
```

**Key Points:**
- Use `@bind-Value` on `MudRadioGroup` (not `SelectedOption`)
- Use `Value` attribute on `MudRadio` (not `Option`)
- Do not mix `@bind-Value` with `SelectedOptionChanged` (causes duplicate parameter errors)

### Checkboxes
```razor
<!-- CORRECT -->
<MudCheckBox @bind-Value="booleanField" Label="My Checkbox" />

<!-- WRONG - Do not use -->
<MudCheckBox @bind-Checked="booleanField" />
<MudCheckBox T="bool" @bind-Checked="..." />
```

**Key Points:**
- Use `@bind-Value` (not `@bind-Checked`)
- MudBlazor checkboxes use `Value` binding, not `Checked` like HTML checkboxes

### Dictionary Binding Issues
```razor
<!-- PROBLEMATIC - Dictionary binding can be unreliable -->
<MudCheckBox @bind-Value="@dictionary[key]" />

<!-- PREFERRED - Individual fields for fixed sets -->
<MudCheckBox @bind-Value="_sunday" Label="Sunday" />
<MudCheckBox @bind-Value="_monday" Label="Monday" />
```

**Best Practices:**
- Avoid dictionary binding in loops - use individual boolean fields for fixed sets
- Direct field binding is more reliable than dictionary access
- Keep binding patterns simple rather than complex property wrappers

## Documentation
- **CHANGELOG.md**: All notable changes to the Club Management Platform are documented in CHANGELOG.md following Keep a Changelog format
- **Feature Tracking**: When adding new features, update CHANGELOG.md with Added/Changed/Fixed sections