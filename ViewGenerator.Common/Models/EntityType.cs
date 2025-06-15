namespace ViewGenerator.Common.Models;

public record EntityType(
    string Identifier,
    string? DisplayName,
    ConsolidationMode? ConsolidationMode,
    IReadOnlyDictionary<string, EntityProperty> Properties
    );
