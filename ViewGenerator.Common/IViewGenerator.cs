using ViewGenerator.Common.Models;

namespace ViewGenerator.Common;

public interface IViewGenerator
{
    event EventHandler<Exception>? ErrorOccurred;
    event EventHandler<EntityType>? EntityTypeIgnored;

    Task<string> GetSqlQueryForViews(bool realoadEntityTypes, CancellationToken cancellationToken);
    
    Task<string> GetSqlQueryForView(string entityTypeIdentifier, bool realoadEntityTypes, CancellationToken cancellationToken);

    Task GenerateViews(CancellationToken cancellationToken);

    Task<List<EntityType>> GetEntityTypes(bool reloadEntityTypes, CancellationToken cancellationToken);

    Task<EntityType> GetEntityType(string entityTypeIdentifier, bool reloadEntityTypes, CancellationToken cancellationToken);
}
