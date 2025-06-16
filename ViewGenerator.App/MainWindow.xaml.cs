using System.Windows;
using System.Windows.Controls;
using ViewGenerator.Common.Models;
using ViewGenerator.Engine;

namespace ViewGenerator.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ViewEngineSettings? viewEngineSettings;
    private Generator? viewGenerator;
    private bool reloadEntityTypes;
    private bool entityTypesLoadedFirstTime = false;
    private string? settingsContents;
    private CancellationTokenSource cancellationTokenSource = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Wnd_Main_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            viewEngineSettings = this.GetViewEngineSettings();
            viewGenerator = new Generator(viewEngineSettings);
            viewGenerator.ErrorOccurred += _viewGenerator_ErrorOccurred;
            viewGenerator.ViewGenerated += _viewGenerator_ViewGenerated;
            viewGenerator.EntityTypeIgnored += _viewGenerator_EntityTypeIgnored;
            viewGenerator.ViewDropped += ViewGenerator_ViewDropped;
            reloadEntityTypes = Chk_ForceReloadEntityTypes.IsChecked ?? false;
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private async void Btn_LoadEntityTypes_Click(object sender, RoutedEventArgs e)
    {
        if (!reloadEntityTypes && entityTypesLoadedFirstTime)
        {
            return;
        }

        try
        {
            viewGenerator.ValidateViewGenerator();

            var entityTypes = await viewGenerator.GetEntityTypes(reloadEntityTypes, cancellationTokenSource.Token);
            Cmb_EntityTypes.AddEntityTypesToComboBox(entityTypes);

            Btn_LoadEntityTypes.Content = "Reload Entity Types";
            entityTypesLoadedFirstTime = true;
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private void Wnd_Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            (viewGenerator as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private void _viewGenerator_ErrorOccurred(object? sender, Exception e) => Dispatcher.Invoke(() => this.AppendMessageToLogBox(e.GetFullExceptionMessage(), false));

    private void ViewGenerator_ViewDropped(object? sender, EntityType e)
        => Dispatcher.Invoke(() => this.AppendMessageToLogBox($"There was an existing view for the entity type {e.Identifier}. The view has been dropped. You can change this setting in the setings file.", false));

    private void _viewGenerator_EntityTypeIgnored(object? sender, EntityType e)
        => Dispatcher.Invoke(() => this.AppendMessageToLogBox($"There was an existing view for the entity type {e.Identifier}. The view was ignored. You can change this setting in the setings file.", false));

    private void _viewGenerator_ViewGenerated(object? sender, EntityType e)
        => Dispatcher.Invoke(() => this.AppendMessageToLogBox($"View has been generated for the entity type {e.Identifier}.", false));

    private void Chk_ForceReloadEntityTypes_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            var checkBox = (CheckBox)sender;
            reloadEntityTypes = checkBox.IsChecked ?? false;
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private async void Btn_ViewEntityTypeQuery_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            viewGenerator.ValidateViewGenerator();

            var entityType = Cmb_EntityTypes.GetSelectedEntityTypeFromComboBox();
            var query = await viewGenerator.GetSqlQueryForView(entityType.Identifier, reloadEntityTypes, cancellationTokenSource.Token);

            this.AppendMessageToResultBox(query, true);
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private void Btn_ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var entityType = Cmb_EntityTypes.GetSelectedEntityTypeFromComboBox();
            var details = entityType.GetEntityTypeDetailsString();
            this.AppendMessageToResultBox(details, true);
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private async void Btn_ViewCompleteQuery_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            viewGenerator.ValidateViewGenerator();

            var query = await viewGenerator.GetSqlQueryForViews(reloadEntityTypes, cancellationTokenSource.Token);

            this.AppendMessageToResultBox(query, true);
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private async void Btn_ViewAppSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = settingsContents ??= await GeneralHelper.GetSettingsFileContents();

            this.AppendMessageToResultBox(settings, true);
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private async void Btn_GenerateViews_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            viewGenerator.ValidateViewGenerator();
            await viewGenerator.GenerateViews(cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }
}