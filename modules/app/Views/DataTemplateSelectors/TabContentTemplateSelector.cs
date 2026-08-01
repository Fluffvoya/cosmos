using System.Windows;
using System.Windows.Controls;
using app.Models;
using app.ViewModels;

namespace app.Views.DataTemplateSelectors;

/// <summary>
/// Selects the appropriate DataTemplate for a tab's content based on its tag type.
/// </summary>
public class TabContentTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Template used for the Settings tab.
    /// </summary>
    public DataTemplate? SettingsTemplate { get; set; }

    /// <summary>
    /// Template used for generic document tabs.
    /// </summary>
    public DataTemplate? DocumentTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is TabInfo { tag: SettingsViewModel })
            return SettingsTemplate ?? DocumentTemplate;

        return DocumentTemplate;
    }
}
