using Microsoft.Extensions.Configuration;
using System.Windows;
using System.Windows.Controls;
using ViewGenerator.Common;
using ViewGenerator.Common.Models;

namespace ViewGenerator.App;

static class WindowHelper
{

#region [Window]

    public static ViewEngineSettings GetViewEngineSettings(this MainWindow window)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var section = configuration.GetSection("ViewEngineSettings");

        var viewEngineSettings = new ViewEngineSettings(
            section["ConfRootFolder"]!,
            section["ConnectionString"]!,
            bool.Parse(section["IncludeForeignKeys"]!),
            bool.Parse(section["IncludeIdColumns"]!),
            Enum.Parse<ExistingViewAction>(section["ExistingViewAction"]!),
            Enum.Parse<ErrorAction>(section["ErrorAction"]!),
            section.GetSection("IncludeViews").Get<string[]>() ?? [],
            section.GetSection("ExcludeViews").Get<string[]>() ?? []
        );

        return viewEngineSettings;
    }

    public static void HandleException(this MainWindow window, Exception ex) => window.ShowError(ex);

    public static void ShowError(this MainWindow window, Exception ex) => window.ShowError(ex.GetFullExceptionMessage());

    public static void ShowError(this MainWindow window, string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void AppendMessageToLogBox(this MainWindow window, string message, bool clear) 
        => window.Txt_LogBox.Text = clear ? message : window.Txt_LogBox.Text + Environment.NewLine + message;

    public static void AppendMessageToResultBox(this MainWindow window, string message, bool clear) 
        => window.Txt_ResultBox.Text = clear ? message : window.Txt_ResultBox.Text + Environment.NewLine + message;

    #endregion

#region [Controls]

    public static void AddEntityTypesToComboBox(this ComboBox comboBox, IEnumerable<EntityType> entityTypes)
    {
        var orderedEntityTypes = entityTypes.OrderBy(x => x.Identifier);
        comboBox.DisplayMemberPath = nameof(EntityType.Identifier);

        foreach (var entityType in orderedEntityTypes)
        {
            comboBox.Items.Add(entityType);
        }

        comboBox.Items.Insert(0, new EntityType("Select an entity type", null, null, new Dictionary<string, EntityProperty>(0)));
        comboBox.SelectedIndex = 0;
    }

    public static EntityType GetSelectedEntityTypeFromComboBox(this ComboBox comboBox)
    {
        if (comboBox.SelectedItem is null)
        {
            throw new Exception("No entity type selected.");
        }

        if (comboBox.SelectedIndex is 0)
        {
            throw new Exception("Not a valid entity type selected.");
        }

        var entityType = (EntityType)comboBox.SelectedItem;
        return entityType;
    }

#endregion  
}
