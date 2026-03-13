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

    // ── OwnerDraw colors ─────────────────────────────────────────────────
    private static readonly System.Drawing.Color ClrDark    = System.Drawing.Color.FromArgb(0x0f, 0x0f, 0x16);
    private static readonly System.Drawing.Color ClrDarkAlt = System.Drawing.Color.FromArgb(0x13, 0x13, 0x1e);
    private static readonly System.Drawing.Color ClrSelect  = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
    private static readonly System.Drawing.Color ClrPrimary = System.Drawing.Color.FromArgb(0xdd, 0xe5, 0xff);
    private static readonly System.Drawing.Color ClrSecond  = System.Drawing.Color.FromArgb(0x88, 0x99, 0xbb);
    private static readonly System.Drawing.Color ClrMuted   = System.Drawing.Color.FromArgb(0x3a, 0x42, 0x60);
    private static readonly System.Drawing.Color ClrGreen   = System.Drawing.Color.FromArgb(0x4a, 0xde, 0x80);
    private static readonly System.Drawing.Color ClrGreenDim = System.Drawing.Color.FromArgb(0x1a, 0x50, 0x30);
    private static readonly System.Drawing.Color ClrBlue    = System.Drawing.Color.FromArgb(0x4f, 0x6e, 0xf7);
    private static readonly System.Drawing.Color ClrRed     = System.Drawing.Color.FromArgb(0xf8, 0x71, 0x71);
    private static readonly System.Drawing.Font  FontSmall  = new("Segoe UI", 8f);
    private static readonly System.Drawing.Font  FontNormal = new("Segoe UI", 9f);

    private enum StatusKind { Idle, Watching, Processing, Error }
    private bool _dotBright = true;

    public MainForm()
    {
        InitializeComponent();

        _trayIcon = new NotifyIcon
        {
            Text             = "WarcraftPulse Uploader",
            Icon             = SystemIcons.Application,
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
        UpdateOnboardingBanner();
        RefreshHistory();

        if (_settings.StartWithWindows)
            SettingsForm.ApplyStartWithWindows(true);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open",             null, (_, _) => ShowWindow());
        menu.Items.Add("Upload Log File…", null, (_, _) => btnUpload_Click(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings",         null, (_, _) => btnSettings_Click(this, EventArgs.Empty));
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

        lblLogPath.Text = string.IsNullOrEmpty(_settings.WowLogDirectory)
            ? "No log directory configured"
            : _settings.WowLogDirectory;

        if (string.IsNullOrEmpty(_settings.WowLogDirectory))
        {
            SetStatus("No log directory configured — open Settings.", StatusKind.Idle);
            return;
        }

        _uploader = new UploadService(_settings.ServerUrl);
        _watcher  = new LogWatcher(_settings.WowLogDirectory);
        _watcher.NewLogDetected += OnNewLogDetected;
        _watcher.Start();

        SetStatus("Watching for new logs…", StatusKind.Watching);
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
            SetStatus("Error: No API token configured. Open Settings → Access Tokens.", StatusKind.Error);
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        btnUpload.Enabled = false;

        string fileHash;
        try
        {
            fileHash = await Task.Run(() => UploadHistory.HashFile(filePath), ct);
        }
        catch (IOException ex)
        {
            SetStatus($"Error reading log file: {ex.Message}", StatusKind.Error);
            btnUpload.Enabled = true;
            return;
        }

        if (_history.IsDuplicate(fileHash))
        {
            var proceed = MessageBox.Show(
                "This log file was already uploaded.\n\nUpload again?",
                "Duplicate Log",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (proceed != DialogResult.Yes)
            {
                SetStatus("Skipped — already uploaded.", StatusKind.Idle);
                btnUpload.Enabled = true;
                return;
            }
        }

        try
        {
            if (fromWatcher)
            {
                SetStatus($"Waiting for WoW to finish writing {Path.GetFileName(filePath)}…", StatusKind.Processing);
                bool stable = await WaitForFileStable(filePath, stableMs: 5000, ct);
                if (!stable)
                {
                    SetStatus("Timed out waiting for log file. Upload manually.", StatusKind.Error);
                    btnUpload.Enabled = true;
                    return;
                }
            }

            SetStatus($"Parsing: {Path.GetFileName(filePath)}…", StatusKind.Processing);

            CombatLogData data;
            try
            {
                data = await Task.Run(() => CombatLogParser.Parse(filePath), ct);
            }
            catch (ParseException ex)
            {
                SetStatus($"Parse error: {ex.Message}", StatusKind.Error);
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
                if (confirm != DialogResult.Yes)
                {
                    SetStatus("Upload cancelled.", StatusKind.Idle);
                    btnUpload.Enabled = true;
                    return;
                }
            }

            SetStatus("Uploading…", StatusKind.Processing);

            _uploader ??= new UploadService(_settings.ServerUrl);
            var result = await _uploader.UploadAsync(data, _settings.ApiToken, ct);

            if (result.Success)
            {
                int kills = data.Fights.Count(f => f.Kill);
                SetStatus($"Done · {data.ZoneName} · {data.Fights.Count} encounter(s), {kills} kill(s)", StatusKind.Watching);

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
                SetStatus($"Error: {result.Error}", StatusKind.Error);
            }

            btnUpload.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            SetStatus("Upload cancelled.", StatusKind.Idle);
            btnUpload.Enabled = true;
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", StatusKind.Error);
            btnUpload.Enabled = true;
        }
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
            UpdateOnboardingBanner();
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
        _dotTimer.Stop();
        _trayIcon.Dispose();
        _watcher?.Dispose();
        _uploader?.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnFormClosed(e);
    }

    // SetStatus is safe to call from any thread.
    private void SetStatus(string message, StatusKind kind = StatusKind.Idle)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(message, kind)); return; }

        lblStatus.Text = message;

        switch (kind)
        {
            case StatusKind.Watching:
                lblDot.ForeColor = ClrGreen;
                _dotTimer.Start();
                break;
            case StatusKind.Processing:
                lblDot.ForeColor = ClrBlue;
                _dotTimer.Stop();
                break;
            case StatusKind.Error:
                lblDot.ForeColor = ClrRed;
                _dotTimer.Stop();
                break;
            default:
                lblDot.ForeColor = ClrMuted;
                _dotTimer.Stop();
                break;
        }
    }

    private void DotTimer_Tick(object? sender, EventArgs e)
    {
        _dotBright       = !_dotBright;
        lblDot.ForeColor = _dotBright ? ClrGreen : ClrGreenDim;
    }

    private void UpdateOnboardingBanner()
    {
        bool show = string.IsNullOrEmpty(_settings.ApiToken);
        pnlOnboard.Visible = show;
        lvHistory.Location = new System.Drawing.Point(0, show ? 108 : 52);
        lvHistory.Height   = show ? 354 : 410;
    }

    private void RefreshHistory()
    {
        lvHistory.Items.Clear();
        int count = _history.Entries.Count;
        lblUploads.Text = $"{count} Upload{(count == 1 ? "" : "s")}";

        if (count == 0)
        {
            var placeholder = new ListViewItem("");
            placeholder.SubItems.Add(""); placeholder.SubItems.Add(""); placeholder.SubItems.Add("");
            placeholder.Tag = null;
            lvHistory.Items.Add(placeholder);
            return;
        }

        foreach (var entry in _history.Entries)
        {
            var item = new ListViewItem(entry.FileName);
            item.SubItems.Add(entry.ZoneName);
            item.SubItems.Add("Uploaded");
            item.SubItems.Add(entry.UploadedAt.ToLocalTime().ToString("MM/dd HH:mm"));
            item.Tag = entry.StatusUrl;
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

    // ── OwnerDraw handlers ───────────────────────────────────────────────

    private void lvHistory_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        e.Graphics.FillRectangle(new System.Drawing.SolidBrush(ClrDark), e.Bounds);
        // Bottom border
        e.Graphics.DrawLine(new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x25, 0x25, 0x35)),
            e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        TextRenderer.DrawText(e.Graphics, e.Header!.Text, FontSmall,
            System.Drawing.Rectangle.FromLTRB(e.Bounds.Left + 6, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom),
            ClrSecond, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private void lvHistory_DrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        // All drawing is done per-subitem in DrawSubItem.
    }

    private void lvHistory_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        bool isSelected    = e.Item.Selected;
        bool isPlaceholder = e.Item.Tag is null;

        var bg = isSelected
            ? ClrSelect
            : e.ItemIndex % 2 == 0 ? ClrDark : ClrDarkAlt;
        e.Graphics.FillRectangle(new System.Drawing.SolidBrush(bg), e.Bounds);

        if (isPlaceholder)
        {
            if (e.ColumnIndex == 0)
                TextRenderer.DrawText(e.Graphics, "No uploads yet", FontNormal,
                    new System.Drawing.Rectangle(0, e.Bounds.Y, lvHistory.Width, e.Bounds.Height),
                    ClrMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            return;
        }

        // Badge column
        if (e.ColumnIndex == 2)
        {
            TextRenderer.DrawText(e.Graphics, e.SubItem!.Text, FontSmall, e.Bounds,
                ClrGreen, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            return;
        }

        var fg   = e.ColumnIndex == 3 ? ClrSecond : ClrPrimary;
        var rect = new System.Drawing.Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.SubItem!.Text, FontNormal, rect,
            fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}
