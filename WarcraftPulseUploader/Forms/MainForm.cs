// Forms/MainForm.cs
using WarcraftPulseUploader.Services;

namespace WarcraftPulseUploader.Forms;

public partial class MainForm : Form
{
    private AppSettings _settings = AppSettings.Load();

    public MainForm()
    {
        InitializeComponent();

        var version = System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version;
        Text = $"WarcraftPulse Uploader v{version?.Major}.{version?.Minor}.{version?.Build}";
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        UpdateTokenWarning();
    }

    private void btnSettings_Click(object sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            UpdateTokenWarning();
            // TODO Task 8: restart LogWatcher with (possibly new) directory
        }
    }

    private void SetStatus(string message)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(message)); return; }
        lblStatus.Text = message;
    }

    private void UpdateTokenWarning()
    {
        lblTokenWarning.Visible = string.IsNullOrEmpty(_settings.ApiToken);
    }

    private void btnUpload_Click(object sender, EventArgs e) { }
    private void btnOpenReport_Click(object sender, EventArgs e) { }
}
