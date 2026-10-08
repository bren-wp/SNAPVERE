using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Snapvere.Shared;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using VirtualKey = Windows.System.VirtualKey;

namespace Snapvere.App.Services;

public sealed class AboutWindow : Window
{
    private const string ProductWebsiteUrl = "https://snapvere.com";
    private const string SupportEmailAddress = "info@snapvere.com";
    private const string SupportEmailUri = "mailto:info@snapvere.com";
    private const string PrivacyUrl = "https://snapvere.com/privacy";
    private const string TermsUrl = "https://snapvere.com/terms";
    private const string DeveloperWebsiteUrl = "https://brendigo.com";

    private readonly DispatcherQueue _dispatcherQueue;
    private string _languageCode;
    private TextBlock _supportStatus;
    private bool _sizeApplied;
    private bool _closed;

    public AboutWindow()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("SNAPVERE could not acquire the About window dispatcher.");
        _languageCode = SnapvereLocalization.NormalizeLanguageCode(
            SnapvereLanguageState.CurrentLanguageCode);
        _supportStatus = CreateSupportStatus();

        Title = SnapvereLocalization.T("About", _languageCode);
        Content = BuildContent();
        SnapvereLanguageState.CurrentLanguageChanged += OnCurrentLanguageChanged;
        Activated += AboutWindow_Activated;
        Closed += AboutWindow_Closed;
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

    private TextBlock CreateSupportStatus()
    {
        var status = Text(string.Empty, 9.5, Success);
        status.TextWrapping = TextWrapping.Wrap;
        status.Visibility = Visibility.Collapsed;
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Assertive);
        return status;
    }

    private void OnCurrentLanguageChanged(object? sender, EventArgs e)
    {
        if (_closed)
        {
            return;
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            RefreshLanguageSafely();
            return;
        }

        if (!_dispatcherQueue.TryEnqueue(RefreshLanguageSafely))
        {
            StartupDiagnostics.WriteLine(
                "About language refresh was not queued because the UI dispatcher is shutting down.");
        }
    }

    private void RefreshLanguageSafely()
    {
        if (_closed)
        {
            return;
        }

        try
        {
            RefreshLanguage();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Refresh About language", exception);
        }
    }

    private void RefreshLanguage()
    {
        var languageCode = SnapvereLocalization.NormalizeLanguageCode(
            SnapvereLanguageState.CurrentLanguageCode);
        if (string.Equals(_languageCode, languageCode, StringComparison.Ordinal))
        {
            return;
        }

        _languageCode = languageCode;
        _supportStatus = CreateSupportStatus();
        Title = SnapvereLocalization.T("About", _languageCode);
        Content = BuildContent();
    }

    private void AboutWindow_Closed(object sender, WindowEventArgs args)
    {
        _closed = true;
        SnapvereLanguageState.CurrentLanguageChanged -= OnCurrentLanguageChanged;
        Activated -= AboutWindow_Activated;
        Closed -= AboutWindow_Closed;
    }

    private string L(string key) => SnapvereLocalization.T(key, _languageCode);

    private string Tagline()
        => string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
            ? "Snimi. Uredi. Gotovo."
            : "Capture. Edit. Done.";

    private string SupportCopiedAnnouncement()
        => string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
            ? $"Adresa podrške {SupportEmailAddress} kopirana je u međuspremnik."
            : $"Support email {SupportEmailAddress} copied to the clipboard.";

    private string LinkOpenFailedAnnouncement()
        => string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
            ? "Windows ne može otvoriti ovu poveznicu. Provjerite zadani preglednik i pokušajte ponovno."
            : "Windows could not open this link. Check your default browser and try again.";

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = SnapvereBrand.Obsidian,
            Padding = new Thickness(24)
        };
        root.KeyDown += Root_KeyDown;
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
        identity.Children.Add(Text(Tagline(), 10.5, Muted));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        root.Children.Add(header);

        var card = new Border
        {
            Margin = new Thickness(0, 20, 0, 18),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(18),
            Background = SnapvereBrand.Surface,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1)
        };

        var content = new StackPanel { Spacing = 12 };
        var version = typeof(AboutWindow).Assembly.GetName().Version?.ToString(3);

        var versionRow = new Grid();
        versionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        versionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel { Spacing = 3 };
        title.Children.Add(Text(L("AboutLocalFirstTitle"), 15, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        title.Children.Add(Text(L("AboutCaptureDescription"), 10.5, Muted));
        versionRow.Children.Add(title);
        var badge = new Border
        {
            Padding = new Thickness(9, 5, 9, 5),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x44, 0x76, 0x55, 0xF6),
            BorderBrush = Brush(0x88, 0xA4, 0x8B, 0xFF),
            BorderThickness = new Thickness(1),
            Child = Text(version is null ? "SNAPVERE" : $"v{version}", 9.5, Strong, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        Grid.SetColumn(badge, 1);
        versionRow.Children.Add(badge);
        content.Children.Add(versionRow);

        var privacy = new Border
        {
            Padding = new Thickness(13),
            CornerRadius = new CornerRadius(13),
            Background = Brush(0x22, 0x80, 0xE1, 0xE5),
            BorderBrush = Brush(0x55, 0x80, 0xE1, 0xE5),
            BorderThickness = new Thickness(1)
        };
        var privacyCopy = new StackPanel { Spacing = 3 };
        privacyCopy.Children.Add(Text(L("LocalFirst").ToUpperInvariant(), 9, Success, Microsoft.UI.Text.FontWeights.Bold));
        var description = Text(L("AboutPrivacyDescription"), 10.5, Muted);
        description.TextWrapping = TextWrapping.Wrap;
        privacyCopy.Children.Add(description);
        privacy.Child = privacyCopy;
        content.Children.Add(privacy);

        content.Children.Add(BuildShortcutRow("Print Screen", L("CaptureRegion")));
        content.Children.Add(BuildShortcutRow("Ctrl + Shift + 2", L("CaptureWindow")));
        content.Children.Add(BuildShortcutRow("Ctrl + Shift + 3", L("CaptureScreen")));

        content.Children.Add(Text(L("CommercialSoftware"), 9.5, Subtle));

        var links = new StackPanel { Spacing = 8 };
        links.Children.Add(CreateLinkButton("snapvere.com", ProductWebsiteUrl));
        links.Children.Add(CreateLinkButton(
            $"{L("Support")} · {SupportEmailAddress}",
            SupportEmailUri,
            SupportEmailAddress));
        links.Children.Add(_supportStatus);
        links.Children.Add(CreateLinkButton(L("Privacy"), PrivacyUrl));
        links.Children.Add(CreateLinkButton(L("Terms"), TermsUrl));
        links.Children.Add(CreateLinkButton("brendigo.com", DeveloperWebsiteUrl));
        content.Children.Add(links);

        card.Child = new ScrollViewer
        {
            Content = content,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsTabStop = false
        };
        Grid.SetRow(card, 1);
        root.Children.Add(card);

        var close = new Button
        {
            Content = L("Close"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(10),
            Background = SnapvereBrand.Slate,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            Foreground = Strong
        };
        AutomationProperties.SetName(close, L("Close"));
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 2);
        root.Children.Add(close);
        return root;
    }

    private Button CreateLinkButton(
        string text,
        string url,
        string? clipboardFallbackText = null)
    {
        var label = Text(text, 10, Strong);
        label.TextWrapping = TextWrapping.Wrap;

        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(11, 6, 11, 6),
            CornerRadius = new CornerRadius(9),
            Background = SnapvereBrand.Slate,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            Foreground = Strong
        };
        AutomationProperties.SetName(button, text);
        button.Click += (_, _) =>
        {
            try
            {
                LocalShellAction.Open(url);
                _supportStatus.Visibility = Visibility.Collapsed;
            }
            catch (Exception exception) when (
                exception is System.ComponentModel.Win32Exception or
                InvalidOperationException or
                IOException or
                System.Security.SecurityException)
            {
                StartupDiagnostics.Record($"Open {url}", exception);
                if (clipboardFallbackText is null)
                {
                    var announcement = LinkOpenFailedAnnouncement();
                    _supportStatus.Text = announcement;
                    _supportStatus.Foreground = Warning;
                    _supportStatus.Visibility = Visibility.Visible;
                    AutomationProperties.SetHelpText(button, announcement);
                    return;
                }

                try
                {
                    var package = new DataPackage();
                    package.SetText(clipboardFallbackText);
                    Clipboard.SetContent(package);
                    Clipboard.Flush();

                    var announcement = SupportCopiedAnnouncement();
                    label.Text = $"✓ {clipboardFallbackText}";
                    AutomationProperties.SetName(button, announcement);
                    AutomationProperties.SetHelpText(button, announcement);
                    _supportStatus.Text = announcement;
                    _supportStatus.Foreground = Success;
                    _supportStatus.Visibility = Visibility.Visible;
                }
                catch (Exception clipboardException) when (
                    clipboardException is System.Runtime.InteropServices.COMException or
                    InvalidOperationException or
                    UnauthorizedAccessException or
                    System.Security.SecurityException)
                {
                    StartupDiagnostics.Record("Copy support email fallback", clipboardException);
                    label.Text = clipboardFallbackText;
                    AutomationProperties.SetName(button, clipboardFallbackText);
                    AutomationProperties.SetHelpText(button, clipboardFallbackText);
                    _supportStatus.Text = LinkOpenFailedAnnouncement();
                    _supportStatus.Foreground = Warning;
                    _supportStatus.Visibility = Visibility.Visible;
                }
            }
        };
        return button;
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
            Background = SnapvereBrand.Slate,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            Child = Text(shortcut, 9.5, Strong, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        grid.Children.Add(shortcutBadge);

        var actionText = Text(action, 10.5, Muted);
        actionText.TextWrapping = TextWrapping.Wrap;
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
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, 560, 560));
    }

    private static FrameworkElement BuildBrandMark()
        => SnapvereBrand.CreateMark(46);

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Strong => SnapvereBrand.Strong;
    private static SolidColorBrush Muted => SnapvereBrand.Muted;
    private static SolidColorBrush Subtle => SnapvereBrand.Subtle;
    private static SolidColorBrush Success => SnapvereBrand.Success;
    private static SolidColorBrush Warning => Brush(0xFF, 0xE2, 0xB5, 0x72);
}