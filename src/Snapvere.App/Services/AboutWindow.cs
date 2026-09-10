using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Snapvere.App.Services;

public sealed class AboutWindow : Window
{
    private bool _sizeApplied;

    public AboutWindow()
    {
        Title = "About SNAPVERE";
        Content = BuildContent();
        Activated += AboutWindow_Activated;
    }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0xFF, 0x07, 0x08, 0x0D),
            Padding = new Thickness(24)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        header.Children.Add(BuildBrandMark());
        var identity = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var brand = Text("SNAPVERE", 17, Strong, Microsoft.UI.Text.FontWeights.Bold);
        brand.CharacterSpacing = 70;
        identity.Children.Add(brand);
        identity.Children.Add(Text("Capture. Edit. Done.", 10.5, Muted));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        root.Children.Add(header);

        var card = new Border
        {
            Margin = new Thickness(0, 20, 0, 18),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(18),
            Background = Brush(0xFF, 0x0F, 0x11, 0x19),
            BorderBrush = Brush(0xFF, 0x28, 0x2C, 0x39),
            BorderThickness = new Thickness(1)
        };

        var content = new StackPanel { Spacing = 14 };
        var version = typeof(AboutWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";

        var versionRow = new Grid();
        versionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        versionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel { Spacing = 3 };
        title.Children.Add(Text("Local-first capture for Windows", 15, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        title.Children.Add(Text("Region, window and screen capture without cloud dependency.", 10.5, Muted));
        versionRow.Children.Add(title);
        var badge = new Border
        {
            Padding = new Thickness(9, 5, 9, 5),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x55, 0x5D, 0x47, 0xB7),
            BorderBrush = Brush(0x70, 0x9C, 0x86, 0xFF),
            BorderThickness = new Thickness(1),
            Child = Text($"v{version}", 9.5, Strong, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        Grid.SetColumn(badge, 1);
        versionRow.Children.Add(badge);
        content.Children.Add(versionRow);

        var privacy = new Border
        {
            Padding = new Thickness(13),
            CornerRadius = new CornerRadius(13),
            Background = Brush(0x35, 0x21, 0x4A, 0x40),
            BorderBrush = Brush(0x50, 0x45, 0xB9, 0x98),
            BorderThickness = new Thickness(1)
        };
        var privacyCopy = new StackPanel { Spacing = 3 };
        privacyCopy.Children.Add(Text("LOCAL-FIRST", 9, Success, Microsoft.UI.Text.FontWeights.Bold));
        var description = Text(
            "Screenshots stay on your device unless you explicitly copy, save or share them through Windows.",
            10.5,
            Muted);
        description.TextWrapping = TextWrapping.Wrap;
        privacyCopy.Children.Add(description);
        privacy.Child = privacyCopy;
        content.Children.Add(privacy);

        content.Children.Add(BuildShortcutRow("Print Screen", "Region Capture"));
        content.Children.Add(BuildShortcutRow("Ctrl + Shift + 2", "Window Capture"));
        content.Children.Add(BuildShortcutRow("Ctrl + Shift + 4", "Screen Capture"));

        var footerNote = Text("Built by Brendigo  •  MPL-2.0", 9.5, Subtle);
        content.Children.Add(footerNote);

        card.Child = content;
        Grid.SetRow(card, 1);
        root.Children.Add(card);

        var close = new Button
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0xFF, 0x18, 0x1B, 0x25),
            BorderBrush = Brush(0xFF, 0x32, 0x36, 0x45),
            BorderThickness = new Thickness(1),
            Foreground = Strong
        };
        AutomationProperties.SetName(close, "Close About SNAPVERE");
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 2);
        root.Children.Add(close);
        return root;
    }

    private static Border BuildShortcutRow(string shortcut, string action)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var shortcutBadge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(9, 5, 9, 5),
            CornerRadius = new CornerRadius(9),
            Background = Brush(0xFF, 0x16, 0x18, 0x22),
            BorderBrush = Brush(0xFF, 0x2E, 0x32, 0x40),
            BorderThickness = new Thickness(1),
            Child = Text(shortcut, 9.5, Strong, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        grid.Children.Add(shortcutBadge);

        var actionText = Text(action, 10.5, Muted);
        actionText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(actionText, 1);
        grid.Children.Add(actionText);
        return new Border { Child = grid };
    }

    private void AboutWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSize(this, 520, 500));
    }

    private static FrameworkElement BuildBrandMark()
    {
        var mark = new Grid { Width = 46, Height = 46 };
        mark.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(14),
            Background = AccentGradient(),
            BorderBrush = Brush(0x66, 0xCA, 0xC1, 0xFF),
            BorderThickness = new Thickness(1)
        });
        mark.Children.Add(new FontIcon
        {
            Glyph = "\uE722",
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 18,
            Foreground = Strong,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        return mark;
    }

    private static TextBlock Text(
        string value,
        double size,
        SolidColorBrush foreground,
        Windows.UI.Text.FontWeight? weight = null)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = foreground,
            FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal
        };

    private static LinearGradientBrush AccentGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x61, 0x4A, 0xE8), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x8C, 0x5A, 0xF4), Offset = 0.62 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x36, 0xB6, 0xD5), Offset = 1 });
        return brush;
    }

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Strong => Brush(0xFF, 0xF6, 0xF5, 0xFB);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAE, 0xAC, 0xBC);
    private static SolidColorBrush Subtle => Brush(0xFF, 0x7D, 0x7C, 0x8D);
    private static SolidColorBrush Success => Brush(0xFF, 0x72, 0xD8, 0xB4);
}
