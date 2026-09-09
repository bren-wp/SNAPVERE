using System.Drawing;

namespace Snapvere.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Surface = Color.FromArgb(18, 20, 26);
    private static readonly Color Card = Color.FromArgb(29, 32, 41);
    private static readonly Color Accent = Color.FromArgb(103, 80, 216);
    private static readonly Color Muted = Color.FromArgb(168, 174, 189);

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
    private bool _completed;

    public SetupForm(bool uninstallMode)
    {
        _uninstallMode = uninstallMode;

        Text = uninstallMode ? "Remove SNAPVERE" : "SNAPVERE Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ClientSize = new Size(760, 590);
        BackColor = Surface;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 108,
            Padding = new Padding(28, 22, 28, 16),
            BackColor = Color.FromArgb(22, 24, 31)
        };
        Controls.Add(header);

        var brand = new Label
        {
            AutoSize = true,
            Text = "SNAPVERE",
            ForeColor = Accent,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Location = new Point(28, 20)
        };
        header.Controls.Add(brand);

        _titleLabel = new Label
        {
            AutoSize = true,
            Text = uninstallMode ? "Remove SNAPVERE" : $"Install SNAPVERE {InstallerEngine.VersionText}",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            Location = new Point(25, 42)
        };
        header.Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            AutoSize = true,
            Text = uninstallMode
                ? "Remove the application and its Windows registration from this account."
                : "Fast, private screen capture for Windows — installed per user, without administrator access.",
            ForeColor = Muted,
            Location = new Point(29, 80)
        };
        header.Controls.Add(_subtitleLabel);

        var card = new Panel
        {
            Location = new Point(28, 128),
            Size = new Size(704, 365),
            BackColor = Card,
            Padding = new Padding(22)
        };
        Controls.Add(card);

        _licenseBox = new RichTextBox
        {
            Location = new Point(22, 38),
            Size = new Size(660, 190),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(14, 16, 21),
            ForeColor = Color.FromArgb(218, 222, 232),
            DetectUrls = true,
            TabStop = false
        };
        card.Controls.Add(_licenseBox);

        var licenseLabel = new Label
        {
            AutoSize = true,
            Text = "Mozilla Public License 2.0",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            Location = new Point(20, 14)
        };
        card.Controls.Add(licenseLabel);

        _acceptLicense = new CheckBox
        {
            AutoSize = true,
            Text = "I accept the license terms",
            ForeColor = Color.White,
            Location = new Point(22, 238)
        };
        _acceptLicense.CheckedChanged += (_, _) => UpdatePrimaryButtonState();
        card.Controls.Add(_acceptLicense);

        var installLocationLabel = new Label
        {
            AutoSize = true,
            Text = "Install location",
            ForeColor = Muted,
            Location = new Point(22, 278)
        };
        card.Controls.Add(installLocationLabel);

        _installPath = new TextBox
        {
            Location = new Point(22, 301),
            Size = new Size(535, 28),
            Text = InstallerEngine.GetDefaultInstallDirectory(),
            BackColor = Color.FromArgb(14, 16, 21),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_installPath);

        _browseButton = CreateSecondaryButton("Browse…", new Point(568, 299), new Size(114, 30));
        _browseButton.Click += BrowseButton_Click;
        card.Controls.Add(_browseButton);

        _startMenuShortcut = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Start menu shortcut",
            ForeColor = Color.White,
            Location = new Point(22, 339)
        };
        card.Controls.Add(_startMenuShortcut);

        _desktopShortcut = new CheckBox
        {
            AutoSize = true,
            Checked = false,
            Text = "Desktop shortcut",
            ForeColor = Color.White,
            Location = new Point(210, 339)
        };
        card.Controls.Add(_desktopShortcut);

        _progressBar = new ProgressBar
        {
            Location = new Point(28, 512),
            Size = new Size(704, 8),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous
        };
        Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            AutoEllipsis = true,
            Text = uninstallMode
                ? "SNAPVERE can be removed without a separate uninstaller executable."
                : "Setup is self-contained. No telemetry or network connection is required.",
            ForeColor = Muted,
            Location = new Point(28, 530),
            Size = new Size(470, 28)
        };
        Controls.Add(_statusLabel);

        _launchAfterInstall = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Launch SNAPVERE",
            ForeColor = Color.White,
            Location = new Point(28, 558),
            Visible = false
        };
        Controls.Add(_launchAfterInstall);

        _cancelButton = CreateSecondaryButton("Cancel", new Point(512, 542), new Size(100, 36));
        _cancelButton.Click += (_, _) => Close();
        Controls.Add(_cancelButton);

        _primaryButton = CreatePrimaryButton(
            uninstallMode ? "Uninstall" : "Install",
            new Point(622, 542),
            new Size(110, 36));
        _primaryButton.Click += PrimaryButton_Click;
        Controls.Add(_primaryButton);

        if (uninstallMode)
        {
            ConfigureUninstallMode(card);
        }
        else
        {
            LoadLicense();
        }

        UpdatePrimaryButtonState();
    }

    private void ConfigureUninstallMode(Panel card)
    {
        _licenseBox.Visible = false;
        _acceptLicense.Visible = false;
        _installPath.Visible = false;
        _browseButton.Visible = false;
        _startMenuShortcut.Visible = false;
        _desktopShortcut.Visible = false;

        foreach (Control control in card.Controls)
        {
            if (control is Label label && label.Text is "Mozilla Public License 2.0" or "Install location")
            {
                label.Visible = false;
            }
        }

        var message = new Label
        {
            AutoSize = false,
            Text = "This removes SNAPVERE application files, Start menu/Desktop shortcuts created by Setup, and the per-user Windows uninstall registration.\r\n\r\nYour screenshots in Pictures\\SNAPVERE are not deleted.",
            ForeColor = Color.FromArgb(224, 227, 235),
            Font = new Font("Segoe UI", 12F),
            Location = new Point(22, 36),
            Size = new Size(650, 130)
        };
        card.Controls.Add(message);
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
                _ = InstallerEngine.LaunchInstalledApplication(_installPath.Text);
            }

            Close();
            return;
        }

        SetBusy(true);
        _progressBar.Value = 0;
        _statusLabel.Text = _uninstallMode ? "Removing SNAPVERE…" : "Preparing installation…";

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
            ? "The application has been removed from this Windows account."
            : "Installation completed successfully.";
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
        => new()
        {
            Text = text,
            Location = location,
            Size = size,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

    private static Button CreateSecondaryButton(string text, Point location, Size size)
        => new()
        {
            Text = text,
            Location = location,
            Size = size,
            BackColor = Color.FromArgb(43, 47, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
}
