namespace ClubManagement.Shared.Models;

public class FacilityType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public PropertySchema PropertySchema { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}