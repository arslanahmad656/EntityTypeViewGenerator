using ViewGenerator.Common;
using ViewGenerator.Common.Models;
using ViewGenerator.Engine;

try
{
    var settings = new ViewEngineSettings(
        //ConfRootFolder: @"D:\test\Conf.Demo",
        ConfRootFolder: @"D:\test\Conf.1 (1)\Conf",
        ConnectionString: "data source=DATASERVER;Database=388225Cx;Integrated Security=SSPI;Min Pool Size=10;MultipleActiveResultSets=True;encrypt=false",
        IncludeForeignKeys: true,
        ExistingViewAction: ExistingViewAction.RollbackTransaction,
        ErrorAction: ErrorAction.Continue,
        IncludeViews: [],
        ExcludeViews: []
        );

    using var engine = new Generator(settings);
    var entityTypes = await engine.GetEntityTypes(CancellationToken.None);
    Console.WriteLine($"Total entity types: {entityTypes.Count}");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}