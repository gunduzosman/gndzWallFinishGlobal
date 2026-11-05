using Autodesk.Revit.DB;
using WallFinishGlobal.Models;
namespace WallFinishGlobal.Core;
public sealed class MaterialResolver
{
    public Material? Resolve(Document doc, FinishSpec spec)
    {
        var materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
        foreach (var material in materials)
        {
            var p = material.LookupParameter("FinishCode");
            if (p?.StorageType == StorageType.String && string.Equals(p.AsString()?.Trim(), spec.FinishCode, StringComparison.OrdinalIgnoreCase)) return material;
        }
        return materials.FirstOrDefault(m => string.Equals(m.Name, spec.MaterialName, StringComparison.OrdinalIgnoreCase));
    }
}
