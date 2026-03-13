// Forms/SettingsForm.cs
using System.Net.Http.Json;
using WarcraftPulseUploader.Services;

namespace WarcraftPulseUploader.Forms;

public partial class SettingsForm : Form
{
    private readonly AppSettings _settings;

    public SettingsForm(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        txtApiToken.Text   = settings.ApiToken;
        txtServerUrl.Text  = settings.ServerUrl;
        txtLogDir.Text     = settings.WowLogDirectory;
        chkAutoUpload.Checked       = settings.AutoUpload;
        chkMinimizeToTray.Checked   = settings.MinimizeToTray;
        chkStartWithWindows.Checked = settings.StartWithWindows;
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
        _settings.ApiToken         = txtApiToken.Text.Trim();
        _settings.ServerUrl        = txtServerUrl.Text.Trim();
        _settings.WowLogDirectory  = txtLogDir.Text.Trim();
        _settings.AutoUpload        = chkAutoUpload.Checked;
        _settings.MinimizeToTray    = chkMinimizeToTray.Checked;
        _settings.StartWithWindows  = chkStartWithWindows.Checked;
        ApplyStartWithWindows(_settings.StartWithWindows);
        _settings.Save();
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
            key.SetValue(AppName, Application.ExecutablePath);
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private async void btnTestToken_Click(object sender, EventArgs e)
    {
        btnTestToken.Enabled = false;
        btnTestToken.Text    = "Testing…";

        using var http    = new HttpClient { BaseAddress = new Uri(txtServerUrl.Text.Trim().TrimEnd('/') + '/') };
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/user");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", txtApiToken.Text.Trim());

        try
        {
            var response = await http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                string name = json.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
                lblTokenStatus.ForeColor = System.Drawing.Color.FromArgb(74, 222, 128);
                lblTokenStatus.Text      = $"✓ Connected as {name}";
            }
            else
            {
                lblTokenStatus.ForeColor = System.Drawing.Color.FromArgb(248, 113, 113);
                lblTokenStatus.Text      = $"✕ Token rejected (HTTP {(int)response.StatusCode})";
            }
        }
        catch (Exception ex)
        {
            lblTokenStatus.ForeColor = System.Drawing.Color.FromArgb(248, 113, 113);
            lblTokenStatus.Text      = $"✕ Could not reach server: {ex.Message}";
        }
        finally
        {
            btnTestToken.Enabled = true;
            btnTestToken.Text    = "Test";
        }
    }
}
