using Microsoft.UI.Xaml.Controls;

namespace Snapvere.App;

public sealed partial class MainWindow
{
    private Grid RootContent = null!;

    private void InitializeComponent()
    {
        RootContent = new Grid();
        Content = RootContent;
    }
}
