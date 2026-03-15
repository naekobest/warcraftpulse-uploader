// Forms/SettingsForm.cs
using WarcraftPulseUploader.Native;
using WarcraftPulseUploader.Services;

namespace WarcraftPulseUploader.Forms;

public partial class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly UploadService _uploader;
    private CancellationTokenSource? _testCts;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkMode.ApplyToWindow(Handle);
        DarkMode.ApplyToControl(chkAutoUpload.Handle);
        DarkMode.ApplyToControl(chkMinimizeToTray.Handle);
        DarkMode.ApplyToControl(chkStartWithWindows.Handle);
    }

    public SettingsForm(AppSettings settings, UploadService uploader)
    {
        InitializeComponent();
        _settings = settings;
        _uploader = uploader;

        txtApiToken.Text            = settings.ApiToken;
        txtLogDir.Text              = settings.WowLogDirectory;
        chkAutoUpload.Checked       = settings.AutoUpload;
        chkMinimizeToTray.Checked   = settings.MinimizeToTray;
        chkStartWithWindows.Checked = settings.StartWithWindows;

        // Show persistent connected badge if we have a previously validated username
        if (!string.IsNullOrEmpty(settings.ValidatedUserName))
        {
            lblConnectedBadge.Text    = $"✓ {settings.ValidatedUserName}";
            lblConnectedBadge.Visible = true;
            btnTestToken.Text         = "Re-test";
        }

        // Clear badge when token text changes
        txtApiToken.TextChanged += (_, _) =>
        {
            lblConnectedBadge.Visible = false;
            btnTestToken.Text         = "Test";
        };
    }

    private void btnShowHide_Click(object sender, EventArgs e)
    {
        txtApiToken.UseSystemPasswordChar = !txtApiToken.UseSystemPasswordChar;
        btnShowHide.Text = txtApiToken.UseSystemPasswordChar ? "Show" : "Hide";
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        string preferredStart = @"C:\Program Files (x86)\World of Warcraft";
        dlg.InitialDirectory = Directory.Exists(preferredStart)
            ? preferredStart
            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        if (dlg.ShowDialog() == DialogResult.OK)
            txtLogDir.Text = dlg.SelectedPath;
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        string newToken = txtApiToken.Text.Trim();
        if (newToken != _settings.ApiToken)
            _settings.ValidatedUserName = string.Empty;
        _settings.ApiToken        = newToken;
        _settings.WowLogDirectory = txtLogDir.Text.Trim();
        _settings.AutoUpload        = chkAutoUpload.Checked;
        _settings.MinimizeToTray    = chkMinimizeToTray.Checked;
        _settings.StartWithWindows  = chkStartWithWindows.Checked;
        ApplyStartWithWindows(_settings.StartWithWindows);
        _settings.Save();
        // Clear token from memory after persisting via DPAPI
        txtApiToken.Text = string.Empty;
        DialogResult = DialogResult.OK;
        Close();
    }

    internal static void ApplyStartWithWindows(bool enable)
    {
        const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string AppName = "WarcraftPulseUploader";
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        if (key is null) return;
        if (enable)
        {
            var exePath = $"\"{Application.ExecutablePath}\"";
            key.SetValue(AppName, exePath);
        }
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _testCts?.Cancel();
        _testCts?.Dispose();
        base.OnFormClosed(e);
    }

    private async void btnTestToken_Click(object sender, EventArgs e)
    {
        _testCts?.Cancel();
        _testCts?.Dispose();
        _testCts = new CancellationTokenSource();

        btnTestToken.Enabled = false;
        btnTestToken.Text    = "Testing…";

        try
        {
            var (success, message) = await _uploader.TestTokenAsync(txtApiToken.Text.Trim(), _testCts.Token);

            if (IsDisposed) return;

            if (success)
            {
                lblTokenStatus.ForeColor    = System.Drawing.Color.FromArgb(74, 222, 128);
                lblTokenStatus.Text         = $"✓ Connected as {message}";
                lblConnectedBadge.Text      = $"✓ {message}";
                lblConnectedBadge.Visible   = true;
                btnTestToken.Text           = "Re-test";
                _settings.ValidatedUserName = message;
            }
            else
            {
                lblTokenStatus.ForeColor = System.Drawing.Color.FromArgb(248, 113, 113);
                lblTokenStatus.Text      = $"✕ {message}";
            }
        }
        catch (TaskCanceledException) { return; }
        finally
        {
            if (!IsDisposed)
            {
                btnTestToken.Enabled = true;
                if (btnTestToken.Text == "Testing…")
                    btnTestToken.Text = "Test";
            }
        }
    }
}
