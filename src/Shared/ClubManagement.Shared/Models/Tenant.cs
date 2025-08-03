namespace ClubManagement.Shared.Models;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public BrandingSettings Branding { get; set; } = new();
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Basic;
    public DateTime? TrialEndsAt { get; set; }
    public int MaxMembers { get; set; } = 100;
    public int MaxFacilities { get; set; } = 10;
    public int MaxStaff { get; set; } = 5;
}

public enum TenantStatus
{
    Active,
    Suspended,
    Trial,
    Cancelled
}

public enum SubscriptionPlan
{
    Basic,
    Premium,
    Enterprise
}

public class BrandingSettings
{
    public string PrimaryColor { get; set; } = "#1976d2";
    public string SecondaryColor { get; set; } = "#dc004e";
    public string LogoUrl { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
}