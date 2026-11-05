using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;
namespace WallFinishGlobal.Commands;
[Transaction(TransactionMode.ReadOnly)]
public sealed class ReportDerivedTypesCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        var doc = data.Application.ActiveUIDocument?.Document;
        if (doc is null) { message = "Açık Revit projesi yok."; return Result.Cancelled; }
        try
        {
            var types = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().ToList();
            var usage = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().GroupBy(w => w.GetTypeId().Value).ToDictionary(g => g.Key, g => g.Count());
            var derived = types.Where(t => t.Name.StartsWith("ORJ__", StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.Name).ToList();
            if (derived.Count == 0) { TaskDialog.Show("Türetilmiş Tipler", "ORJ__ önekli tip bulunamadı."); return Result.Succeeded; }
            var text = new StringBuilder();
            foreach (var type in derived) text.AppendLine($"{type.Name} — kullanım: {usage.GetValueOrDefault(type.Id.Value, 0)} — adlandırma: {(ValidName(type.Name) ? "uygun" : "kontrol gerekli")}");
            var output = text.ToString(); TaskDialog.Show("Türetilmiş Tipler", output.Length > 9000 ? output[..9000] + "\n...rapor kısaltıldı." : output);
            return Result.Succeeded;
        }
        catch (Exception ex) { message = ex.Message; return Result.Failed; }
    }
    private static bool ValidName(string name) { var a = name.IndexOf("__", StringComparison.Ordinal); var b = name.LastIndexOf("__", StringComparison.Ordinal); return a == 3 && b > a + 2 && b + 2 < name.Length; }
}
