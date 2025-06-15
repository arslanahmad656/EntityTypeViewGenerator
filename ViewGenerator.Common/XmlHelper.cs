using System.Xml.Linq;
using System.Xml;
using ViewGenerator.Common.Models;

namespace ViewGenerator.Common;

public static class XmlHelper
{
    public static async Task<XDocument> ReadXmlDocumentFromFile(string path, CancellationToken cancellationToken)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var reader = XmlReader.Create(fs, new()
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        });

        return await XDocument.LoadAsync(reader, LoadOptions.SetLineInfo, cancellationToken).ConfigureAwait(false);
    }

    public static EntityType ParseEntityType(XElement entityTypeEl, ErrorHandler onError, bool abortOnError, CancellationToken cancellationToken)
    {
        var identifierValue = entityTypeEl.Attribute("Identifier")?.Value ?? throw new Exception(GetErrorLine(entityTypeEl, "Identifier"));
        var displayNameValue = entityTypeEl.Attribute("DisplayName_L1")?.Value;
        ConsolidationMode? consolidationMode = null;

        if (entityTypeEl.Attribute("ConsolidationMode")?.Value is string consolidationModeValue)
        {
            consolidationMode = Enum.Parse<ConsolidationMode>(consolidationModeValue);
            if (consolidationMode is not (ConsolidationMode.Default or ConsolidationMode.Merge))
            {
                throw new Exception($"Unsupported consolidation mode {consolidationModeValue} at {GeneralHelper.GetLineInfoRepresentation(entityTypeEl)}. Supported consolidation modes are {ConsolidationMode.Default} and {ConsolidationMode.Merge}");
            }
        }

        var properties = new Dictionary<string, EntityProperty>();
        foreach (var propertyEl in entityTypeEl.GetElements("Property"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var property = ParseEntityProperty(propertyEl);
                if (properties.ContainsKey(property.Identifier))
                {
                    throw new Exception($"Duplicate property {property.Identifier} at {GeneralHelper.GetLineInfoRepresentation(propertyEl)} from entity type at {GeneralHelper.GetLineInfoRepresentation(entityTypeEl)}.");
                }

                properties.Add(property.Identifier, property);
            }
            catch (Exception ex)
            {
                var exception = new Exception($"An error occurred while parsing a property at {GeneralHelper.GetLineInfoRepresentation(propertyEl)} from entity type at {GeneralHelper.GetLineInfoRepresentation(entityTypeEl)}.", ex);

                GeneralHelper.ThrowOrInvokeErrorAction(exception, onError, abortOnError);
            }
        }

        var entityType = new EntityType(identifierValue, displayNameValue, consolidationMode, properties);
        return entityType;
    }

    public static IEnumerable<XElement> GetDescendants(this XContainer element, string elementName)
    {
        XNamespace ns = "urn:schemas-usercube-com:configuration";
        return element.Descendants(ns + elementName);
    }

    public static IEnumerable<XElement> GetElements(this XContainer element, string elementName)
    {
        XNamespace ns = "urn:schemas-usercube-com:configuration";
        return element.Elements(ns + elementName);
    }

    private static EntityProperty ParseEntityProperty(XElement element)
    {
        var identifierValue = element.Attribute("Identifier")?.Value ?? throw new Exception(GetErrorLine(element, "Identifier"));
        var displayNameValue = element.Attribute("DisplayName_L1")?.Value;
        var isKeyValue = element.Attribute("IsKey")?.Value;
        var targetColumnIndexValue = element.Attribute("TargetColumnIndex")?.Value;
        var typeValue = element.Attribute("Type")?.Value;

        var identfier = identifierValue;
        var displayName = displayNameValue;
        var isKey = parseIsKey(isKeyValue);
        var targetColumnIndex = parseTargetColumnIndex(targetColumnIndexValue);
        var type = parseType(typeValue);

        return new(identfier, displayName, isKey, targetColumnIndex, type);

        bool? parseIsKey(string? value) => value switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => null
        };

        uint? parseTargetColumnIndex(string? value) => uint.TryParse(value, out var result) ? result : null;

        EntityPropertyType? parseType(string? value)
        {
            if (value is null)
            {
                return null;
            }

            if (int.TryParse(value, out var typeAsInt))
            {
                return (EntityPropertyType)typeAsInt;
            }
            else
            {
                return Enum.Parse<EntityPropertyType>(value);
            }
        }
    }

    private static string GetLineInfoRepresentation(XElement element) => GeneralHelper.GetLineInfoRepresentation(element);

    private static string GetErrorLine(XElement element, string attributeName) => $"Attribute {attributeName} was not found at line: {GetLineInfoRepresentation(element)}";
}
