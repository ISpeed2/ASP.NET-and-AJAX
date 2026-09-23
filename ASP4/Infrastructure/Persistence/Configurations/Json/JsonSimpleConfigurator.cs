using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Shop.Infrastructure.Persistence.Configurations.Json;

public static class JsonSimpleConfigurator
{
    public static void Apply(ModelBuilder modelBuilder, string jsonPath, Assembly domainAssembly)
    {
        if (!File.Exists(jsonPath))
            return;

        var root = JsonSerializer.Deserialize<EntityConfigRoot>(
            File.ReadAllText(jsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (root is null)
            return;

        foreach (var config in root.Entities)
        {
            var type = domainAssembly.GetTypes().FirstOrDefault(t => t.Name == config.Name)
                ?? throw new InvalidOperationException($"Type {config.Name} not found");

            var entity = modelBuilder.Entity(type);
            if (!string.IsNullOrWhiteSpace(config.Table))
                entity.ToTable(config.Table);

            foreach (var (propertyName, propertyConfig) in config.Properties)
            {
                var property = entity.Property(propertyName);
                if (propertyConfig.Required)
                    property.IsRequired();
                if (propertyConfig.MaxLength.HasValue)
                    property.HasMaxLength(propertyConfig.MaxLength.Value);
                if (!string.IsNullOrWhiteSpace(propertyConfig.ColumnType))
                    property.HasColumnType(propertyConfig.ColumnType);
                if (propertyConfig.IsKey)
                    entity.HasKey(propertyName);
                if (propertyConfig.Unique)
                    entity.HasIndex(propertyName).IsUnique();
                else if (propertyConfig.Index)
                    entity.HasIndex(propertyName);
            }
        }
    }
}
