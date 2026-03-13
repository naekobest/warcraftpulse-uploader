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

        // Controls
        var lblApiToken       = new Label();
        txtApiToken           = new TextBox();
        btnTestToken          = new Button();
        lblTokenStatus        = new Label();
        var lblServerUrl      = new Label();
        txtServerUrl          = new TextBox();
        var lblLogDir         = new Label();
        txtLogDir             = new TextBox();
        chkAutoUpload         = new CheckBox();
        chkMinimizeToTray     = new CheckBox();
        chkStartWithWindows   = new CheckBox();
        var btnCancel         = new Button();
        var btnSave           = new Button();

        SuspendLayout();

        // Form
        Text            = "WarcraftPulse Uploader / Settings";
        ClientSize      = new System.Drawing.Size(420, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;

        int x = 12, y = 12, w = 396;

        // API Token label
        lblApiToken.Text     = "API Token";
        lblApiToken.Location = new System.Drawing.Point(x, y);
        lblApiToken.Size     = new System.Drawing.Size(200, 20);

        y += 22;
        // API Token input
        txtApiToken.UseSystemPasswordChar = true;
        txtApiToken.Location = new System.Drawing.Point(x, y);
        txtApiToken.Size     = new System.Drawing.Size(290, 23);
        // Test button
        btnTestToken.Text     = "Test";
        btnTestToken.Location = new System.Drawing.Point(x + 296, y);
        btnTestToken.Size     = new System.Drawing.Size(100, 23);
        btnTestToken.Click   += btnTestToken_Click;

        y += 28;
        // Token status label
        lblTokenStatus.Text     = "";
        lblTokenStatus.Location = new System.Drawing.Point(x, y);
        lblTokenStatus.Size     = new System.Drawing.Size(w, 20);
        lblTokenStatus.Font     = new System.Drawing.Font("Segoe UI", 8.25f);

        y += 28;
        // Server URL label
        lblServerUrl.Text     = "Server URL";
        lblServerUrl.Location = new System.Drawing.Point(x, y);
        lblServerUrl.Size     = new System.Drawing.Size(200, 20);

        y += 22;
        txtServerUrl.Location = new System.Drawing.Point(x, y);
        txtServerUrl.Size     = new System.Drawing.Size(w, 23);

        y += 32;
        // Log directory label
        lblLogDir.Text     = "WoW Log Directory";
        lblLogDir.Location = new System.Drawing.Point(x, y);
        lblLogDir.Size     = new System.Drawing.Size(200, 20);

        y += 22;
        txtLogDir.ReadOnly = true;
        txtLogDir.Location = new System.Drawing.Point(x, y);
        txtLogDir.Size     = new System.Drawing.Size(290, 23);
        var btnBrowseLocal = new Button();
        btnBrowseLocal.Text     = "Browse…";
        btnBrowseLocal.Location = new System.Drawing.Point(x + 296, y);
        btnBrowseLocal.Size     = new System.Drawing.Size(100, 23);
        btnBrowseLocal.Click   += btnBrowse_Click;

        y += 36;
        // Checkboxes
        chkAutoUpload.Text     = "Upload automatically";
        chkAutoUpload.Location = new System.Drawing.Point(x, y);
        chkAutoUpload.Size     = new System.Drawing.Size(w, 22);
        chkAutoUpload.Checked  = true;

        y += 26;
        chkMinimizeToTray.Text     = "Minimize to tray on close";
        chkMinimizeToTray.Location = new System.Drawing.Point(x, y);
        chkMinimizeToTray.Size     = new System.Drawing.Size(w, 22);
        chkMinimizeToTray.Checked  = true;

        y += 26;
        chkStartWithWindows.Text     = "Start with Windows";
        chkStartWithWindows.Location = new System.Drawing.Point(x, y);
        chkStartWithWindows.Size     = new System.Drawing.Size(w, 22);
        chkStartWithWindows.Checked  = true;

        y += 36;
        // Cancel / Save buttons
        btnCancel.Text         = "Cancel";
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location     = new System.Drawing.Point(w - 168, y);
        btnCancel.Size         = new System.Drawing.Size(80, 26);
        btnSave.Text           = "Save";
        btnSave.Location       = new System.Drawing.Point(w - 80, y);
        btnSave.Size           = new System.Drawing.Size(80, 26);
        btnSave.Click         += btnSave_Click;

        Controls.AddRange(new Control[] {
            lblApiToken, txtApiToken, btnTestToken, lblTokenStatus,
            lblServerUrl, txtServerUrl,
            lblLogDir, txtLogDir, btnBrowseLocal,
            chkAutoUpload, chkMinimizeToTray, chkStartWithWindows,
            btnCancel, btnSave,
        });

        ResumeLayout(false);
    }

    // Control fields
    private TextBox  txtApiToken     = null!;
    private Button   btnTestToken    = null!;
    private Label    lblTokenStatus  = null!;
    private TextBox  txtServerUrl    = null!;
    private TextBox  txtLogDir       = null!;
    private CheckBox chkAutoUpload   = null!;
    private CheckBox chkMinimizeToTray    = null!;
    private CheckBox chkStartWithWindows  = null!;
}
