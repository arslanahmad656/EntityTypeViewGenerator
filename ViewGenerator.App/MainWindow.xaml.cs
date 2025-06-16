using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

        }
        catch (Exception ex)
        {
            this.HandleException(ex);
        }
    }

    private void _viewGenerator_ErrorOccurred(object? sender, Exception e) => Dispatcher.Invoke(() => this.AppendMessageToLogBox(e.GetFullExceptionMessage(), false));

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
}