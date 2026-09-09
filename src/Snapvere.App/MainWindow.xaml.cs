using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snapvere.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
    }

    private void RootNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            Title = "SNAPVERE — Settings";
            return;
        }

        if (args.SelectedItemContainer?.Tag is string tag)
        {
            Title = tag switch
            {
                "capture" => "SNAPVERE — Capture",
                "history" => "SNAPVERE — History",
                "editor" => "SNAPVERE — Editor",
                _ => "SNAPVERE"
            };
        }
    }
}
