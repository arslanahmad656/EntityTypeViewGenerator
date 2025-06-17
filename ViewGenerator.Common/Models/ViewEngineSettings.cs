namespace ViewGenerator.Common.Models;

public record ViewEngineSettings(
    string ConfRootFolder,
    string ConnectionString,
    bool IncludeForeignKeys,
    bool IncludeIdColumns,
    ExistingViewAction ExistingViewAction,
    ErrorAction ErrorAction,
    string[] IncludeViews,
    string[] ExcludeViews
    );