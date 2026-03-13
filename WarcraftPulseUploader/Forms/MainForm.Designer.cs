// Forms/MainForm.Designer.cs
namespace WarcraftPulseUploader.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        var bgWindow = System.Drawing.Color.FromArgb(0x15, 0x15, 0x1e);
        var bgDark   = System.Drawing.Color.FromArgb(0x0f, 0x0f, 0x16);
        var bgCard   = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
        var bgBanner = System.Drawing.Color.FromArgb(0x1a, 0x1a, 0x2e);
        var clrBlue   = System.Drawing.Color.FromArgb(0x4f, 0x6e, 0xf7);
        var clrPrimary = System.Drawing.Color.FromArgb(0xdd, 0xe5, 0xff);
        var clrSecond  = System.Drawing.Color.FromArgb(0x88, 0x99, 0xbb);
        var clrMuted   = System.Drawing.Color.FromArgb(0x3a, 0x42, 0x60);
        var clrBorder  = System.Drawing.Color.FromArgb(0x25, 0x25, 0x35);

        pnlStats    = new System.Windows.Forms.Panel();
        lblUploads  = new System.Windows.Forms.Label();
        pnlDivider  = new System.Windows.Forms.Panel();
        lblDot      = new System.Windows.Forms.Label();
        lblStatus   = new System.Windows.Forms.Label();
        pnlOnboard  = new System.Windows.Forms.Panel();
        lblOnboard  = new System.Windows.Forms.Label();
        btnOnboard  = new System.Windows.Forms.Button();
        lvHistory   = new System.Windows.Forms.ListView();
        pnlFooter   = new System.Windows.Forms.Panel();
        lblLogPath  = new System.Windows.Forms.Label();
        btnUpload   = new System.Windows.Forms.Button();
        btnSettings = new System.Windows.Forms.Button();
        _dotTimer   = new System.Windows.Forms.Timer(components);

        SuspendLayout();

        // Form
        AutoScaleMode   = System.Windows.Forms.AutoScaleMode.Font;
        BackColor       = bgWindow;
        ForeColor       = clrPrimary;
        ClientSize      = new System.Drawing.Size(440, 520);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        // ── Stats strip ──────────────────────────────────────────────────
        pnlStats.BackColor = bgDark;
        pnlStats.Location  = new System.Drawing.Point(0, 0);
        pnlStats.Size      = new System.Drawing.Size(440, 52);

        lblUploads.Text      = "0 Uploads";
        lblUploads.ForeColor = clrBlue;
        lblUploads.Font      = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
        lblUploads.Location  = new System.Drawing.Point(12, 14);
        lblUploads.Size      = new System.Drawing.Size(160, 24);
        lblUploads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        pnlDivider.BackColor = clrBorder;
        pnlDivider.Location  = new System.Drawing.Point(185, 12);
        pnlDivider.Size      = new System.Drawing.Size(1, 28);

        lblDot.Text      = "●";
        lblDot.ForeColor = clrMuted;
        lblDot.Font      = new System.Drawing.Font("Segoe UI", 9f);
        lblDot.Location  = new System.Drawing.Point(196, 15);
        lblDot.Size      = new System.Drawing.Size(18, 22);
        lblDot.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        lblStatus.Text      = "Starting…";
        lblStatus.ForeColor = clrSecond;
        lblStatus.Font      = new System.Drawing.Font("Segoe UI", 9f);
        lblStatus.Location  = new System.Drawing.Point(218, 15);
        lblStatus.Size      = new System.Drawing.Size(210, 22);
        lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        pnlStats.Controls.AddRange(new System.Windows.Forms.Control[] {
            lblUploads, pnlDivider, lblDot, lblStatus,
        });

        // ── Onboarding banner ────────────────────────────────────────────
        pnlOnboard.BackColor = bgBanner;
        pnlOnboard.Location  = new System.Drawing.Point(0, 52);
        pnlOnboard.Size      = new System.Drawing.Size(440, 56);
        pnlOnboard.Visible   = false;
        pnlOnboard.Paint    += (_, e) =>
            e.Graphics.FillRectangle(new System.Drawing.SolidBrush(clrBlue), 0, 0, 3, pnlOnboard.Height);

        lblOnboard.Text      = "Connect your account to get started\r\n" +
                               "You need a personal API token from WarcraftPulse to upload logs.";
        lblOnboard.ForeColor = clrSecond;
        lblOnboard.Font      = new System.Drawing.Font("Segoe UI", 8f);
        lblOnboard.Location  = new System.Drawing.Point(14, 6);
        lblOnboard.Size      = new System.Drawing.Size(290, 44);

        btnOnboard.Text      = "Open Settings";
        btnOnboard.ForeColor = clrPrimary;
        btnOnboard.BackColor = clrBlue;
        btnOnboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnOnboard.FlatAppearance.BorderSize = 0;
        btnOnboard.Location  = new System.Drawing.Point(310, 13);
        btnOnboard.Size      = new System.Drawing.Size(116, 28);
        btnOnboard.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnOnboard.Click    += btnSettings_Click;

        pnlOnboard.Controls.AddRange(new System.Windows.Forms.Control[] { lblOnboard, btnOnboard });

        // ── History ListView ─────────────────────────────────────────────
        lvHistory.BackColor     = bgDark;
        lvHistory.ForeColor     = clrPrimary;
        lvHistory.Location      = new System.Drawing.Point(0, 52);
        lvHistory.Size          = new System.Drawing.Size(440, 410);
        lvHistory.View          = System.Windows.Forms.View.Details;
        lvHistory.FullRowSelect = true;
        lvHistory.GridLines     = false;
        lvHistory.MultiSelect   = false;
        lvHistory.BorderStyle   = System.Windows.Forms.BorderStyle.None;
        lvHistory.HeaderStyle   = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
        lvHistory.OwnerDraw     = true;
        lvHistory.Columns.Add("Log File", 200);
        lvHistory.Columns.Add("Zone",     120);
        lvHistory.Columns.Add("Badge",     64);
        lvHistory.Columns.Add("Date",      56);
        lvHistory.DrawColumnHeader += lvHistory_DrawColumnHeader;
        lvHistory.DrawItem         += lvHistory_DrawItem;
        lvHistory.DrawSubItem      += lvHistory_DrawSubItem;
        lvHistory.DoubleClick      += lvHistory_DoubleClick;

        // ── Footer ───────────────────────────────────────────────────────
        pnlFooter.BackColor = bgDark;
        pnlFooter.Location  = new System.Drawing.Point(0, 462);
        pnlFooter.Size      = new System.Drawing.Size(440, 58);

        lblLogPath.Text         = "No log directory configured";
        lblLogPath.ForeColor    = clrMuted;
        lblLogPath.Font         = new System.Drawing.Font("Consolas", 7.5f);
        lblLogPath.Location     = new System.Drawing.Point(12, 6);
        lblLogPath.Size         = new System.Drawing.Size(416, 14);
        lblLogPath.AutoEllipsis = true;

        btnUpload.Text      = "Upload…";
        btnUpload.ForeColor = clrPrimary;
        btnUpload.BackColor = clrBlue;
        btnUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnUpload.FlatAppearance.BorderSize = 0;
        btnUpload.Location  = new System.Drawing.Point(12, 24);
        btnUpload.Size      = new System.Drawing.Size(90, 26);
        btnUpload.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnUpload.Click    += btnUpload_Click;

        btnSettings.Text      = "Settings";
        btnSettings.ForeColor = clrSecond;
        btnSettings.BackColor = bgCard;
        btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnSettings.FlatAppearance.BorderColor = clrBorder;
        btnSettings.FlatAppearance.BorderSize  = 1;
        btnSettings.Location  = new System.Drawing.Point(338, 24);
        btnSettings.Size      = new System.Drawing.Size(90, 26);
        btnSettings.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnSettings.Click    += btnSettings_Click;

        pnlFooter.Controls.AddRange(new System.Windows.Forms.Control[] {
            lblLogPath, btnUpload, btnSettings,
        });

        // ── Dot animation timer ──────────────────────────────────────────
        _dotTimer.Interval = 600;
        _dotTimer.Tick    += DotTimer_Tick;

        Controls.AddRange(new System.Windows.Forms.Control[] {
            pnlStats, pnlOnboard, lvHistory, pnlFooter,
        });

        ResumeLayout(false);
    }

    // Control fields
    private System.Windows.Forms.Panel    pnlStats    = null!;
    private System.Windows.Forms.Label    lblUploads  = null!;
    private System.Windows.Forms.Panel    pnlDivider  = null!;
    private System.Windows.Forms.Label    lblDot      = null!;
    private System.Windows.Forms.Label    lblStatus   = null!;
    private System.Windows.Forms.Panel    pnlOnboard  = null!;
    private System.Windows.Forms.Label    lblOnboard  = null!;
    private System.Windows.Forms.Button   btnOnboard  = null!;
    private System.Windows.Forms.ListView lvHistory   = null!;
    private System.Windows.Forms.Panel    pnlFooter   = null!;
    private System.Windows.Forms.Label    lblLogPath  = null!;
    private System.Windows.Forms.Button   btnUpload   = null!;
    private System.Windows.Forms.Button   btnSettings = null!;
    private System.Windows.Forms.Timer    _dotTimer   = null!;
}
