using System.Text.Json.Serialization;

namespace Shop.Infrastructure.Persistence.Configurations.Json;

public sealed class EntityConfigRoot
{
    [JsonPropertyName("entities")]
    public List<EntityConfig> Entities { get; set; } = new();
}

public sealed class EntityConfig
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("table")] public string Table { get; set; } = string.Empty;
    [JsonPropertyName("properties")] public Dictionary<string, PropertyConfig> Properties { get; set; } = new();
    [JsonPropertyName("owned")] public List<OwnedConfig> Owned { get; set; } = new();
}

public sealed class PropertyConfig
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("isKey")] public bool IsKey { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }
    [JsonPropertyName("maxLength")] public int? MaxLength { get; set; }
    [JsonPropertyName("columnType")] public string? ColumnType { get; set; }
    [JsonPropertyName("unique")] public bool Unique { get; set; }
    [JsonPropertyName("index")] public bool Index { get; set; }
}

public sealed class OwnedConfig
{
    [JsonPropertyName("navigation")] public string Navigation { get; set; } = string.Empty;
    [JsonPropertyName("backingField")] public string BackingField { get; set; } = string.Empty;
    [JsonPropertyName("table")] public string Table { get; set; } = string.Empty;
    [JsonPropertyName("properties")] public Dictionary<string, PropertyConfig> Properties { get; set; } = new();
}
