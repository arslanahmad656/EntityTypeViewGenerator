using System.Collections.ObjectModel;
using ViewGenerator.Common;
using ViewGenerator.Common.Models;

namespace ViewGenerator.Engine;

public static class Helper
{
    public static EntityType MergeProperties(IEnumerable<EntityType> entityTypes)
    {
        if (!entityTypes.Any())
        {
            throw new InvalidOperationException($"Cannot merge properties for empty list of EntityTypes.");
        }

        Validators.ValidateEntityTypesIdentifierConsistency(entityTypes);

        var allEntityProperties = entityTypes
            .AsParallel()
            .Select(et => et.Properties)
            .SelectMany(d => d.Values);

        //var finalEntityProperties = allEntityProperties.ToHashSet();  // It won't give us the exact duplicate property.

        var anyEntityType = entityTypes.First();
        var finalEntityProperties = new Dictionary<string, EntityProperty>();
        foreach (var entityProperty in allEntityProperties)
        {
            if (finalEntityProperties.ContainsKey(entityProperty.Identifier))
            {
                throw new Exception($"Duplicate property identifier {entityProperty.Identifier} found for entity type {anyEntityType.Identifier}.");
            }

            finalEntityProperties.Add(entityProperty.Identifier, entityProperty);
        }

        var finalEntityType = anyEntityType with
        {
            Properties = new ReadOnlyDictionary<string, EntityProperty>(finalEntityProperties),
        };

        return finalEntityType;
    }
}
