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

        lblStatus        = new Label();
        lblTokenWarning  = new Label();
        btnUpload        = new Button();
        btnOpenReport    = new Button();
        btnSettings      = new Button();

        SuspendLayout();

        AutoScaleMode   = AutoScaleMode.Font;
        ClientSize      = new System.Drawing.Size(440, 500);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;

        // Status label
        lblStatus.Text      = "Starting…";
        lblStatus.Location  = new System.Drawing.Point(12, 12);
        lblStatus.Size      = new System.Drawing.Size(416, 22);
        lblStatus.Font      = new System.Drawing.Font("Segoe UI", 9f);

        // Token warning label
        lblTokenWarning.Text      = "No API token configured — open Settings to add one.";
        lblTokenWarning.ForeColor = System.Drawing.Color.FromArgb(248, 113, 113);
        lblTokenWarning.Location  = new System.Drawing.Point(12, 40);
        lblTokenWarning.Size      = new System.Drawing.Size(416, 22);
        lblTokenWarning.Visible   = false;

        // Upload button
        btnUpload.Text     = "Upload Log File…";
        btnUpload.Location = new System.Drawing.Point(12, 440);
        btnUpload.Size     = new System.Drawing.Size(140, 30);
        btnUpload.Click   += btnUpload_Click;

        // Open report button
        btnOpenReport.Text     = "Open Report";
        btnOpenReport.Location = new System.Drawing.Point(160, 440);
        btnOpenReport.Size     = new System.Drawing.Size(120, 30);
        btnOpenReport.Visible  = false;
        btnOpenReport.Click   += btnOpenReport_Click;

        // Settings button
        btnSettings.Text     = "Settings…";
        btnSettings.Location = new System.Drawing.Point(350, 440);
        btnSettings.Size     = new System.Drawing.Size(80, 30);
        btnSettings.Click   += btnSettings_Click;

        Controls.AddRange(new Control[] {
            lblStatus, lblTokenWarning,
            btnUpload, btnOpenReport, btnSettings,
        });

        ResumeLayout(false);
    }

    // Control fields
    private Label  lblStatus       = null!;
    private Label  lblTokenWarning = null!;
    private Button btnUpload        = null!;
    private Button btnOpenReport    = null!;
    private Button btnSettings      = null!;
}
