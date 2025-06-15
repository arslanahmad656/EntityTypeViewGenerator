using Microsoft.Data.SqlClient;
using System.Data;
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

        Validators.ValidateConfRootFolder(settings.ConfRootFolder);
        Validators.ValidateIncludeAndSkipViews(settings.IncludeViews, settings.ExcludeViews);
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

    void IDisposable.Dispose() => _connection.Dispose();

    private void OnError(Exception exception)
    {
        if (Settings.ErrorAction is ErrorAction.Continue)
        {
            // Only execute the event handler if the ErrorAction is Continue otherwise, the exception will be propagated anyhow.
            ErrorOccurred?.Invoke(this, exception);
        }
    }
    
    private async Task OpenConnection()
    {
        if (_connection.State is not ConnectionState.Open)
        {
            await _connection.OpenAsync().ConfigureAwait(false);
        }
    }
}
