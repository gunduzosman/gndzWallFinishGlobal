using System.Windows.Forms;
namespace WallFinishInstaller;
internal static class Program
{
    [STAThread] private static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new InstallerForm()); }
}
internal sealed class InstallerForm : Form
{
    private readonly Label status = new() { Dock = DockStyle.Top, Height = 80, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
    public InstallerForm()
    {
        Text = "WallFinishGlobal — Kurulum"; Width = 520; Height = 220; StartPosition = FormStartPosition.CenterScreen;
        status.Text = "Eklentiyi mevcut Windows kullanıcısı için Revit 2025'e kurar.\nKurulumdan önce Revit'i kapatın.";
        var button = new Button { Text = "Revit 2025'e Kur", Dock = DockStyle.Bottom, Height = 42 }; button.Click += (_, _) => Install(); Controls.Add(status); Controls.Add(button);
    }
    private void Install()
    {
        try
        {
            var payload = Path.Combine(AppContext.BaseDirectory, "payload");
            var dll = Path.Combine(payload, "WallFinishGlobal.dll"); var rules = Path.Combine(payload, "finish-rules.json");
            if (!File.Exists(dll) || !File.Exists(rules)) throw new FileNotFoundException("Kurulum paketi eksik; payload DLL/JSON bulunamadı.");
            var addins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", "2025");
            var target = Path.Combine(addins, "WallFinishGlobal"); Directory.CreateDirectory(Path.Combine(target, "Resources"));
            File.Copy(dll, Path.Combine(target, "WallFinishGlobal.dll"), true); File.Copy(rules, Path.Combine(target, "Resources", "finish-rules.json"), true);
            var manifest = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<RevitAddIns><AddIn Type=\"Application\"><Name>WallFinishGlobal</Name><Assembly>{System.Security.SecurityElement.Escape(Path.Combine(target, "WallFinishGlobal.dll"))}</Assembly><AddInId>8a852675-5f6b-47bd-9d8b-1498a8646e1d</AddInId><FullClassName>WallFinishGlobal.App</FullClassName><VendorId>GNDZ</VendorId><VendorDescription>Wall finish management</VendorDescription></AddIn></RevitAddIns>";
            File.WriteAllText(Path.Combine(addins, "WallFinishGlobal.addin"), manifest, new System.Text.UTF8Encoding(false));
            status.Text = $"Kurulum tamamlandı.\n{target}\nRevit 2025'i yeniden başlatın.";
            MessageBox.Show(this, "Kurulum tamamlandı. Revit 2025'i yeniden başlatın.", "WallFinishGlobal", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Kurulum başarısız", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
