using ViewGenerator.Common.Models;

namespace ViewGenerator.Common;

public interface IViewGenerator
{
    event EventHandler<Exception>? ErrorOccurred;

    Task<string> GetSqlQueryForViews(CancellationToken cancellationToken);
    
    Task GenerateViews(CancellationToken cancellationToken);

    Task<List<EntityType>> GetEntityTypes(CancellationToken cancellationToken);
}
