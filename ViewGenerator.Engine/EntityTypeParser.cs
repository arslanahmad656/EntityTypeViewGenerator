using System.Xml;
using System.Xml.Linq;
using ViewGenerator.Common;
using ViewGenerator.Common.Models;
using ElementFileInfo = (System.Collections.Generic.List<System.Xml.Linq.XElement> Elements, string File);

namespace ViewGenerator.Engine;

/// <summary>
/// Helper methods for the <see cref="Generator"/> class.
/// </summary>
/// <remarks>
/// The methods of this class do not do any validation of the input parameters. It assumes that the parameters received are already validated.
/// </remarks>
static class EntityTypeParser
{
    /// <summary>
    /// Gets the EntityType xml elements. This method assumes that either <paramref name="includeViews"/> or <paramref name="skipViews"/> is specified but not both.
    /// </summary>
    /// <param name="rootFolder">Root conf folder.</param>
    /// <param name="includeViews">If this array has some values, only those EntityTypes will be included. If this parameter is provided, the <paramref name="includeViews"/> will be ignored.</param>
    /// <param name="skipViews">If this array has some values, the entity types with these names will be skipped. This parameter will only considered if <paramref name="includeViews"/> is not provided.</param>
    /// <param name="onError">The action to invoke in case of error.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="EntityType"/>s.</returns>
    /// <remarks>
    /// If both <paramref name="includeViews"/> and <paramref name="skipViews"/> are provided, only <paramref name="includeViews"/> will be considered.
    /// </remarks>
    public static async Task<List<EntityType>> GetEntityTypes(ViewEngineSettings viewEngineSettings, ErrorHandler onError, CancellationToken cancellationToken)
    {
        // Step 1:
        // Read all xml files from the conf folder and extract all EntityTypes as XElements. Also get the file names from them. File names will help for diagnostics.
        var allDictionaries = new List<Dictionary<string, ElementFileInfo>>();
        foreach (var file in Directory.EnumerateFiles(viewEngineSettings.ConfRootFolder, "*.xml", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var xDocument = await XmlHelper.ReadXmlDocumentFromFile(file, cancellationToken).ConfigureAwait(false);
                var entityTypeIdenfiersToElements = GetEntityTypeElements(xDocument, viewEngineSettings.IncludeViews, viewEngineSettings.SkipViews, onError, viewEngineSettings.ErrorAction is ErrorAction.Abort, cancellationToken)
                    .ToDictionary(x => x.Key, x => new ElementFileInfo(x.Value, file));
                allDictionaries.Add(entityTypeIdenfiersToElements);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var exception = new Exception($"Error occurred while processing the file {file}. See inner exception for more details.", ex);
                GeneralHelper.ThrowOrInvokeErrorAction(exception, onError, viewEngineSettings.ErrorAction is ErrorAction.Abort);
            }
        }

        // Step 2:
        // Since there can be multiple EntityTypes with the same identifier (in different files), we need to merge them.
        var entityTypesDictionaryFromAllFiles = new Dictionary<string, ElementFileInfo>();
        foreach (var dictionary in allDictionaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var kvp in dictionary)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!entityTypesDictionaryFromAllFiles.TryGetValue(kvp.Key, out var existingElements))
                {
                    existingElements = new ElementFileInfo([], kvp.Value.File);
                    entityTypesDictionaryFromAllFiles.Add(kvp.Key, existingElements);
                }

                existingElements.Elements.AddRange(kvp.Value.Elements);
            }
        }

        // Step 3:
        // Parse each XElement representing an EntityType and convert it to an EntityType object.
        // Just like step2, this step will give us multiple EntityTypes with the same identifier (from the same or different files).
        var allEntityTypesKeyedByIdentifier = new Dictionary<string, List<EntityType>>();
        foreach (var kvp in entityTypesDictionaryFromAllFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach (var entityTypeElement in kvp.Value.Elements)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var entityType = XmlHelper.ParseEntityType(entityTypeElement, onError, viewEngineSettings.ErrorAction is ErrorAction.Abort, cancellationToken);
                        if (!allEntityTypesKeyedByIdentifier.TryGetValue(entityType.Identifier, out var entityTypesWithCurrentIdentifier))
                        {
                            entityTypesWithCurrentIdentifier = [];
                            allEntityTypesKeyedByIdentifier.Add(entityType.Identifier, entityTypesWithCurrentIdentifier);
                        }

                        entityTypesWithCurrentIdentifier.Add(entityType);
                    }
                    catch (Exception ex)
                    {
                        var exception = new Exception($"An error occurred while processing the entity type {kvp.Key}. The error occurred inside file {kvp.Value.File} at line {GeneralHelper.GetLineInfoRepresentation(entityTypeElement)}. See inner exception for details.", ex);
                        GeneralHelper.ThrowOrInvokeErrorAction(exception, onError, viewEngineSettings.ErrorAction is ErrorAction.Abort);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var exception = new Exception($"An error occurred while processing the entity type {kvp.Key} inside file {kvp.Value.File}. See the inner exception for details.", ex);
                GeneralHelper.ThrowOrInvokeErrorAction(exception, onError, viewEngineSettings.ErrorAction is ErrorAction.Abort);
            }
        }

        // Step 4:
        // Merge the properties from different EntityTypes with the same identifier into a single EntityType.
        var finalEntityTypes = new List<EntityType>();
        foreach (var kvp in allEntityTypesKeyedByIdentifier)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                EntityType finalEntityType;
                try
                {
                    finalEntityType = Helper.MergeProperties(kvp.Value);
                }
                catch (InvalidOperationException)
                {
                    var anyEntityType = kvp.Value[0];   // the list is bound to have at least one entity type per identifier.
                    finalEntityType = anyEntityType with { };   // Create a copy of the entity type
                }

                finalEntityTypes.Add(finalEntityType);
            }
            catch (Exception ex)
            {
                var exception = new Exception($"An error occurred while processing the entity type {kvp.Key}. See the inner exception for details.", ex);
                GeneralHelper.ThrowOrInvokeErrorAction(exception, onError, viewEngineSettings.ErrorAction is ErrorAction.Abort);
            }
        }


        // The resulting entity types.
        return finalEntityTypes;
    }

    private  static Dictionary<string, List<XElement>> GetEntityTypeElements(XDocument xDocument, string[] includeViews, string[] skipViews, ErrorHandler onError, bool abortOnError, CancellationToken cancellationToken)
    {
        var resultEntityTypeElements = new Dictionary<string, List<XElement>>();
        var entityTypeElementsInDocument = xDocument.Descendants(Constants.EntityTypeTagName);

        foreach (var entityTypeElement in entityTypeElementsInDocument)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var lineInfo = (IXmlLineInfo)entityTypeElement;
                var entityTypeIdentifier = entityTypeElement.Attribute(Constants.IdentifierAttributeName)?.Value ?? throw new Exception($"Entity type identifier is missing from the EntityType. Line {GeneralHelper.GetLineInfoRepresentation(lineInfo)}.");
                if (includeViews.Length > 0 && !includeViews.Contains(entityTypeIdentifier, StringComparer.InvariantCulture))
                {
                    continue;
                }

                if (skipViews.Length > 0 && skipViews.Contains(entityTypeIdentifier, StringComparer.InvariantCulture))
                {
                    continue;
                }

                if (!resultEntityTypeElements.TryGetValue(entityTypeIdentifier, out var fetchedEntityTypeElements))
                {
                    fetchedEntityTypeElements = [];
                }

                fetchedEntityTypeElements.Add(entityTypeElement);
            }
            catch (Exception ex)
            {
                var exception = new Exception($"An error occurred while processing the entity type at line {GeneralHelper.GetLineInfoRepresentation(entityTypeElement)}. See the inner exception for details.", ex);

                GeneralHelper.ThrowOrInvokeErrorAction(exception, onError, abortOnError);
            }
        }

        return resultEntityTypeElements;
    }

    
}
