using System.Drawing;

namespace Snapvere.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Canvas = Color.FromArgb(12, 14, 19);
    private static readonly Color Sidebar = Color.FromArgb(17, 19, 26);
    private static readonly Color Surface = Color.FromArgb(24, 27, 36);
    private static readonly Color SurfaceRaised = Color.FromArgb(31, 35, 46);
    private static readonly Color Accent = Color.FromArgb(124, 108, 255);
    private static readonly Color AccentHover = Color.FromArgb(139, 125, 255);
    private static readonly Color Muted = Color.FromArgb(154, 162, 180);
    private static readonly Color Border = Color.FromArgb(46, 52, 67);
    private static readonly Color Success = Color.FromArgb(69, 214, 162);

    private readonly bool _uninstallMode;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly RichTextBox _licenseBox;
    private readonly CheckBox _acceptLicense;
    private readonly TextBox _installPath;
    private readonly Button _browseButton;
    private readonly CheckBox _startMenuShortcut;
    private readonly CheckBox _desktopShortcut;
    private readonly ProgressBar _progressBar;
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
        ClientSize = new Size(920, 620);
        MinimumSize = new Size(936, 659);
        MaximumSize = new Size(936, 659);
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

        var brandEyebrow = new Label
        {
            AutoSize = true,
            Text = uninstallMode ? "MAINTENANCE" : "INSTALLATION",
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            Location = new Point(30, 28)
        };
        main.Controls.Add(brandEyebrow);

        _titleLabel = new Label
        {
            AutoSize = true,
            Text = uninstallMode ? "Remove SNAPVERE" : $"Install SNAPVERE {InstallerEngine.VersionText}",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            Location = new Point(26, 48)
        };
        main.Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            AutoSize = false,
            Text = uninstallMode
                ? "Remove the application and Windows registration while keeping your screenshots."
                : "A local-first, self-contained Windows install. No account, telemetry, or administrator access is required.",
            ForeColor = Muted,
            Location = new Point(30, 88),
            Size = new Size(600, 34)
        };
        main.Controls.Add(_subtitleLabel);

        var card = new Panel
        {
            Location = new Point(28, 132),
            Size = new Size(614, 360),
            BackColor = Surface,
            Padding = new Padding(22)
        };
        main.Controls.Add(card);

        _licenseLabel = new Label
        {
            AutoSize = true,
            Text = "Mozilla Public License 2.0",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            Location = new Point(22, 16)
        };
        card.Controls.Add(_licenseLabel);

        var licenseHint = new Label
        {
            AutoSize = true,
            Text = "Review the terms before continuing",
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9F),
            Location = new Point(22, 38)
        };
        card.Controls.Add(licenseHint);

        _licenseBox = new RichTextBox
        {
            Location = new Point(22, 64),
            Size = new Size(570, 154),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(13, 15, 20),
            ForeColor = Color.FromArgb(222, 225, 234),
            DetectUrls = true,
            TabStop = false,
            Font = new Font("Segoe UI", 9F)
        };
        card.Controls.Add(_licenseBox);

        _acceptLicense = new CheckBox
        {
            AutoSize = true,
            Text = "I have read and accept the license terms",
            ForeColor = Color.White,
            Location = new Point(22, 230),
            Cursor = Cursors.Hand
        };
        _acceptLicense.CheckedChanged += (_, _) => UpdatePrimaryButtonState();
        card.Controls.Add(_acceptLicense);

        _installLocationLabel = new Label
        {
            AutoSize = true,
            Text = "Install location",
            ForeColor = Muted,
            Location = new Point(22, 265)
        };
        card.Controls.Add(_installLocationLabel);

        _installPath = new TextBox
        {
            Location = new Point(22, 288),
            Size = new Size(448, 29),
            Text = InstallerEngine.GetDefaultInstallDirectory(),
            BackColor = Color.FromArgb(14, 16, 22),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_installPath);

        _browseButton = CreateSecondaryButton("Browse", new Point(480, 286), new Size(112, 32));
        _browseButton.Click += BrowseButton_Click;
        card.Controls.Add(_browseButton);

        _startMenuShortcut = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Start menu shortcut",
            ForeColor = Color.White,
            Location = new Point(22, 329),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_startMenuShortcut);

        _desktopShortcut = new CheckBox
        {
            AutoSize = true,
            Checked = false,
            Text = "Desktop shortcut",
            ForeColor = Color.White,
            Location = new Point(208, 329),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(_desktopShortcut);

        _progressBar = new ProgressBar
        {
            Location = new Point(28, 510),
            Size = new Size(614, 7),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous
        };
        main.Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            AutoEllipsis = true,
            Text = uninstallMode
                ? "Same Setup executable removes SNAPVERE; there is no separate uninstall.exe."
                : "Ready to install locally. Screenshots stay under Pictures\\SNAPVERE.",
            ForeColor = Muted,
            Location = new Point(28, 530),
            Size = new Size(390, 40)
        };
        main.Controls.Add(_statusLabel);

        _launchAfterInstall = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Launch SNAPVERE now",
            ForeColor = Color.White,
            Location = new Point(28, 574),
            Visible = false,
            Cursor = Cursors.Hand
        };
        main.Controls.Add(_launchAfterInstall);

        _cancelButton = CreateSecondaryButton("Cancel", new Point(424, 554), new Size(100, 38));
        _cancelButton.Click += (_, _) => Close();
        main.Controls.Add(_cancelButton);

        _primaryButton = CreatePrimaryButton(
            uninstallMode ? "Remove" : "Install",
            new Point(534, 554),
            new Size(108, 38));
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
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 250,
            BackColor = Sidebar
        };

        var accentBar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = Accent
        };
        sidebar.Controls.Add(accentBar);

        var mark = new Label
        {
            AutoSize = false,
            Text = "S",
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(28, 32),
            Size = new Size(46, 46),
            BackColor = Accent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold)
        };
        sidebar.Controls.Add(mark);

        var brand = new Label
        {
            AutoSize = true,
            Text = "SNAPVERE",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            Location = new Point(28, 94)
        };
        sidebar.Controls.Add(brand);

        var version = new Label
        {
            AutoSize = true,
            Text = $"Version {InstallerEngine.VersionText}",
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9F),
            Location = new Point(29, 121)
        };
        sidebar.Controls.Add(version);

        var tagline = new Label
        {
            AutoSize = false,
            Text = uninstallMode
                ? "Clean removal, without touching your captures."
                : "Capture anything.\r\nPrivate by default.",
            ForeColor = Color.FromArgb(214, 218, 229),
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            Location = new Point(28, 174),
            Size = new Size(190, 58)
        };
        sidebar.Controls.Add(tagline);

        AddSidebarFeature(sidebar, 270, "SELF-CONTAINED", "Includes its required app runtime.");
        AddSidebarFeature(sidebar, 340, "PER-USER", "No administrator elevation by default.");
        AddSidebarFeature(sidebar, 410, "LOCAL-FIRST", "No account or telemetry required.");

        var footerDot = new Label
        {
            AutoSize = true,
            Text = "●",
            ForeColor = Success,
            Font = new Font("Segoe UI", 8F),
            Location = new Point(28, 563)
        };
        sidebar.Controls.Add(footerDot);

        var footer = new Label
        {
            AutoSize = true,
            Text = "Brendigo · Windows",
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F),
            Location = new Point(44, 564)
        };
        sidebar.Controls.Add(footer);

        return sidebar;
    }

    private static void AddSidebarFeature(Panel sidebar, int top, string title, string description)
    {
        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
            Location = new Point(28, top)
        };
        sidebar.Controls.Add(titleLabel);

        var descriptionLabel = new Label
        {
            AutoSize = false,
            Text = description,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9F),
            Location = new Point(28, top + 21),
            Size = new Size(188, 38)
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
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Location = new Point(24, 28)
        };
        card.Controls.Add(removalTitle);

        var message = new Label
        {
            AutoSize = false,
            Text = "SNAPVERE application files\r\nStart menu and Desktop shortcuts created by Setup\r\nWindows Installed apps registration\r\n\r\nYour screenshots in Pictures\\SNAPVERE are preserved.",
            ForeColor = Color.FromArgb(224, 227, 235),
            Font = new Font("Segoe UI", 11F),
            Location = new Point(25, 82),
            Size = new Size(550, 170)
        };
        card.Controls.Add(message);

        var privacy = new Label
        {
            AutoSize = false,
            Text = "✓  Capture files are not part of the uninstall cleanup.",
            ForeColor = Success,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Location = new Point(25, 278),
            Size = new Size(540, 30)
        };
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
            : "Installation completed successfully. SNAPVERE is ready to launch.";
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
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AccentHover;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(92, 78, 210);
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
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = new Font("Segoe UI", 9.5F)
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 44, 57);
        return button;
    }
}
