using ViewGenerator.Common.Models;

namespace ViewGenerator.Engine;

static class Validators
{
    public static void ValidateConfRootFolder(string rootFolder)
    {
        if (!Directory.Exists(rootFolder))
        {
            throw new DirectoryNotFoundException($"The root folder {rootFolder} does not exist.");
        }
    }

    public static void ValidateIncludeAndSkipViews(string[] includeViews, string[] skipViews)
    {
        if (includeViews.Length > 0 && skipViews.Length > 0)
        {
            throw new ArgumentException("Cannot include and skip views at the same time.");
        }
    }

    public static void ValidateEntityTypesIdentifierConsistency(IEnumerable<EntityType> entityTypes)
    {
        using var enumerator = entityTypes.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        var currentIdentifier = enumerator.Current.Identifier;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.Identifier != currentIdentifier)
            {
                throw new ArgumentException("Entity types identifiers are not consistent. A single identifier is expected for each EntityType.");
            }
        }
    }
}
