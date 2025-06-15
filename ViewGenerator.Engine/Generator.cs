using Microsoft.Data.SqlClient;
using ViewGenerator.Common;
using ViewGenerator.Common.Models;

namespace ViewGenerator.Engine;

public class Generator : IViewGenerator, IDisposable
{
    private readonly SqlConnection _connection;
    private string? queryForViews;

    public event EventHandler<Exception>? ErrorOccurred;

    public ViewEngineSettings Settings { get; }

    public Generator(ViewEngineSettings settings)
    {
        Settings = settings;

        _connection = new SqlConnection(settings.ConnectionString);
        _connection.Open();

        Validators.ValidateConfRootFolder(settings.ConfRootFolder);
        Validators.ValidateIncludeAndSkipViews(settings.IncludeViews, settings.SkipViews);
    }

    public Task GenerateViews(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetSqlQueryForViews(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<List<EntityType>> GetEntityTypes(CancellationToken cancellationToken)
    {
        var entityTypes = await EntityTypeParser.GetEntityTypes(Settings, OnError, cancellationToken).ConfigureAwait(false);
        return entityTypes;
    }

    public void Dispose() => _connection.Dispose();

    private void OnError(Exception exception) => ErrorOccurred?.Invoke(this, exception);
}
