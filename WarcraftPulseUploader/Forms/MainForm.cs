// Forms/MainForm.cs
using WarcraftPulseUploader.Services;

namespace WarcraftPulseUploader.Forms;

public partial class MainForm : Form
{
    private AppSettings _settings = AppSettings.Load();
    private LogWatcher? _watcher;
    private CancellationTokenSource? _cts;

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
        StartWatcher();
        UpdateTokenWarning();
    }

    private void btnSettings_Click(object sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            UpdateTokenWarning();
            StartWatcher();
        }
    }

    private void StartWatcher()
    {
        _watcher?.Dispose();
        if (string.IsNullOrEmpty(_settings.WowLogDirectory)) return;

        _watcher = new LogWatcher(_settings.WowLogDirectory);
        _watcher.NewLogDetected += OnNewLogDetected;
        _watcher.Start();

        SetStatus("Watching for new logs…");
    }

    // FSW fires on a background thread — Invoke back to UI thread immediately.
    private void OnNewLogDetected(object? sender, string filePath)
    {
        Invoke(() => _ = ProcessLogAsync(filePath, fromWatcher: true));
    }

    // Called from UI thread; kicks off async work.
    private async Task ProcessLogAsync(string filePath, bool fromWatcher = false)
    {
        if (string.IsNullOrEmpty(_settings.ApiToken))
        {
            SetStatus("Error: No API token configured. Open Settings → Access Tokens.");
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        btnUpload.Enabled = false;

        if (fromWatcher)
        {
            SetStatus($"Waiting for WoW to finish writing {Path.GetFileName(filePath)}…");
            bool stable = await WaitForFileStable(filePath, stableMs: 5000, ct);
            if (!stable) { SetStatus("Timed out waiting for log file. Upload manually."); btnUpload.Enabled = true; return; }
        }

        SetStatus($"Parsing: {Path.GetFileName(filePath)}…");
        // Parsing and upload wired in Task 10
        btnUpload.Enabled = true;
    }

    /// <summary>
    /// Waits until the file's LastWriteTime stops changing for <paramref name="stableMs"/> ms.
    /// Returns false if cancelled. Checks every 2 seconds; gives up after 10 minutes.
    /// </summary>
    private static async Task<bool> WaitForFileStable(string path, int stableMs = 5000, CancellationToken ct = default)
    {
        DateTime lastWrite = default;
        int stableCount    = 0;
        int maxChecks      = 300;  // 300 × 2s = 10 minutes

        for (int i = 0; i < maxChecks; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(2000, ct);

            DateTime current = File.GetLastWriteTimeUtc(path);
            if (current == lastWrite)
            {
                stableCount += 2000;
                if (stableCount >= stableMs) return true;
            }
            else
            {
                lastWrite   = current;
                stableCount = 0;
            }
        }
        return false;  // still changing after 10 minutes — give up
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _watcher?.Dispose();
        _cts?.Dispose();
        base.OnFormClosed(e);
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
