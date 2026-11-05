using Autodesk.Revit.UI;
using System.Reflection;

namespace WallFinishGlobal;

public sealed class App : IExternalApplication
{
    private const string TabName = "Kaplama";
    private const string PanelName = "Duvar Kaplamaları";

    public Result OnStartup(UIControlledApplication application)
    {
        try { application.CreateRibbonTab(TabName); }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
        var panel = application.GetRibbonPanels(TabName).FirstOrDefault(p => p.Name == PanelName)
                    ?? application.CreateRibbonPanel(TabName, PanelName);
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        AddButton(panel, assemblyPath, "Uygula", "WallFinishGlobal.Commands.ApplyFinishCommand",
            "Kaplama planı oluşturur, önizletir ve onaylanan değişiklikleri uygular.");
        AddButton(panel, assemblyPath, "Rapor", "WallFinishGlobal.Commands.ReportDerivedTypesCommand",
            "Türetilmiş duvar tiplerini ve kullanım sayılarını raporlar.");
        return Result.Succeeded;
    }

    private static void AddButton(RibbonPanel panel, string assemblyPath, string name, string command, string tooltip)
    {
        if (panel.GetItems().Any(i => i.Name == $"WallFinishGlobal_{name}")) return;
        panel.AddItem(new PushButtonData($"WallFinishGlobal_{name}", name, assemblyPath, command) { ToolTip = tooltip });
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}
