namespace ViewGenerator.Common.Models;

public record ViewEngineSettings(
    string ConfRootFolder,
    string ConnectionString,
    bool IncludeForeignKeys,
    ExistingViewAction ExistingViewAction,
    ErrorAction ErrorAction,
    string[] IncludeViews,
    string[] SkipViews
    );