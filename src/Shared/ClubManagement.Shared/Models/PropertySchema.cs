namespace ClubManagement.Shared.Models;

public class PropertySchema
{
    public List<PropertyDefinition> Properties { get; set; } = new();
}

public class PropertyDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public PropertyType Type { get; set; } = PropertyType.Text;
    public bool Required { get; set; } = false;
    public List<string> Options { get; set; } = new();
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public int? MaxLength { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
}

public enum PropertyType
{
    Text,
    Number,
    Boolean,
    Select,
    MultiSelect,
    Date,
    DateTime,
    Time,
    Email,
    Phone,
    Url,
    File,
    TextArea,
    Currency
}