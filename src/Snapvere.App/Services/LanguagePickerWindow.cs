using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Shared;
using Windows.Graphics;
using Windows.System;

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
    private TextBlock? _titleText;
    private TextBlock? _introText;
    private TextBlock? _languageEyebrow;
    private Button? _closeButton;
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

        _status = Text(string.Empty, 10.5, Muted);
        _status.TextWrapping = TextWrapping.Wrap;
        _status.Visibility = Visibility.Collapsed;
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);

        var content = BuildContent();
        content.KeyDown += Root_KeyDown;
        Content = content;
        SelectCurrentLanguage();
        _initializing = false;
        Activated += LanguagePickerWindow_Activated;
    }

    public static void ShowStandalone(CapturePreferencesService preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        if (_standaloneWindow is not null)
        {
            _standaloneWindow.Activate();
            return;
        }

        var window = new LanguagePickerWindow(preferences);
        _standaloneWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_standaloneWindow, window))
            {
                _standaloneWindow = null;
            }
        };

        try
        {
            window.Activate();
        }
        catch
        {
            if (ReferenceEquals(_standaloneWindow, window))
            {
                _standaloneWindow = null;
            }

            try
            {
                window.Close();
            }
            catch (Exception closeException)
            {
                StartupDiagnostics.Record(
                    "Rollback failed Language window activation",
                    closeException);
            }

            throw;
        }
    }

    public static void CloseStandalone()
    {
        var window = _standaloneWindow;
        _standaloneWindow = null;
        if (window is null)
        {
            return;
        }

        try
        {
            window.Close();
        }
        catch (Exception exception)
        {
            // Shutdown/recovery must not retain a stale singleton if WinUI has
            // already invalidated the underlying window.
            StartupDiagnostics.Record("Close standalone Language window", exception);
        }
    }

    private string L(string key) => SnapvereLocalization.T(key, _preferences.Current.LanguageCode);

    private string LanguageSaveFailureText()
        => string.Equals(
                SnapvereLocalization.NormalizeLanguageCode(_preferences.Current.LanguageCode),
                "hr",
                StringComparison.OrdinalIgnoreCase)
            ? "Postavku jezika nije moguće spremiti. Pokušajte ponovno ili ponovno pokrenite SNAPVERE."
            : "The language setting could not be saved. Try again or restart SNAPVERE.";

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
        _titleText = Text(L("ChooseLanguage"), 26, Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        identity.Children.Add(_titleText);
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        root.Children.Add(header);

        _introText = Text(L("LanguagePickerIntro"), 11, Muted);
        _introText.TextWrapping = TextWrapping.Wrap;
        _introText.Margin = new Thickness(0, 22, 0, 12);
        Grid.SetRow(_introText, 1);
        root.Children.Add(_introText);

        var card = new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(18),
            Background = SnapvereBrand.Slate,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1)
        };
        var stack = new StackPanel { Spacing = 12 };
        _languageEyebrow = Text(L("Language").ToUpperInvariant(), 9, Accent, Microsoft.UI.Text.FontWeights.Bold);
        stack.Children.Add(_languageEyebrow);
        stack.Children.Add(_languageCombo);
        stack.Children.Add(_status);
        // This compact surface never needs scrolling. Keeping the three controls
        // directly in the card avoids transient overlay-scrollbar composition that
        // can make the otherwise static picker visually unstable during capture.
        card.Child = stack;
        Grid.SetRow(card, 2);
        root.Children.Add(card);

        _closeButton = new Button
        {
            Content = L("Close"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(20, 9, 20, 9),
            CornerRadius = new CornerRadius(11),
            Background = SnapvereBrand.Violet,
            BorderBrush = SnapvereBrand.Lavender,
            BorderThickness = new Thickness(1),
            Foreground = Strong
        };
        AutomationProperties.SetName(_closeButton, L("Close"));
        _closeButton.Click += (_, _) => Close();
        Grid.SetRow(_closeButton, 3);
        root.Children.Add(_closeButton);
        return root;
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        e.Handled = true;
        Close();
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
            RefreshLocalizedText();
            _status.Text = L("LanguageSaved");
            _status.Foreground = Success;
            _status.Visibility = Visibility.Visible;
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            System.Security.SecurityException or
            InvalidOperationException)
        {
            StartupDiagnostics.Record("Save language preference", exception);

            _initializing = true;
            try
            {
                SelectCurrentLanguage();
            }
            finally
            {
                _initializing = false;
            }

            _status.Text = LanguageSaveFailureText();
            _status.Foreground = Error;
            _status.Visibility = Visibility.Visible;
        }
    }

    private void RefreshLocalizedText()
    {
        Title = $"SNAPVERE — {L("Language")}";
        AutomationProperties.SetName(_languageCombo, L("ChooseLanguage"));

        if (_titleText is not null)
        {
            _titleText.Text = L("ChooseLanguage");
        }

        if (_introText is not null)
        {
            _introText.Text = L("LanguagePickerIntro");
        }

        if (_languageEyebrow is not null)
        {
            _languageEyebrow.Text = L("Language").ToUpperInvariant();
        }

        if (_closeButton is not null)
        {
            _closeButton.Content = L("Close");
            AutomationProperties.SetName(_closeButton, L("Close"));
        }
    }

    private void LanguagePickerWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, WindowWidth, WindowHeight));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }
    }

    private static FrameworkElement BuildBrandMark()
        => SnapvereBrand.CreateMark(46);

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

    private static SolidColorBrush Surface => SnapvereBrand.Obsidian;
    private static SolidColorBrush Outline => SnapvereBrand.Outline;
    private static SolidColorBrush Strong => SnapvereBrand.Strong;
    private static SolidColorBrush Muted => SnapvereBrand.Muted;
    private static SolidColorBrush Accent => SnapvereBrand.Lavender;
    private static SolidColorBrush Success => SnapvereBrand.Success;
    private static SolidColorBrush Error => SnapvereBrand.Danger;
}