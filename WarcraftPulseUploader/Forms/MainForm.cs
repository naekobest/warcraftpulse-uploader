namespace WarcraftPulseUploader.Forms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        var version = System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version;
        Text = $"WarcraftPulse Uploader v{version?.Major}.{version?.Minor}.{version?.Build}";
    }
}
