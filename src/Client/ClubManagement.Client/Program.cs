using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ClubManagement.Client;
using ClubManagement.Client.Services;
using Heron.MudCalendar;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to use API base address - use singleton so all services share the same instance
var apiBaseAddress = builder.Configuration.GetValue<string>("ApiBaseAddress") ?? "https://localhost:4001";
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

// Add MudBlazor services
builder.Services.AddMudServices();

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMemberPermissionService, MemberPermissionService>();
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPermissionsService, PermissionsService>();
builder.Services.AddScoped<IHardwareService, HardwareService>();
builder.Services.AddScoped<IHardwareTypeService, HardwareTypeService>();
builder.Services.AddScoped<IHardwareAssignmentService, HardwareAssignmentService>();

// Add authentication
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
