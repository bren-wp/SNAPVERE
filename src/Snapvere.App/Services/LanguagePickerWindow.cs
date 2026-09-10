using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Shared;
using Windows.Graphics;

namespace Snapvere.App.Services;

/// <summary>
/// On-demand language picker. The window exists only while the user changes
/// language and therefore adds no resident timers, polling or background work.
/// </summary>
public sealed class LanguagePickerWindow : Window
{
    private const int WindowWidth = 560;
    private const int WindowHeight = 520;

    private static LanguagePickerWindow? _standaloneWindow;

    private readonly CapturePreferencesService _preferences;
    private readonly ComboBox _languageCombo;
    private readonly TextBlock _status;
    private bool _sizeApplied;
    private bool _initializing = true;

    public LanguagePickerWindow(CapturePreferencesService preferences)
    {
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        Title = $"SNAPVERE — {L("Language")}";

        _languageCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 44
        };
        AutomationProperties.SetName(_languageCombo, L("ChooseLanguage"));
        foreach (var language in SnapvereLocalization.SupportedLanguages)
        {
            _languageCombo.Items.Add(new ComboBoxItem
            {
                Content = $"{language.NativeName}  —  {language.EnglishName}",
                Tag = language.Code
            });
        }
        _languageCombo.SelectionChanged += LanguageCombo_SelectionChanged;

        _status = Text(L("LanguageSaved"), 10.5, Muted);
        _status.TextWrapping = TextWrapping.Wrap;

        Content = BuildContent();
        SelectCurrentLanguage();
        _initializing = false;
        Activated += LanguagePickerWindow_Activated;
    }

    public static void ShowStandalone()
    {
        if (_standaloneWindow is not null)
        {
            _standaloneWindow.Activate();
            return;
        }

        var window = new LanguagePickerWindow(new CapturePreferencesService());
        _standaloneWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_standaloneWindow, window))
            {
                _standaloneWindow = null;
            }
        };
        window.Activate();
    }

    private string L(string key) => SnapvereLocalization.T(key, _preferences.Current.LanguageCode);

    private string LF(string key, params object[] arguments)
        => string.Format(L(key), arguments);

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Surface,
            Padding = new Thickness(26)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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
            Margin = new Thickness(14, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        identity.Children.Add(Text("SNAPVERE", 12, Accent, Microsoft.UI.Text.FontWeights.Bold));
        identity.Children.Add(Text(L("ChooseLanguage"), 26, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        root.Children.Add(header);

        var intro = Text(L("LanguagePickerIntro"), 11, Muted);
        intro.TextWrapping = TextWrapping.Wrap;
        intro.Margin = new Thickness(0, 22, 0, 12);
        Grid.SetRow(intro, 1);
        root.Children.Add(intro);

        var card = new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(18),
            Background = Brush(0xFF, 0x12, 0x17, 0x24),
            BorderBrush = Outline,
            BorderThickness = new Thickness(1)
        };
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(Text(L("Language").ToUpperInvariant(), 9, Accent, Microsoft.UI.Text.FontWeights.Bold));
        stack.Children.Add(_languageCombo);
        stack.Children.Add(_status);
        card.Child = stack;
        Grid.SetRow(card, 2);
        root.Children.Add(card);

        var close = new Button
        {
            Content = L("Close"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(20, 9, 20, 9),
            CornerRadius = new CornerRadius(11),
            Background = Brush(0xFF, 0x3A, 0x2B, 0x78),
            BorderBrush = Brush(0xFF, 0x86, 0x67, 0xF4),
            BorderThickness = new Thickness(1),
            Foreground = Strong
        };
        AutomationProperties.SetName(close, L("Close"));
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 3);
        root.Children.Add(close);
        return root;
    }

    private void SelectCurrentLanguage()
    {
        var current = SnapvereLocalization.NormalizeLanguageCode(_preferences.Current.LanguageCode);
        foreach (var item in _languageCombo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, current, StringComparison.OrdinalIgnoreCase))
            {
                _languageCombo.SelectedItem = item;
                return;
            }
        }
        _languageCombo.SelectedIndex = 0;
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || _languageCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string code)
        {
            return;
        }

        try
        {
            _preferences.SetLanguageCode(code);
            _status.Text = L("LanguageSaved");
            _status.Foreground = Success;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _status.Text = LF("LanguageSaveFailed", exception.Message);
            _status.Foreground = Error;
        }
    }

    private void LanguagePickerWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSize(this, WindowWidth, WindowHeight));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }
    }

    private static FrameworkElement BuildBrandMark()
    {
        var mark = new Grid { Width = 52, Height = 52 };
        var gradient = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        gradient.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x62, 0x4B, 0xE8), Offset = 0 });
        gradient.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x37, 0xB6, 0xD4), Offset = 1 });
        mark.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = gradient,
            BorderBrush = Brush(0x70, 0xC9, 0xC0, 0xFF),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = "\uE774",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 20,
                Foreground = Strong
            }
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

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Surface => Brush(0xFF, 0x0D, 0x13, 0x21);
    private static SolidColorBrush Outline => Brush(0xFF, 0x52, 0x61, 0x7F);
    private static SolidColorBrush Strong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAF, 0xB6, 0xC8);
    private static SolidColorBrush Accent => Brush(0xFF, 0xA7, 0x7C, 0xFF);
    private static SolidColorBrush Success => Brush(0xFF, 0x72, 0xD8, 0xB4);
    private static SolidColorBrush Error => Brush(0xFF, 0xF0, 0x8C, 0x9A);
}