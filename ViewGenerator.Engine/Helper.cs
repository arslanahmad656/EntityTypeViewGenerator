using System.Collections.ObjectModel;
using System.Text;
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

    public static string GetViewQueryForEntityType(EntityType entityType, bool includeIdColumn)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"""
            CREATE VIEW [zz_{entityType.Identifier}] AS
            SELECT
            """);

        var idColumn = includeIdColumn ? "Id AS [Id]" : string.Empty;

        var projectionsList = new[] { idColumn }.Concat(entityType.Properties.Values
            .Select(GetProjectionForEntityProperty)
            .Where(p => !string.IsNullOrEmpty(p)));

        var projections = string.Join($",{Environment.NewLine}", projectionsList.Select(p => $"   {p}"));

        sb.AppendLine(projections);

        sb.AppendLine($"FROM UR_Resources");
        
        var filter = GetFilterForViewCreation(entityType);
        sb.AppendLine($"""
            WHERE
               {filter}
            """);

        return sb.ToString();
    }

    public static string GetSqlQueryToDropView(string entityTypeIdentifier) => $"DROP VIEW [zz_{entityTypeIdentifier}];";

    public static string GetSqlQueryToCheckTheViewExists(string entityTypeIdentifier) => $"SELECT OBJECT_ID(N'zz_{entityTypeIdentifier}', N'V') ";

    private static string GetFilterForViewCreation(EntityType entityType)
        => $"""
        [Type] = (SELECT Id FROM UM_EntityTypes WHERE Identifier = '{entityType.Identifier}')
           AND ValidTo = CONVERT(DATETIME2(2),'9999-12-31 23:59:59.999',121)
        """;

    private static string GetProjectionForEntityProperty(EntityProperty entityProperty)
    {
        if (entityProperty.TargetColumnIndex is null)
        {
            return string.Empty;
        }

        var columnPrefix = entityProperty.Type == EntityPropertyType.ForeignKey ? "I" : "C";
        var base32ColumnColumnIndex = entityProperty.Base32ColumnIndex;

        var projectction = $"{columnPrefix}{base32ColumnColumnIndex} AS [{entityProperty.Identifier}]";
        return projectction;
    }
}
