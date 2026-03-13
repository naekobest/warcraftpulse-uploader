// Forms/SettingsForm.Designer.cs
namespace WarcraftPulseUploader.Forms;

partial class SettingsForm
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
        var bgInput   = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
        var bgCard    = System.Drawing.Color.FromArgb(0x1e, 0x1e, 0x2e);
        var clrBlue    = System.Drawing.Color.FromArgb(0x4f, 0x6e, 0xf7);
        var clrPrimary = System.Drawing.Color.FromArgb(0xdd, 0xe5, 0xff);
        var clrSecond  = System.Drawing.Color.FromArgb(0x88, 0x99, 0xbb);
        var clrMuted   = System.Drawing.Color.FromArgb(0x3a, 0x42, 0x60);
        var clrBorder  = System.Drawing.Color.FromArgb(0x25, 0x25, 0x35);

        txtApiToken         = new System.Windows.Forms.TextBox();
        btnShowHide         = new System.Windows.Forms.Button();
        btnTestToken        = new System.Windows.Forms.Button();
        lblTokenStatus      = new System.Windows.Forms.Label();
        txtLogDir           = new System.Windows.Forms.TextBox();
        chkAutoUpload       = new System.Windows.Forms.CheckBox();
        chkMinimizeToTray   = new System.Windows.Forms.CheckBox();
        chkStartWithWindows = new System.Windows.Forms.CheckBox();

        SuspendLayout();

        // Form
        Text            = "WarcraftPulse Uploader / Settings";
        BackColor       = bgWindow;
        ForeColor       = clrPrimary;
        ClientSize      = new System.Drawing.Size(420, 330);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        int x = 16, y = 16, w = 388;

        // ── API Token ────────────────────────────────────────────────────
        var lblApiToken = new System.Windows.Forms.Label();
        lblApiToken.Text      = "API Token";
        lblApiToken.ForeColor = clrPrimary;
        lblApiToken.Location  = new System.Drawing.Point(x, y);
        lblApiToken.Size      = new System.Drawing.Size(200, 20);

        var lblTokenHint = new System.Windows.Forms.Label();
        lblTokenHint.Text      = "from warcraftpulse.com/settings/personal-tokens";
        lblTokenHint.ForeColor = clrMuted;
        lblTokenHint.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
        lblTokenHint.Location  = new System.Drawing.Point(x, y + 20);
        lblTokenHint.Size      = new System.Drawing.Size(w, 16);

        y += 40;
        txtApiToken.UseSystemPasswordChar = true;
        txtApiToken.BackColor = bgInput;
        txtApiToken.ForeColor = clrPrimary;
        txtApiToken.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        txtApiToken.Location  = new System.Drawing.Point(x, y);
        txtApiToken.Size      = new System.Drawing.Size(258, 23);

        btnShowHide.Text      = "Show";
        btnShowHide.ForeColor = clrSecond;
        btnShowHide.BackColor = bgCard;
        btnShowHide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnShowHide.FlatAppearance.BorderColor = clrBorder;
        btnShowHide.FlatAppearance.BorderSize  = 1;
        btnShowHide.Location  = new System.Drawing.Point(x + 262, y);
        btnShowHide.Size      = new System.Drawing.Size(46, 23);
        btnShowHide.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnShowHide.Click    += btnShowHide_Click;

        btnTestToken.Text      = "Test";
        btnTestToken.ForeColor = clrPrimary;
        btnTestToken.BackColor = clrBlue;
        btnTestToken.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnTestToken.FlatAppearance.BorderSize = 0;
        btnTestToken.Location  = new System.Drawing.Point(x + 312, y);
        btnTestToken.Size      = new System.Drawing.Size(76, 23);
        btnTestToken.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnTestToken.Click    += btnTestToken_Click;

        y += 28;
        lblTokenStatus.Text      = "";
        lblTokenStatus.ForeColor = clrSecond;
        lblTokenStatus.Font      = new System.Drawing.Font("Segoe UI", 8.25f);
        lblTokenStatus.Location  = new System.Drawing.Point(x, y);
        lblTokenStatus.Size      = new System.Drawing.Size(w, 18);

        // ── WoW Log Directory ────────────────────────────────────────────
        y += 26;
        var lblLogDir = new System.Windows.Forms.Label();
        lblLogDir.Text      = "WoW Log Directory";
        lblLogDir.ForeColor = clrPrimary;
        lblLogDir.Location  = new System.Drawing.Point(x, y);
        lblLogDir.Size      = new System.Drawing.Size(200, 20);

        y += 22;
        txtLogDir.ReadOnly   = true;
        txtLogDir.BackColor  = bgInput;
        txtLogDir.ForeColor  = clrSecond;
        txtLogDir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        txtLogDir.Font       = new System.Drawing.Font("Consolas", 8f);
        txtLogDir.Location   = new System.Drawing.Point(x, y);
        txtLogDir.Size       = new System.Drawing.Size(298, 23);

        btnBrowse = new System.Windows.Forms.Button();
        btnBrowse.Text      = "Browse…";
        btnBrowse.ForeColor = clrSecond;
        btnBrowse.BackColor = bgCard;
        btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnBrowse.FlatAppearance.BorderColor = clrBorder;
        btnBrowse.FlatAppearance.BorderSize  = 1;
        btnBrowse.Location  = new System.Drawing.Point(x + 302, y);
        btnBrowse.Size      = new System.Drawing.Size(86, 23);
        btnBrowse.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnBrowse.Click    += btnBrowse_Click;

        // ── Toggles ──────────────────────────────────────────────────────
        y += 36;
        chkAutoUpload.Text      = "Upload automatically";
        chkAutoUpload.ForeColor = clrPrimary;
        chkAutoUpload.BackColor = bgWindow;
        chkAutoUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        chkAutoUpload.FlatAppearance.BorderColor = clrMuted;
        chkAutoUpload.Location  = new System.Drawing.Point(x, y);
        chkAutoUpload.Size      = new System.Drawing.Size(w, 22);
        chkAutoUpload.Checked   = true;

        y += 26;
        chkMinimizeToTray.Text      = "Minimize to tray on close";
        chkMinimizeToTray.ForeColor = clrPrimary;
        chkMinimizeToTray.BackColor = bgWindow;
        chkMinimizeToTray.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        chkMinimizeToTray.FlatAppearance.BorderColor = clrMuted;
        chkMinimizeToTray.Location  = new System.Drawing.Point(x, y);
        chkMinimizeToTray.Size      = new System.Drawing.Size(w, 22);
        chkMinimizeToTray.Checked   = true;

        y += 26;
        chkStartWithWindows.Text      = "Start with Windows";
        chkStartWithWindows.ForeColor = clrPrimary;
        chkStartWithWindows.BackColor = bgWindow;
        chkStartWithWindows.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        chkStartWithWindows.FlatAppearance.BorderColor = clrMuted;
        chkStartWithWindows.Location  = new System.Drawing.Point(x, y);
        chkStartWithWindows.Size      = new System.Drawing.Size(w, 22);
        chkStartWithWindows.Checked   = true;

        // ── Footer ───────────────────────────────────────────────────────
        y += 40;
        pnlFooter = new System.Windows.Forms.Panel();
        pnlFooter.BackColor = bgDark;
        pnlFooter.Location  = new System.Drawing.Point(0, y);
        pnlFooter.Size      = new System.Drawing.Size(420, 42);

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var lblVersion = new System.Windows.Forms.Label();
        lblVersion.Text      = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
        lblVersion.ForeColor = clrMuted;
        lblVersion.Font      = new System.Drawing.Font("Segoe UI", 8f);
        lblVersion.Location  = new System.Drawing.Point(10, 12);
        lblVersion.Size      = new System.Drawing.Size(80, 18);

        btnCancel = new System.Windows.Forms.Button();
        btnCancel.Text         = "Cancel";
        btnCancel.ForeColor    = clrSecond;
        btnCancel.BackColor    = bgCard;
        btnCancel.FlatStyle    = System.Windows.Forms.FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderColor = clrBorder;
        btnCancel.FlatAppearance.BorderSize  = 1;
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        btnCancel.Location     = new System.Drawing.Point(226, 8);
        btnCancel.Size         = new System.Drawing.Size(80, 26);
        btnCancel.Cursor       = System.Windows.Forms.Cursors.Hand;

        btnSave = new System.Windows.Forms.Button();
        btnSave.Text      = "Save";
        btnSave.ForeColor = clrPrimary;
        btnSave.BackColor = clrBlue;
        btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Location  = new System.Drawing.Point(314, 8);
        btnSave.Size      = new System.Drawing.Size(90, 26);
        btnSave.Cursor    = System.Windows.Forms.Cursors.Hand;
        btnSave.Click    += btnSave_Click;

        pnlFooter.Controls.AddRange(new System.Windows.Forms.Control[] { lblVersion, btnCancel, btnSave });

        // Button hover states
        static void WireHover(System.Windows.Forms.Button b, System.Drawing.Color normal, System.Drawing.Color hover)
        {
            b.MouseEnter += (_, _) => b.BackColor = hover;
            b.MouseLeave += (_, _) => b.BackColor = normal;
        }
        var blueHover = System.Drawing.Color.FromArgb(0x60, 0x78, 0xf8);
        var cardNormal = bgCard;
        var cardHover  = System.Drawing.Color.FromArgb(0x25, 0x25, 0x40);
        WireHover(btnShowHide,   cardNormal, cardHover);
        WireHover(btnTestToken,  clrBlue,    blueHover);
        WireHover(btnBrowse,     cardNormal, cardHover);
        WireHover(btnCancel,     cardNormal, cardHover);
        WireHover(btnSave,       clrBlue,    blueHover);

        // Blue accent stripe at top of footer panel
        pnlFooter.Paint += (_, e) =>
            e.Graphics.FillRectangle(new System.Drawing.SolidBrush(clrBorder), 0, 0, pnlFooter.Width, 1);

        Controls.AddRange(new System.Windows.Forms.Control[] {
            lblApiToken, lblTokenHint,
            txtApiToken, btnShowHide, btnTestToken, lblTokenStatus,
            lblLogDir, txtLogDir, btnBrowse,
            chkAutoUpload, chkMinimizeToTray, chkStartWithWindows,
            pnlFooter,
        });

        ResumeLayout(false);
    }

    // Control fields
    private System.Windows.Forms.TextBox  txtApiToken         = null!;
    private System.Windows.Forms.Button   btnShowHide         = null!;
    private System.Windows.Forms.Button   btnTestToken        = null!;
    private System.Windows.Forms.Label    lblTokenStatus      = null!;
    private System.Windows.Forms.TextBox  txtLogDir           = null!;
    private System.Windows.Forms.CheckBox chkAutoUpload       = null!;
    private System.Windows.Forms.CheckBox chkMinimizeToTray   = null!;
    private System.Windows.Forms.CheckBox chkStartWithWindows = null!;
    private System.Windows.Forms.Button   btnBrowse           = null!;
    private System.Windows.Forms.Button   btnCancel           = null!;
    private System.Windows.Forms.Button   btnSave             = null!;
    private System.Windows.Forms.Panel    pnlFooter           = null!;
}
