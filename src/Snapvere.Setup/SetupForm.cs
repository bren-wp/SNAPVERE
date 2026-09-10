using System.Drawing;
using System.Drawing.Drawing2D;

namespace Snapvere.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Canvas = Color.FromArgb(7, 8, 13);
    private static readonly Color Sidebar = Color.FromArgb(10, 11, 17);
    private static readonly Color Surface = Color.FromArgb(15, 17, 25);
    private static readonly Color SurfaceRaised = Color.FromArgb(22, 24, 34);
    private static readonly Color Accent = Color.FromArgb(141, 121, 255);
    private static readonly Color AccentStrong = Color.FromArgb(103, 80, 210);
    private static readonly Color AccentHover = Color.FromArgb(154, 137, 255);
    private static readonly Color Cyan = Color.FromArgb(54, 182, 213);
    private static readonly Color Muted = Color.FromArgb(174, 172, 188);
    private static readonly Color Subtle = Color.FromArgb(125, 124, 141);
    private static readonly Color Border = Color.FromArgb(42, 46, 58);
    private static readonly Color Success = Color.FromArgb(114, 216, 180);

    private readonly bool _uninstallMode;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly RichTextBox _licenseBox;
    private readonly CheckBox _acceptLicense;
    private readonly TextBox _installPath;
    private readonly Button _browseButton;
    private readonly CheckBox _startMenuShortcut;
    private readonly CheckBox _desktopShortcut;
    private readonly PremiumProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Button _primaryButton;
    private readonly Button _cancelButton;
    private readonly CheckBox _launchAfterInstall;
    private readonly Label _licenseLabel;
    private readonly Label _installLocationLabel;
    private bool _completed;

    public SetupForm(bool uninstallMode)
    {
        _uninstallMode = uninstallMode;

        Text = uninstallMode ? "Remove SNAPVERE" : "SNAPVERE Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowIcon = false;
        ClientSize = new Size(980, 650);
        MinimumSize = new Size(996, 689);
        MaximumSize = new Size(996, 689);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Canvas;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var sidebar = BuildSidebar(uninstallMode);
        Controls.Add(sidebar);

        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas
        };
        Controls.Add(main);
        main.BringToFront();

        var eyebrow = new Label
        {
            AutoSize = true,
            Text = uninstallMode ? "MAINTENANCE" : "INSTALLATION",
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            Location = new Point(34, 30)
        };
        main.Controls.Add(eyebrow);

        _titleLabel = new Label
        {
            AutoSize = true,
            Text = uninstallMode ? "Remove SNAPVERE" : $"Install SNAPVERE {InstallerEngine.VersionText}",
            ForeColor = Color.FromArgb(247, 245, 255),
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            Location = new Point(30, 49)
        };
        main.Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            AutoSize = false,
            Text = uninstallMode
                ? "Remove the application and Windows registration while preserving your local captures."
                : "Fast, private Windows capture. Self-contained, per-user and ready for tray-first use after installation.",
            ForeColor = Muted,
            Location = new Point(34, 91),
            Size = new Size(620, 38)
        };
        main.Controls.Add(_subtitleLabel);

        var card = new RoundedPanel
        {
            Location = new Point(32, 139),
            Size = new Size(636, 374),
            BackColor = Surface,
            BorderColor = Border,
            CornerRadius = 18,
            Padding = new Padding(22)
        };
        main.Controls.Add(card);

        _licenseLabel = new Label
        {
            AutoSize = true,
            Text = "Mozilla Public License 2.0",
            ForeColor = Color.FromArgb(247, 245, 255),
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            Location = new Point(22, 18)
        };
        card.Controls.Add(_licenseLabel);

        var licenseHint = new Label
        {
            AutoSize = true,
            Text = "Review the license terms before continuing",
            ForeColor = Subtle,
            Font = new Font("Segoe UI", 9F),
            Location = new Point(22, 42)
        };
        card.Controls.Add(licenseHint);

        _licenseBox = new RichTextBox
        {
            Location = new Point(22, 69),
            Size = new Size(592, 151),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(9, 11, 17),
            ForeColor = Color.FromArgb(222, 221, 230),
            DetectUrls = true,
            TabStop = false,
            Font = new Font("Segoe UI", 9F)
        };
        card.Controls.Add(_licenseBox);

        _acceptLicense = new CheckBox
        {
            AutoSize = true,
            Text = "I have read and accept the license terms",
            ForeColor = Color.FromArgb(240, 238, 247),
            Location = new Point(22, 232),
            Cursor = Cursors.Hand
        };
        _acceptLicense.CheckedChanged += (_, _) => UpdatePrimaryButtonState();
        card.Controls.Add(_acceptLicense);

        _installLocationLabel = new Label
        {
            AutoSize = true,
            Text = "Install location",
            ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 9F),
            Location = new Point(22, 267)
        };
        card.Controls.Add(_installLocationLabel);

        _installPath = new TextBox
        {
            Location = new Point(22, 291),
            Size = new Size(463, 29),
            Text = InstallerEngine.GetDefaultInstallDirectory(),
            BackColor = Color.FromArgb(9, 11, 17),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_installPath);

        _browseButton = CreateSecondaryButton("Browse", new Point(496, 289), new Size(118, 34));
        _browseButton.Click += BrowseButton_Click;
        card.Controls.Add(_browseButton);

        _startMenuShortcut = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Start menu shortcut",
            ForeColor = Color.FromArgb(235, 233, 242),
            Location = new Point(22, 337),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_startMenuShortcut);

        _desktopShortcut = new CheckBox
        {
            AutoSize = true,
            Checked = false,
            Text = "Desktop shortcut",
            ForeColor = Color.FromArgb(235, 233, 242),
            Location = new Point(210, 337),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_desktopShortcut);

        _progressBar = new PremiumProgressBar
        {
            Location = new Point(32, 530),
            Size = new Size(636, 7),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            TrackColor = SurfaceRaised,
            ProgressColor = Accent
        };
        main.Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            AutoEllipsis = true,
            Text = uninstallMode
                ? "The same Setup executable removes SNAPVERE. Your captures remain untouched."
                : "Ready to install. Normal launch stays quietly in the notification area.",
            ForeColor = Muted,
            Location = new Point(32, 550),
            Size = new Size(430, 42)
        };
        main.Controls.Add(_statusLabel);

        _launchAfterInstall = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Launch SNAPVERE in the tray now",
            ForeColor = Color.FromArgb(235, 233, 242),
            Location = new Point(32, 605),
            Visible = false,
            Cursor = Cursors.Hand
        };
        main.Controls.Add(_launchAfterInstall);

        _cancelButton = CreateSecondaryButton("Cancel", new Point(448, 592), new Size(102, 40));
        _cancelButton.Click += (_, _) => Close();
        main.Controls.Add(_cancelButton);

        _primaryButton = CreatePrimaryButton(
            uninstallMode ? "Remove" : "Install",
            new Point(560, 592),
            new Size(108, 40));
        _primaryButton.Click += PrimaryButton_Click;
        main.Controls.Add(_primaryButton);

        AcceptButton = _primaryButton;
        CancelButton = _cancelButton;

        if (uninstallMode)
        {
            ConfigureUninstallMode(card, licenseHint);
        }
        else
        {
            LoadLicense();
        }

        UpdatePrimaryButtonState();
    }

    private static Panel BuildSidebar(bool uninstallMode)
    {
        var sidebar = new GradientPanel
        {
            Dock = DockStyle.Left,
            Width = 280,
            StartColor = Sidebar,
            EndColor = Color.FromArgb(17, 14, 29)
        };

        var mark = new BrandMarkControl
        {
            Location = new Point(30, 34),
            Size = new Size(54, 54)
        };
        sidebar.Controls.Add(mark);

        var brand = new Label
        {
            AutoSize = true,
            Text = "SNAPVERE",
            ForeColor = Color.FromArgb(247, 245, 255),
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            Location = new Point(30, 104)
        };
        sidebar.Controls.Add(brand);

        var tagline = new Label
        {
            AutoSize = true,
            Text = "Capture. Edit. Done.",
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Location = new Point(31, 135)
        };
        sidebar.Controls.Add(tagline);

        var version = new Label
        {
            AutoSize = true,
            Text = $"Version {InstallerEngine.VersionText}",
            ForeColor = Subtle,
            Font = new Font("Segoe UI", 8.5F),
            Location = new Point(31, 160)
        };
        sidebar.Controls.Add(version);

        var statement = new Label
        {
            AutoSize = false,
            Text = uninstallMode
                ? "Clean removal.\r\nCaptures stay yours."
                : "Private capture,\r\nready when you are.",
            ForeColor = Color.FromArgb(235, 232, 244),
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(30, 213),
            Size = new Size(210, 68)
        };
        sidebar.Controls.Add(statement);

        AddSidebarFeature(sidebar, 323, "TRAY-FIRST", "Starts quietly. Print Screen opens Region Capture.", Accent);
        AddSidebarFeature(sidebar, 403, "LOCAL-FIRST", "No account, telemetry or cloud upload required.", Cyan);
        AddSidebarFeature(sidebar, 483, "SELF-CONTAINED", "Per-user install with its required app runtime.", Accent);

        var footerDot = new Label
        {
            AutoSize = true,
            Text = "●",
            ForeColor = Success,
            Font = new Font("Segoe UI", 8F),
            Location = new Point(30, 607)
        };
        sidebar.Controls.Add(footerDot);

        var footer = new Label
        {
            AutoSize = true,
            Text = "Brendigo  •  Windows",
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F),
            Location = new Point(47, 608)
        };
        sidebar.Controls.Add(footer);

        return sidebar;
    }

    private static void AddSidebarFeature(Panel sidebar, int top, string title, string description, Color accentColor)
    {
        var indicator = new Panel
        {
            Location = new Point(30, top + 2),
            Size = new Size(3, 45),
            BackColor = accentColor
        };
        sidebar.Controls.Add(indicator);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            ForeColor = accentColor,
            Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
            Location = new Point(44, top)
        };
        sidebar.Controls.Add(titleLabel);

        var descriptionLabel = new Label
        {
            AutoSize = false,
            Text = description,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.8F),
            Location = new Point(44, top + 21),
            Size = new Size(200, 43)
        };
        sidebar.Controls.Add(descriptionLabel);
    }

    private void ConfigureUninstallMode(Panel card, Label licenseHint)
    {
        _licenseLabel.Visible = false;
        licenseHint.Visible = false;
        _licenseBox.Visible = false;
        _acceptLicense.Visible = false;
        _installLocationLabel.Visible = false;
        _installPath.Visible = false;
        _browseButton.Visible = false;
        _startMenuShortcut.Visible = false;
        _desktopShortcut.Visible = false;

        var removalTitle = new Label
        {
            AutoSize = true,
            Text = "What will be removed",
            ForeColor = Color.FromArgb(247, 245, 255),
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(24, 28)
        };
        card.Controls.Add(removalTitle);

        var message = new Label
        {
            AutoSize = false,
            Text = "SNAPVERE application files\r\nStart menu and Desktop shortcuts created by Setup\r\nWindows Installed apps registration\r\nWindows startup registration owned by this installation\r\n\r\nYour screenshots in Pictures\\SNAPVERE are preserved.",
            ForeColor = Color.FromArgb(224, 222, 232),
            Font = new Font("Segoe UI", 11F),
            Location = new Point(25, 82),
            Size = new Size(570, 182)
        };
        card.Controls.Add(message);

        var privacy = new RoundedPanel
        {
            Location = new Point(24, 283),
            Size = new Size(570, 56),
            BackColor = Color.FromArgb(17, 38, 34),
            BorderColor = Color.FromArgb(51, 109, 91),
            CornerRadius = 12
        };
        privacy.Controls.Add(new Label
        {
            AutoSize = false,
            Text = "●   Capture files are not part of uninstall cleanup.",
            ForeColor = Success,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Location = new Point(14, 17),
            Size = new Size(530, 24)
        });
        card.Controls.Add(privacy);
    }

    private void LoadLicense()
    {
        try
        {
            _licenseBox.Text = InstallerEngine.ReadLicenseText();
        }
        catch (Exception exception)
        {
            _licenseBox.Text = exception.Message;
            _acceptLicense.Enabled = false;
            _statusLabel.Text = "Setup cannot continue because the license resource is unavailable.";
        }
    }

    private async void PrimaryButton_Click(object? sender, EventArgs e)
    {
        if (_completed)
        {
            if (!_uninstallMode && _launchAfterInstall.Checked)
            {
                if (!InstallerEngine.LaunchInstalledApplication(_installPath.Text))
                {
                    MessageBox.Show(
                        this,
                        "SNAPVERE was installed, but Windows could not start the application.\r\n\r\nTry launching SNAPVERE again from the Start menu. If it still fails, check %LOCALAPPDATA%\\SNAPVERE\\Logs\\startup.log.",
                        "SNAPVERE startup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            Close();
            return;
        }

        SetBusy(true);
        _progressBar.Value = 0;
        _statusLabel.Text = _uninstallMode ? "Removing SNAPVERE…" : "Preparing secure local installation…";

        InstallerResult result;
        if (_uninstallMode)
        {
            result = await Task.Run(() => InstallerEngine.Uninstall(silent: false));
        }
        else
        {
            var installPath = _installPath.Text;
            var startMenu = _startMenuShortcut.Checked;
            var desktop = _desktopShortcut.Checked;
            result = await Task.Run(() => InstallerEngine.Install(
                installPath,
                startMenu,
                desktop,
                silent: false,
                progress => BeginInvoke(() => _progressBar.Value = Math.Clamp(progress, 0, 100))));
        }

        _statusLabel.Text = result.Message;
        if (!result.Succeeded)
        {
            _progressBar.Value = 0;
            SetBusy(false);
            MessageBox.Show(this, result.Message, "SNAPVERE Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _progressBar.Value = 100;
        _completed = true;
        _titleLabel.Text = _uninstallMode ? "SNAPVERE removed" : "SNAPVERE is ready";
        _subtitleLabel.Text = _uninstallMode
            ? "The application has been removed from this Windows account. Your screenshots remain untouched."
            : "Installation completed successfully. SNAPVERE can now stay ready in your notification area.";
        _primaryButton.Text = "Finish";
        _primaryButton.Enabled = true;
        _cancelButton.Visible = false;
        _launchAfterInstall.Visible = !_uninstallMode;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose where SNAPVERE should be installed",
            SelectedPath = _installPath.Text,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _installPath.Text = Path.Combine(dialog.SelectedPath, "SNAPVERE");
        }
    }

    private void SetBusy(bool busy)
    {
        _primaryButton.Enabled = !busy && (_uninstallMode || _acceptLicense.Checked || _completed);
        _cancelButton.Enabled = !busy;
        _acceptLicense.Enabled = !busy;
        _installPath.Enabled = !busy;
        _browseButton.Enabled = !busy;
        _startMenuShortcut.Enabled = !busy;
        _desktopShortcut.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void UpdatePrimaryButtonState()
    {
        _primaryButton.Enabled = _uninstallMode || _acceptLicense.Checked || _completed;
    }

    private static Button CreatePrimaryButton(string text, Point location, Size size)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = size,
            BackColor = AccentStrong,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
        };
        button.FlatAppearance.BorderColor = Accent;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = AccentHover;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(92, 72, 190);
        return button;
    }

    private static Button CreateSecondaryButton(string text, Point location, Size size)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = size,
            BackColor = SurfaceRaised,
            ForeColor = Color.FromArgb(241, 239, 248),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI Semibold", 9.5F)
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 36, 49);
        return button;
    }

    private sealed class GradientPanel : Panel
    {
        public Color StartColor { get; init; } = Sidebar;
        public Color EndColor { get; init; } = Surface;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(ClientRectangle, StartColor, EndColor, 45F);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    private sealed class RoundedPanel : Panel
    {
        public int CornerRadius { get; init; } = 16;
        public Color BorderColor { get; init; } = Border;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            using var path = CreateRoundedRectangle(rect, CornerRadius);
            using var pen = new Pen(BorderColor, 1F);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            using var path = CreateRoundedRectangle(ClientRectangle, CornerRadius);
            Region = new Region(path);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, int radius)
        {
            var path = new GraphicsPath();
            var diameter = Math.Max(2, radius * 2);
            var arc = new Rectangle(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class BrandMarkControl : Control
    {
        public BrandMarkControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using var path = RoundedPanel.CreateRoundedRectangle(rect, 14);
            using var gradient = new LinearGradientBrush(
                rect,
                Color.FromArgb(97, 74, 232),
                Color.FromArgb(54, 182, 213),
                45F);
            e.Graphics.FillPath(gradient, path);
            using var borderPen = new Pen(Color.FromArgb(165, 155, 255), 1F);
            e.Graphics.DrawPath(borderPen, path);

            using var shardPen = new Pen(Color.FromArgb(247, 245, 255), 6F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            e.Graphics.DrawLine(shardPen, 18, 38, 35, 15);

            using var highlightPen = new Pen(Color.FromArgb(210, 205, 255), 3F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            e.Graphics.DrawLine(highlightPen, 27, 35, 39, 20);
        }
    }

    private sealed class PremiumProgressBar : Control
    {
        private int _minimum;
        private int _maximum = 100;
        private int _value;

        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                _value = Math.Max(_value, _minimum);
                Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(value, _minimum + 1);
                _value = Math.Min(_value, _maximum);
                Invalidate();
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, _minimum, _maximum);
                Invalidate();
            }
        }

        public Color TrackColor { get; init; } = SurfaceRaised;
        public Color ProgressColor { get; init; } = Accent;

        public PremiumProgressBar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using var trackPath = RoundedPanel.CreateRoundedRectangle(rect, Math.Max(2, Height / 2));
            using var trackBrush = new SolidBrush(TrackColor);
            e.Graphics.FillPath(trackBrush, trackPath);

            var ratio = (_value - _minimum) / (double)(_maximum - _minimum);
            var progressWidth = (int)Math.Round(rect.Width * ratio);
            if (progressWidth <= 0)
            {
                return;
            }

            var progressRect = new Rectangle(rect.X, rect.Y, Math.Max(1, progressWidth), rect.Height);
            using var progressPath = RoundedPanel.CreateRoundedRectangle(progressRect, Math.Max(2, Height / 2));
            using var gradient = new LinearGradientBrush(
                progressRect,
                ProgressColor,
                Cyan,
                LinearGradientMode.Horizontal);
            e.Graphics.FillPath(gradient, progressPath);
        }
    }
}
