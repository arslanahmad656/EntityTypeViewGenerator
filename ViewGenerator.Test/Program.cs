using ViewGenerator.Common;
using ViewGenerator.Common.Models;
using ViewGenerator.Engine;

try
{
    var settings = new ViewEngineSettings(
        //ConfRootFolder: @"D:\test\Conf.Demo",
        //ConfRootFolder: @"D:\test\Conf.1 (1)\Conf",
        ConfRootFolder: @"C:\UsercubeDemo\Conf.388225Cx",
        ConnectionString: "data source=DATASERVER;Database=388225Cx1;Integrated Security=SSPI;Min Pool Size=10;MultipleActiveResultSets=True;encrypt=false",
        IncludeForeignKeys: true,
        IncludeIdColumns: true,
        ExistingViewAction: ExistingViewAction.RecreateView,
        ErrorAction: ErrorAction.Continue,
        IncludeViews: [],
        ExcludeViews: []
        );

    using var engine = new Generator(settings);
    engine.ErrorOccurred += (_, args) => Console.WriteLine($"Error detected: {args.Message}");

    //var entityTypes = await engine.GetEntityTypes(CancellationToken.None);
    //Console.WriteLine($"Total entity types: {entityTypes.Count}");
    //var viewQuery = await engine.GetSqlQueryForViews(CancellationToken.None);
    //Console.WriteLine(viewQuery);
    await engine.GenerateViews(CancellationToken.None);
    Console.WriteLine("Done");
 }
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}