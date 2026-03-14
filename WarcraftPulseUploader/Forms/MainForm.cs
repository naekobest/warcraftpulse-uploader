// Forms/MainForm.cs
using WarcraftPulseUploader.Native;
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

        var appIcon = LoadAppIcon();
        if (appIcon is not null) Icon = appIcon;

        _trayIcon = new NotifyIcon
        {
            Text             = "WarcraftPulse Uploader",
            Icon             = appIcon ?? SystemIcons.Application,
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkMode.ApplyToWindow(Handle);
        DarkMode.ApplyToControl(lvHistory.Handle);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Blue accent stripe at top + bottom separator on stats strip
        pnlStats.Paint += (_, pe) =>
        {
            using var accentBrush = new System.Drawing.SolidBrush(ClrBlue);
            pe.Graphics.FillRectangle(accentBrush, 0, 0, pnlStats.Width, 2);
            using var sepBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x25, 0x25, 0x35));
            pe.Graphics.FillRectangle(sepBrush, 0, pnlStats.Height - 1, pnlStats.Width, 1);
        };
        // Top separator on footer
        pnlFooter.Paint += (_, pe) =>
        {
            using var footerBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x25, 0x25, 0x35));
            pe.Graphics.FillRectangle(footerBrush, 0, 0, pnlFooter.Width, 1);
        };

        // Button hover states
        WireHover(btnUpload,   ClrBlue, System.Drawing.Color.FromArgb(0x60, 0x78, 0xf8));
        WireHover(btnOnboard,  ClrBlue, System.Drawing.Color.FromArgb(0x60, 0x78, 0xf8));
        var cardHover = System.Drawing.Color.FromArgb(0x25, 0x25, 0x40);
        var cardNormal = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
        WireHover(btnSettings, cardNormal, cardHover);
        WireHover(btnHistory,  cardNormal, cardHover);

        StartWatcher();
        UpdateOnboardingBanner();
        RefreshHistory();

        if (_settings.StartWithWindows)
            SettingsForm.ApplyStartWithWindows(true);
    }

    private static System.Drawing.Icon? LoadAppIcon()
    {
        try
        {
            var asm    = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = asm.GetManifestResourceStream("WarcraftPulseUploader.Resources.logo.ico");
            return stream is null ? null : new System.Drawing.Icon(stream);
        }
        catch { return null; }
    }

    private static void WireHover(Button btn, System.Drawing.Color normal, System.Drawing.Color hover)
    {
        btn.MouseEnter += (_, _) => btn.BackColor = hover;
        btn.MouseLeave += (_, _) => btn.BackColor = normal;
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
        if (e.CloseReason == CloseReason.UserClosing)
        {
            var result = MessageBox.Show(
                "Minimize to tray and keep running in the background?",
                "WarcraftPulse Uploader",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (result == DialogResult.Yes)
            {
                e.Cancel = true;
                Hide();
                return;
            }
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

        _uploader = new UploadService();
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

        // Size check — no disk read, just metadata
        const long MaxBytes = 500L * 1024 * 1024;
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            SetStatus("Error: Log file not found.", StatusKind.Error);
            btnUpload.Enabled = true;
            return;
        }
        if (fileInfo.Length > MaxBytes)
        {
            SetStatus($"Error: Log file too large ({fileInfo.Length / 1024 / 1024} MB, max 500 MB).", StatusKind.Error);
            btnUpload.Enabled = true;
            return;
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
            string fileHash;
            try
            {
                (data, fileHash) = await Task.Run(() => CombatLogParser.ParseWithHash(filePath), ct);
            }
            catch (ParseException ex)
            {
                SetStatus($"Parse error: {ex.Message}", StatusKind.Error);
                btnUpload.Enabled = true;
                return;
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

            _uploader ??= new UploadService();
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
                    PayloadKb  = result.PayloadKb,
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
        lvHistory.Location = new System.Drawing.Point(0, show ? 148 : 92);
        lvHistory.Height   = show ? 338 : 394;
    }

    private void RefreshHistory()
    {
        lvHistory.Items.Clear();
        int count = _history.Entries.Count;
        lblUploads.Text = count.ToString();

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
            string sizeLabel = entry.PayloadKb > 0 ? $"{entry.PayloadKb} KB" : "";
            string dateLabel = entry.UploadedAt.ToLocalTime().ToString("MMM dd · HH:mm");
            var item = new ListViewItem(entry.FileName);
            item.SubItems.Add(sizeLabel);
            item.SubItems.Add("Uploaded");
            item.SubItems.Add(dateLabel);
            item.Tag = entry.StatusUrl;
            lvHistory.Items.Add(item);
        }
    }

    private void btnHistory_Click(object sender, EventArgs e)
    {
        if (_history.Entries.Count == 0)
        {
            MessageBox.Show("No uploads yet.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var entry in _history.Entries)
            sb.AppendLine($"{entry.UploadedAt.ToLocalTime():MMM dd · HH:mm}  {entry.FileName}  ({entry.PayloadKb} KB)  {entry.ReportCode}");
        MessageBox.Show(sb.ToString(), "Upload History", MessageBoxButtons.OK, MessageBoxIcon.None);
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
        using var headerBrush = new System.Drawing.SolidBrush(ClrDark);
        e.Graphics.FillRectangle(headerBrush, e.Bounds);
        // Bottom border
        using var separatorPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x25, 0x25, 0x35));
        e.Graphics.DrawLine(separatorPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
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
        bool isSelected    = e.Item?.Selected ?? false;
        bool isPlaceholder = e.Item?.Tag is null;
        string badge = e.Item?.SubItems.Count > 2 ? e.Item.SubItems[2].Text : "";
        bool isFailed = badge == "Failed";

        System.Drawing.Color bg;
        if (isSelected)
            bg = ClrSelect;
        else if (isFailed)
            bg = System.Drawing.Color.FromArgb(30, 248, 113, 113);
        else
            bg = e.ItemIndex % 2 == 0 ? ClrDark : ClrDarkAlt;

        using var rowBrush = new System.Drawing.SolidBrush(bg);
        e.Graphics.FillRectangle(rowBrush, e.Bounds);

        if (isPlaceholder)
        {
            if (e.ColumnIndex == 0)
                TextRenderer.DrawText(e.Graphics, "No uploads yet", FontNormal,
                    new System.Drawing.Rectangle(0, e.Bounds.Y, lvHistory.Width, e.Bounds.Height),
                    ClrSecond, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            return;
        }

        // Badge column — pill shape
        if (e.ColumnIndex == 2)
        {
            string text = e.SubItem!.Text;
            if (string.IsNullOrEmpty(text)) return;

            System.Drawing.Color pillBg, pillFg, pillBorder;
            switch (text)
            {
                case "Uploaded":
                    pillBg     = System.Drawing.Color.FromArgb(25, 74, 222, 128);
                    pillBorder = System.Drawing.Color.FromArgb(120, 74, 222, 128);
                    pillFg     = ClrGreen;
                    break;
                case "Failed":
                    pillBg     = System.Drawing.Color.FromArgb(40, 248, 113, 113);
                    pillBorder = System.Drawing.Color.FromArgb(160, 248, 113, 113);
                    pillFg     = ClrRed;
                    break;
                case "Duplicate":
                    pillBg     = System.Drawing.Color.FromArgb(25, 245, 158, 11);
                    pillBorder = System.Drawing.Color.FromArgb(140, 245, 158, 11);
                    pillFg     = System.Drawing.Color.FromArgb(245, 158, 11);
                    break;
                default: // Ignored
                    pillBg     = System.Drawing.Color.FromArgb(20, 88, 153, 187);
                    pillBorder = ClrSecond;
                    pillFg     = ClrSecond;
                    break;
            }

            var sz  = TextRenderer.MeasureText(text, FontSmall);
            int pw  = sz.Width + 14;
            int ph  = 18;
            int px  = e.Bounds.X + (e.Bounds.Width - pw) / 2;
            int py  = e.Bounds.Y + (e.Bounds.Height - ph) / 2;

            var prev = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = RoundedRectPath(new System.Drawing.RectangleF(px, py, pw, ph), 9f);
            using var pillBrush = new System.Drawing.SolidBrush(pillBg);
            using var pillPen   = new System.Drawing.Pen(pillBorder, 1f);
            e.Graphics.FillPath(pillBrush, path);
            e.Graphics.DrawPath(pillPen, path);
            e.Graphics.SmoothingMode = prev;

            TextRenderer.DrawText(e.Graphics, text, FontSmall,
                new System.Drawing.Rectangle(px, py, pw, ph),
                pillFg, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            return;
        }

        var fg   = e.ColumnIndex >= 3 ? ClrSecond : (isFailed && e.ColumnIndex == 0 ? ClrRed : ClrPrimary);
        var rect = new System.Drawing.Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.SubItem!.Text, FontNormal, rect,
            fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRectPath(System.Drawing.RectangleF r, float radius)
    {
        float d    = radius * 2;
        var   path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(r.X,          r.Y,           d, d, 180, 90);
        path.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
        path.AddArc(r.Right - d,  r.Bottom - d,  d, d,   0, 90);
        path.AddArc(r.X,          r.Bottom - d,  d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}
