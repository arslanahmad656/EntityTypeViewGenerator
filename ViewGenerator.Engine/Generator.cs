using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using ViewGenerator.Common;
using ViewGenerator.Common.Models;

namespace ViewGenerator.Engine;

public class Generator : IViewGenerator, IDisposable
{
    private readonly SqlConnection _connection;
    private string? queryForViewsComplete;
    private Dictionary<string, EntityType>? allEntityTypes;
    private Dictionary<string, string> queriesPerEntityTypeIdentifier = new();
    private Dictionary<string, EntityType> entityTypePerEntityTypeIdentifier = new();

    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<EntityType>? EntityTypeIgnored;
    public event EventHandler<EntityType>? ViewGenerated;
    public event EventHandler<EntityType>? ViewDropped;

    public ViewEngineSettings Settings { get; }

    public Generator(ViewEngineSettings settings)
    {
        Settings = settings;
        _connection = new SqlConnection(settings.ConnectionString);

        Validators.ValidateConfRootFolder(settings.ConfRootFolder);
        Validators.ValidateIncludeAndSkipViews(settings.IncludeViews, settings.ExcludeViews);
    }

    public async Task GenerateViews(CancellationToken cancellationToken)
    {
        try
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted, "GenerateViewsTransaction");

            var entityTypes = await GetEntityTypes(true, cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < entityTypes.Count; i++)
            {
                var entityType = entityTypes[i];
                try
                {
                    var queryToCheckView = Helper.GetSqlQueryToCheckTheViewExists(entityType.Identifier);
                    using var checkViewCommand = new SqlCommand(queryToCheckView, _connection, transaction);
                    var viewId = await checkViewCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (viewId is not DBNull)
                    {
                        if (Settings.ExistingViewAction is ExistingViewAction.IgnoreView)
                        {
                            EntityTypeIgnored?.Invoke(this, entityType);
                            continue;
                        }

                        if (Settings.ExistingViewAction is ExistingViewAction.RollbackTransaction)
                        {
                            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                            throw new Exception($"An error occurred while creating the view for {entityType.Identifier}. The view already exists. The entire transaction was rolled back because the setting is set to {ExistingViewAction.RollbackTransaction} when a conflicting view occurred.");
                        }

                        if (Settings.ExistingViewAction is ExistingViewAction.RecreateView)
                        {
                            var queryToDropView = Helper.GetSqlQueryToDropView(entityType.Identifier);
                            var dropViewCommand = new SqlCommand(queryToDropView, _connection, transaction);
                            await dropViewCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                            ViewDropped?.Invoke(this, entityType);
                        }
                    }

                    var queryToCreateView = await GetSqlQueryForView(entityType.Identifier, false, cancellationToken).ConfigureAwait(false);
                    using var createViewCommand = new SqlCommand(queryToCreateView, _connection, transaction);
                    await createViewCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    
                    ViewGenerated?.Invoke(this, entityType);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occurred while creating the view for {entityType.Identifier}. See the inner exception for more details.", ex);
                }
            }

            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new Exception("Error while generating views. See inner exception for details.", ex);
        }
        finally
        {
            if (_connection.State is ConnectionState.Open)
            {
                _connection.Close();
            }
        }
    }

    public async Task<string> GetSqlQueryForView(string entityTypeIdentifier, bool reloadEntityTypes, CancellationToken cancellationToken)
    {
        if (!reloadEntityTypes && queriesPerEntityTypeIdentifier.TryGetValue(entityTypeIdentifier, out var query))
        {
            return query;
        }

        await GetEntityTypes(reloadEntityTypes, cancellationToken).ConfigureAwait(false);   // to load into cache
        var entityType = allEntityTypes![entityTypeIdentifier];

        var entityTypeViewQuery = Helper.GetViewQueryForEntityType(entityType, Settings.IncludeIdColumns);
        queriesPerEntityTypeIdentifier[entityTypeIdentifier] = entityTypeViewQuery;

        return entityTypeViewQuery;
    }

    public async Task<string> GetSqlQueryForViews(bool reloadEntityTypes, CancellationToken cancellationToken)
    {
        if (!reloadEntityTypes && !string.IsNullOrEmpty(queryForViewsComplete))
        {
            return queryForViewsComplete;
        }

        var queryBuilder= new StringBuilder();

        var entityTypes = await GetEntityTypes(reloadEntityTypes, cancellationToken).ConfigureAwait(false);
        foreach (var entityType in entityTypes)
        {
            queryBuilder.AppendLine();
            queryBuilder.AppendLine();

            var entityTypeViewQuery = Helper.GetViewQueryForEntityType(entityType, Settings.IncludeIdColumns);
            queryBuilder.AppendLine(entityTypeViewQuery);
        }

        queryForViewsComplete = queryBuilder.ToString();
        return queryForViewsComplete;
    }

    public async Task<List<EntityType>> GetEntityTypes(bool reloadEntityTypes, CancellationToken cancellationToken)
    {
        if (!reloadEntityTypes && allEntityTypes is not null)
        {
            return [.. this.allEntityTypes.Values];
        }

        var entityTypes = await EntityTypeParser.GetEntityTypes(Settings, OnError, cancellationToken).ConfigureAwait(false);
        allEntityTypes = entityTypes.ToDictionary(et => et.Identifier, et => et);

        return entityTypes;
    }

    public async Task<EntityType> GetEntityType(string entityTypeIdentifier, bool reloadEntityTypes, CancellationToken cancellationToken)
    {
        if (!reloadEntityTypes && entityTypePerEntityTypeIdentifier.TryGetValue(entityTypeIdentifier, out var entityType))
        {
            return entityType;
        }

        await GetEntityTypes(reloadEntityTypes, cancellationToken).ConfigureAwait(false);   // to load into cache
        entityType = allEntityTypes![entityTypeIdentifier];

        entityTypePerEntityTypeIdentifier[entityTypeIdentifier] = entityType;
        return entityType;
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
}
