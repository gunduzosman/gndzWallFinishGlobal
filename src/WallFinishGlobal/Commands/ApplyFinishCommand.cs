using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using WallFinishGlobal.Core;
using WallFinishGlobal.Models;
using WallFinishGlobal.UI;
namespace WallFinishGlobal.Commands;
[Transaction(TransactionMode.Manual)]
public sealed class ApplyFinishCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        var doc = data.Application.ActiveUIDocument?.Document;
        if (doc is null) { message = "Açık Revit projesi yok."; return Result.Cancelled; }
        try
        {
            var dir = Path.GetDirectoryName(typeof(App).Assembly.Location)!;
            var rules = RuleEngine.Load(Path.Combine(dir, "Resources", "finish-rules.json"));
            var plan = FinishPlanner.BuildPlan(doc, rules, RoomBoundaryResolver.GetPairs(doc), new MaterialResolver());
            if (plan.Count == 0) { TaskDialog.Show("Duvar Kaplamaları", "Kurallarla eşleşen duvar bulunamadı."); return Result.Succeeded; }
            using var preview = new ChangePreviewForm(plan);
            if (preview.ShowDialog() != System.Windows.Forms.DialogResult.OK) return Result.Cancelled;
            using var tx = new Transaction(doc, "Duvar Kaplamalarını Uygula"); tx.Start();
            try { TypeVariantManager.Apply(doc, plan); tx.Commit(); }
            catch { if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack(); throw; }
            var apply = plan.Count(p => p.Action == ChangeAction.Apply);
            var compliant = plan.Count(p => p.Action == ChangeAction.AlreadyCompliant);
            TaskDialog.Show("Duvar Kaplamaları", $"Uygulanan: {apply}\nAtlanan/çakışan: {plan.Count - apply - compliant}\nZaten uygun: {compliant}");
            return Result.Succeeded;
        }
        catch (Exception ex) { message = ex.Message; return Result.Failed; }
    }
}
