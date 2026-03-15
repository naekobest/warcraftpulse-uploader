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

        var bgWindow  = System.Drawing.Color.FromArgb(0x15, 0x15, 0x1e);
        var bgDark    = System.Drawing.Color.FromArgb(0x0f, 0x0f, 0x16);
        var bgCard    = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
        var bgBanner  = System.Drawing.Color.FromArgb(0x1a, 0x1a, 0x2e);
        var clrBlue    = System.Drawing.Color.FromArgb(0x4f, 0x6e, 0xf7);
        var clrPrimary = System.Drawing.Color.FromArgb(0xdd, 0xe5, 0xff);
        var clrSecond  = System.Drawing.Color.FromArgb(0x88, 0x99, 0xbb);
        var clrMuted   = System.Drawing.Color.FromArgb(0x3a, 0x42, 0x60);
        var clrBorder  = System.Drawing.Color.FromArgb(0x25, 0x25, 0x35);

        pnlStats           = new System.Windows.Forms.Panel();
        lblUploadsHeader   = new System.Windows.Forms.Label();
        lblUploads         = new System.Windows.Forms.Label();
        lblLastUpload      = new System.Windows.Forms.Label();
        pnlDivider         = new System.Windows.Forms.Panel();
        lblStatusHeader    = new System.Windows.Forms.Label();
        lblEncounterVal    = new System.Windows.Forms.Label();
        lblEncounterLbl    = new System.Windows.Forms.Label();
        lblKillVal         = new System.Windows.Forms.Label();
        lblKillLbl         = new System.Windows.Forms.Label();
        lblDot             = new System.Windows.Forms.Label();
        lblStatus          = new System.Windows.Forms.Label();
        pnlProgress        = new System.Windows.Forms.Panel();
        lblSectionHeader   = new System.Windows.Forms.Label();
        pnlDuplicateBanner = new System.Windows.Forms.Panel();
        lblDuplicateText   = new System.Windows.Forms.Label();
        btnDuplicateSkip   = new System.Windows.Forms.Button();
        btnDuplicateUpload = new System.Windows.Forms.Button();
        pnlOnboard         = new System.Windows.Forms.Panel();
        lblOnboard         = new System.Windows.Forms.Label();
        btnOnboard         = new System.Windows.Forms.Button();
        lvHistory          = new System.Windows.Forms.ListView();
        lvContextMenu      = new System.Windows.Forms.ContextMenuStrip(components);
        pnlFooter          = new System.Windows.Forms.Panel();
        lblLogPath         = new System.Windows.Forms.Label();
        btnUpload          = new System.Windows.Forms.Button();
        btnSettings        = new System.Windows.Forms.Button();
        _dotTimer          = new System.Windows.Forms.Timer(components);

        SuspendLayout();

        // Form
        AutoScaleMode   = System.Windows.Forms.AutoScaleMode.Font;
        BackColor       = bgWindow;
        ForeColor       = clrPrimary;
        ClientSize      = new System.Drawing.Size(440, 544);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        // ── Stats strip (80px, two-box layout) ───────────────────────────
        pnlStats.BackColor = bgDark;
        pnlStats.Location  = new System.Drawing.Point(0, 0);
        pnlStats.Size      = new System.Drawing.Size(440, 80);

        lblUploadsHeader.Text      = "TOTAL UPLOADED";
        lblUploadsHeader.ForeColor = clrSecond;
        lblUploadsHeader.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
        lblUploadsHeader.Location  = new System.Drawing.Point(12, 10);
        lblUploadsHeader.Size      = new System.Drawing.Size(160, 13);

        lblUploads.Text      = "0";
        lblUploads.ForeColor = clrBlue;
        lblUploads.Font      = new System.Drawing.Font("Segoe UI", 18f, System.Drawing.FontStyle.Bold);
        lblUploads.Location  = new System.Drawing.Point(10, 22);
        lblUploads.Size      = new System.Drawing.Size(185, 26);
        lblUploads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        lblLastUpload.Text      = "";
        lblLastUpload.ForeColor = clrSecond;
        lblLastUpload.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
        lblLastUpload.Location  = new System.Drawing.Point(12, 50);
        lblLastUpload.Size      = new System.Drawing.Size(185, 14);

        pnlDivider.BackColor = clrBorder;
        pnlDivider.Location  = new System.Drawing.Point(200, 8);
        pnlDivider.Size      = new System.Drawing.Size(1, 64);

        // Right side: "LAST UPLOAD" header + encounter/kill mini-stats + status row
        lblStatusHeader.Text      = "LAST UPLOAD";
        lblStatusHeader.ForeColor = clrSecond;
        lblStatusHeader.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
        lblStatusHeader.Location  = new System.Drawing.Point(212, 8);
        lblStatusHeader.Size      = new System.Drawing.Size(200, 13);

        lblEncounterVal.Text      = "—";
        lblEncounterVal.ForeColor = clrPrimary;
        lblEncounterVal.Font      = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
        lblEncounterVal.Location  = new System.Drawing.Point(212, 22);
        lblEncounterVal.Size      = new System.Drawing.Size(58, 22);
        lblEncounterVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        lblKillVal.Text      = "—";
        lblKillVal.ForeColor = System.Drawing.Color.FromArgb(0x4a, 0xde, 0x80);
        lblKillVal.Font      = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
        lblKillVal.Location  = new System.Drawing.Point(282, 22);
        lblKillVal.Size      = new System.Drawing.Size(58, 22);
        lblKillVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        lblEncounterLbl.Text      = "FIGHTS";
        lblEncounterLbl.ForeColor = clrSecond;
        lblEncounterLbl.Font      = new System.Drawing.Font("Segoe UI", 7f);
        lblEncounterLbl.Location  = new System.Drawing.Point(212, 45);
        lblEncounterLbl.Size      = new System.Drawing.Size(58, 12);

        lblKillLbl.Text      = "KILLS";
        lblKillLbl.ForeColor = clrSecond;
        lblKillLbl.Font      = new System.Drawing.Font("Segoe UI", 7f);
        lblKillLbl.Location  = new System.Drawing.Point(282, 45);
        lblKillLbl.Size      = new System.Drawing.Size(58, 12);

        lblDot.Text      = "●";
        lblDot.ForeColor = clrMuted;
        lblDot.Font      = new System.Drawing.Font("Segoe UI", 8f);
        lblDot.Location  = new System.Drawing.Point(212, 62);
        lblDot.Size      = new System.Drawing.Size(16, 14);
        lblDot.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        lblStatus.Text      = "Starting…";
        lblStatus.ForeColor = clrSecond;
        lblStatus.Font      = new System.Drawing.Font("Segoe UI", 9f);
        lblStatus.Location  = new System.Drawing.Point(230, 60);
        lblStatus.Size      = new System.Drawing.Size(198, 18);
        lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        pnlStats.Controls.AddRange(new System.Windows.Forms.Control[] {
            lblUploadsHeader, lblUploads, lblLastUpload, pnlDivider,
            lblStatusHeader, lblEncounterVal, lblKillVal, lblEncounterLbl, lblKillLbl,
            lblDot, lblStatus,
        });

        // ── Progress bar (2px, shown during upload) ──────────────────────
        pnlProgress.BackColor = clrBlue;
        pnlProgress.Location  = new System.Drawing.Point(0, 80);
        pnlProgress.Size      = new System.Drawing.Size(440, 2);
        pnlProgress.Visible   = false;

        // ── Section label ────────────────────────────────────────────────
        lblSectionHeader.Text      = "UPLOAD HISTORY";
        lblSectionHeader.ForeColor = clrSecond;
        lblSectionHeader.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
        lblSectionHeader.BackColor = bgDark;
        lblSectionHeader.Location  = new System.Drawing.Point(0, 82);
        lblSectionHeader.Size      = new System.Drawing.Size(440, 24);
        lblSectionHeader.Padding   = new System.Windows.Forms.Padding(12, 0, 0, 0);
        lblSectionHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // ── Duplicate upload inline banner ───────────────────────────────
        var clrAmber = System.Drawing.Color.FromArgb(0xf5, 0x9e, 0x0b);
        pnlDuplicateBanner.BackColor = bgBanner;
        pnlDuplicateBanner.Location  = new System.Drawing.Point(0, 106);
        pnlDuplicateBanner.Size      = new System.Drawing.Size(440, 40);
        pnlDuplicateBanner.Visible   = false;
        pnlDuplicateBanner.Paint    += (_, e) =>
        {
            using var b = new System.Drawing.SolidBrush(clrAmber);
            e.Graphics.FillRectangle(b, 0, 0, 3, pnlDuplicateBanner.Height);
        };

        lblDuplicateText.Text      = "";
        lblDuplicateText.ForeColor = clrPrimary;
        lblDuplicateText.Font      = new System.Drawing.Font("Segoe UI", 8.5f);
        lblDuplicateText.Location  = new System.Drawing.Point(14, 4);
        lblDuplicateText.Size      = new System.Drawing.Size(230, 32);

        btnDuplicateSkip.Text      = "Skip";
        btnDuplicateSkip.ForeColor = clrSecond;
        btnDuplicateSkip.BackColor = bgCard;
        btnDuplicateSkip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnDuplicateSkip.FlatAppearance.BorderColor = clrBorder;
        btnDuplicateSkip.FlatAppearance.BorderSize  = 1;
        btnDuplicateSkip.Location  = new System.Drawing.Point(252, 8);
        btnDuplicateSkip.Size      = new System.Drawing.Size(72, 24);
        btnDuplicateSkip.Cursor    = System.Windows.Forms.Cursors.Hand;

        btnDuplicateUpload.Text      = "Upload again";
        btnDuplicateUpload.ForeColor = clrPrimary;
        btnDuplicateUpload.BackColor = clrBlue;
        btnDuplicateUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnDuplicateUpload.FlatAppearance.BorderSize = 0;
        btnDuplicateUpload.Location  = new System.Drawing.Point(330, 8);
        btnDuplicateUpload.Size      = new System.Drawing.Size(96, 24);
        btnDuplicateUpload.Cursor    = System.Windows.Forms.Cursors.Hand;

        pnlDuplicateBanner.Controls.AddRange(new System.Windows.Forms.Control[] {
            lblDuplicateText, btnDuplicateSkip, btnDuplicateUpload,
        });

        // ── Onboarding banner ────────────────────────────────────────────
        pnlOnboard.BackColor = bgBanner;
        pnlOnboard.Location  = new System.Drawing.Point(0, 106);
        pnlOnboard.Size      = new System.Drawing.Size(440, 56);
        pnlOnboard.Visible   = false;
        pnlOnboard.Paint    += (_, e) =>
        {
            using var b = new System.Drawing.SolidBrush(clrBlue);
            e.Graphics.FillRectangle(b, 0, 0, 3, pnlOnboard.Height);
        };

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
        var lvImageList = new System.Windows.Forms.ImageList { ImageSize = new System.Drawing.Size(1, 32) };
        lvImageList.Images.Add(new System.Drawing.Bitmap(1, 32));

        lvHistory.BackColor      = bgDark;
        lvHistory.ForeColor      = clrPrimary;
        lvHistory.Location       = new System.Drawing.Point(0, 106);
        lvHistory.Size           = new System.Drawing.Size(440, 380);
        lvHistory.View           = System.Windows.Forms.View.Details;
        lvHistory.FullRowSelect  = true;
        lvHistory.GridLines      = false;
        lvHistory.MultiSelect    = false;
        lvHistory.BorderStyle    = System.Windows.Forms.BorderStyle.None;
        lvHistory.HeaderStyle    = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
        lvHistory.OwnerDraw      = true;
        lvHistory.SmallImageList = lvImageList;
        lvHistory.Columns.Add("Log File", 218);
        lvHistory.Columns.Add("Size",      82);
        lvHistory.Columns.Add("Badge",     72);
        lvHistory.Columns.Add("Date",      68);
        lvHistory.DrawColumnHeader += lvHistory_DrawColumnHeader;
        lvHistory.DrawItem         += lvHistory_DrawItem;
        lvHistory.DrawSubItem      += lvHistory_DrawSubItem;
        lvHistory.DoubleClick      += lvHistory_DoubleClick;
        lvHistory.MouseDown        += lvHistory_MouseDown;
        lvHistory.ContextMenuStrip  = lvContextMenu;

        // ── History context menu ──────────────────────────────────────────
        lvContextMenu.BackColor = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
        lvContextMenu.ForeColor = clrPrimary;
        lvContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
        var menuOpen    = new System.Windows.Forms.ToolStripMenuItem("Open in Browser");
        var menuSep     = new System.Windows.Forms.ToolStripSeparator();
        var menuCopyUrl = new System.Windows.Forms.ToolStripMenuItem("Copy Report URL");
        var menuCopyCode = new System.Windows.Forms.ToolStripMenuItem("Copy Report Code");
        menuOpen.Click     += lvMenu_OpenBrowser;
        menuCopyUrl.Click  += lvMenu_CopyUrl;
        menuCopyCode.Click += lvMenu_CopyCode;
        lvContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            menuOpen, menuSep, menuCopyUrl, menuCopyCode,
        });
        lvContextMenu.Opening += lvContextMenu_Opening;

        // ── Footer ───────────────────────────────────────────────────────
        pnlFooter.BackColor = bgDark;
        pnlFooter.Location  = new System.Drawing.Point(0, 486);
        pnlFooter.Size      = new System.Drawing.Size(440, 58);

        lblLogPath.Text         = "No log directory configured";
        lblLogPath.ForeColor    = clrSecond;
        lblLogPath.Font         = new System.Drawing.Font("Consolas", 8.5f);
        lblLogPath.Location     = new System.Drawing.Point(12, 5);
        lblLogPath.Size         = new System.Drawing.Size(416, 16);
        lblLogPath.AutoEllipsis = true;

        btnUpload.Text      = "Upload…";
        btnUpload.ForeColor = clrPrimary;
        btnUpload.BackColor = clrBlue;
        btnUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnUpload.FlatAppearance.BorderSize = 0;
        btnUpload.Location  = new System.Drawing.Point(12, 24);
        btnUpload.Size      = new System.Drawing.Size(88, 26);
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
            pnlStats, pnlProgress, lblSectionHeader, pnlDuplicateBanner, pnlOnboard, lvHistory, pnlFooter,
        });

        ResumeLayout(false);
    }

    // Control fields
    private System.Windows.Forms.Panel    pnlStats         = null!;
    private System.Windows.Forms.Label    lblUploadsHeader = null!;
    private System.Windows.Forms.Label    lblUploads       = null!;
    private System.Windows.Forms.Panel    pnlDivider       = null!;
    private System.Windows.Forms.Label    lblStatusHeader  = null!;
    private System.Windows.Forms.Label    lblEncounterVal  = null!;
    private System.Windows.Forms.Label    lblEncounterLbl  = null!;
    private System.Windows.Forms.Label    lblKillVal       = null!;
    private System.Windows.Forms.Label    lblKillLbl       = null!;
    private System.Windows.Forms.Label    lblDot           = null!;
    private System.Windows.Forms.Label    lblStatus        = null!;
    private System.Windows.Forms.Panel    pnlProgress          = null!;
    private System.Windows.Forms.Label    lblLastUpload        = null!;
    private System.Windows.Forms.Label    lblSectionHeader     = null!;
    private System.Windows.Forms.Panel    pnlDuplicateBanner   = null!;
    private System.Windows.Forms.Label    lblDuplicateText     = null!;
    private System.Windows.Forms.Button   btnDuplicateSkip     = null!;
    private System.Windows.Forms.Button   btnDuplicateUpload   = null!;
    private System.Windows.Forms.Panel    pnlOnboard           = null!;
    private System.Windows.Forms.Label    lblOnboard       = null!;
    private System.Windows.Forms.Button   btnOnboard       = null!;
    private System.Windows.Forms.ListView        lvHistory       = null!;
    private System.Windows.Forms.ContextMenuStrip lvContextMenu  = null!;
    private System.Windows.Forms.Panel            pnlFooter      = null!;
    private System.Windows.Forms.Label    lblLogPath       = null!;
    private System.Windows.Forms.Button   btnUpload        = null!;
    private System.Windows.Forms.Button   btnSettings      = null!;
    private System.Windows.Forms.Timer    _dotTimer        = null!;
}
