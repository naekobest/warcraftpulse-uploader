// Forms/MainForm.cs
using WarcraftPulseUploader.Parser;
using WarcraftPulseUploader.Services;

namespace WarcraftPulseUploader.Forms;

public partial class MainForm : Form
{
    private AppSettings    _settings = AppSettings.Load();
    private UploadHistory  _history  = UploadHistory.Load();
    private LogWatcher?    _watcher;
    private UploadService? _uploader;
    private CancellationTokenSource? _cts;
    private readonly NotifyIcon _trayIcon;

    public MainForm()
    {
        InitializeComponent();

        _trayIcon = new NotifyIcon
        {
            Text             = "WarcraftPulse Uploader",
            Icon             = SystemIcons.Application,   // replace with app icon once one exists
            Visible          = true,
            ContextMenuStrip = BuildTrayMenu(),
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

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
        RefreshHistory();

        // Sync registry state with current setting (handles setting changed outside the app)
        if (_settings.StartWithWindows)
            SettingsForm.ApplyStartWithWindows(true);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open",             null, (_, _) => ShowWindow());
        menu.Items.Add("Upload Log File…", null, (_, _) => btnUpload_Click(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit",             null, (_, _) => Application.Exit());
        return menu;
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(2000, "WarcraftPulse Uploader",
                "Still running in the background.", ToolTipIcon.Info);
            return;
        }
        _trayIcon.Visible = false;
        base.OnFormClosing(e);
    }

    private void StartWatcher()
    {
        _watcher?.Dispose();
        _uploader?.Dispose();
        if (string.IsNullOrEmpty(_settings.WowLogDirectory)) return;

        _uploader = new UploadService(_settings.ServerUrl);
        _watcher  = new LogWatcher(_settings.WowLogDirectory);
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

        string fileHash = await Task.Run(() => UploadHistory.HashFile(filePath), ct);

        if (_history.IsDuplicate(fileHash))
        {
            var proceed = MessageBox.Show(
                "This log file was already uploaded.\n\nUpload again?",
                "Duplicate Log",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (proceed != DialogResult.Yes) { SetStatus("Skipped — already uploaded."); btnUpload.Enabled = true; return; }
        }

        try
        {
            if (fromWatcher)
            {
                SetStatus($"Waiting for WoW to finish writing {Path.GetFileName(filePath)}…");
                bool stable = await WaitForFileStable(filePath, stableMs: 5000, ct);
                if (!stable) { SetStatus("Timed out waiting for log file. Upload manually."); btnUpload.Enabled = true; return; }
            }

            SetStatus($"Parsing: {Path.GetFileName(filePath)}…");

            CombatLogData data;
            try
            {
                // Run CPU-bound parse off the UI thread
                data = await Task.Run(() => CombatLogParser.Parse(filePath), ct);
            }
            catch (ParseException ex)
            {
                SetStatus($"Parse error: {ex.Message}");
                btnUpload.Enabled = true;
                return;
            }

            if (!_settings.AutoUpload)
            {
                var confirm = MessageBox.Show(
                    $"Upload \"{Path.GetFileName(filePath)}\"?\n\n" +
                    $"Zone: {data.ZoneName}  ·  {data.Fights.Count} encounter(s)",
                    "WarcraftPulse Uploader",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (confirm != DialogResult.Yes) { SetStatus("Upload cancelled."); btnUpload.Enabled = true; return; }
            }

            SetStatus("Uploading…");

            _uploader ??= new UploadService(_settings.ServerUrl);
            var result = await _uploader.UploadAsync(data, _settings.ApiToken, ct);

            if (result.Success)
            {
                int kills = data.Fights.Count(f => f.Kill);
                SetStatus($"Done · {data.ZoneName} · {data.Fights.Count} encounter(s), {kills} kill(s)");
                btnOpenReport.Tag     = result.StatusUrl;
                btnOpenReport.Visible = true;

                _history.Add(new UploadEntry
                {
                    FileName   = Path.GetFileName(filePath),
                    FileHash   = fileHash,
                    ZoneName   = data.ZoneName,
                    FightCount = data.Fights.Count,
                    KillCount  = kills,
                    ReportCode = result.ReportCode!,
                    StatusUrl  = result.StatusUrl!,
                    UploadedAt = DateTime.UtcNow,
                });
                RefreshHistory();
            }
            else
            {
                SetStatus($"Error: {result.Error}");
            }

            btnUpload.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            btnUpload.Enabled = true;
        }
    }

    private void btnOpenReport_Click(object sender, EventArgs e)
    {
        if (btnOpenReport.Tag is string url && !string.IsNullOrEmpty(url))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
    }

    private void btnUpload_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Select WoW Combat Log",
            Filter = "WoW Combat Log (*.txt)|*.txt|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            _ = ProcessLogAsync(dlg.FileName);
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
        _trayIcon.Dispose();
        _watcher?.Dispose();
        _uploader?.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnFormClosed(e);
    }

    // SetStatus is safe to call from any thread.
    private void SetStatus(string message)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(message)); return; }
        lblStatus.Text = message;
    }

    private void RefreshHistory()
    {
        lvHistory.Items.Clear();
        foreach (var e in _history.Entries)
        {
            var item = new ListViewItem(e.UploadedAt.ToLocalTime().ToString("MM/dd HH:mm"));
            item.SubItems.Add(e.ZoneName);
            item.SubItems.Add(e.FightCount.ToString());
            item.SubItems.Add(e.KillCount.ToString());
            item.SubItems.Add(e.ReportCode);
            item.Tag = e.StatusUrl;
            lvHistory.Items.Add(item);
        }
    }

    private void lvHistory_DoubleClick(object sender, EventArgs e)
    {
        if (lvHistory.SelectedItems.Count == 0) return;
        if (lvHistory.SelectedItems[0].Tag is string url && !string.IsNullOrEmpty(url))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
    }

    private void UpdateTokenWarning()
    {
        lblTokenWarning.Visible = string.IsNullOrEmpty(_settings.ApiToken);
    }
}
