using System.Text;
using ViewGenerator.Common.Models;

namespace ViewGenerator.App;

static class GeneralHelper
{
    public static string GetFullExceptionMessage(this Exception exception)
    {
        var sb = new StringBuilder();
        Exception? ex = exception;
        while (ex != null)
        {
            sb.AppendLine(ex.Message);
            ex = ex.InnerException;
        }

        return sb.ToString();
    }

    public static string GetEntityTypeDetailsString(this EntityType entityType)
    {
        var sb = new StringBuilder();

        var et = entityType with
        {
            Properties = entityType.Properties.Keys.Order().ToDictionary(k => k, k => entityType.Properties[k])
        };

        var propertyDetails = et.Properties.Values.Select(p => p.GetEntityPropertyDetailsString()).ToList();

        sb.AppendLine($"Details of the entity type {et.Identifier}:");
        sb.AppendLine($"\tName: {(entityType.DisplayName ?? "NA")}, Total Properties: {et.Properties.Count}.");
        sb.AppendLine($"Property Details:");

        propertyDetails.ForEach(d => sb.AppendLine(d));

        return sb.ToString();
    }

    private static string GetEntityPropertyDetailsString(this EntityProperty entityProperty)
    {
        var sb = new StringBuilder();

        sb.Append(entityProperty.Identifier);

        var space = false;
        if (!string.IsNullOrEmpty(entityProperty.DisplayName))
        {
            sb.Append($" ({entityProperty.DisplayName})");
            space = true;
        }

        if (entityProperty.IsKey.HasValue)
        {
            if (space)
            {
                sb.Append(' ');
                space = false;
            }
            if (entityProperty.IsKey is true)
            {
                sb.Append($"[Key Property]");
            }
            else
            {
                sb.Append($"[Not a Key Property]");
            }
        }

        sb.AppendLine();

        var tab = true;
        var comma = false;
        if (entityProperty.Type.HasValue)
        {
            if (tab)
            {
                sb.Append('\t');
                tab = false;
            }

            sb.Append($"Type: {entityProperty.Type.Value}");
            comma = true;
            space = true;
        }    

        if (entityProperty.TargetColumnIndex.HasValue)
        {
            if (tab)
            {
                sb.Append('\t');
                tab = false;
            }

            if (comma)
            {
                sb.Append(',');
            }

            if (space)
            {
                sb.Append(' ');
                space = false;
            }

            sb.Append($"Target Column Index: {entityProperty.TargetColumnIndex.Value} ({entityProperty.Base32ColumnIndex})");
        }

        return sb.ToString();
    }
}
