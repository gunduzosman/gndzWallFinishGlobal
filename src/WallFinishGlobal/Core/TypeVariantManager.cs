using Autodesk.Revit.DB;
using WallFinishGlobal.Models;
namespace WallFinishGlobal.Core;
public static class TypeVariantManager
{
    public static string GenerateVariantName(FinishRuleSet rules, string sourceName, string code) => $"{rules.VariantPrefix}{rules.VariantSeparator}{sourceName}{rules.VariantSeparator}{code}";
    public static string GetBaseTypeName(string name, FinishRuleSet rules)
    {
        var marker = rules.VariantPrefix + rules.VariantSeparator;
        if (!name.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) return name;
        var body = name[marker.Length..]; var suffix = body.LastIndexOf(rules.VariantSeparator, StringComparison.Ordinal);
        return suffix > 0 ? body[..suffix] : name;
    }
    public static void Apply(Document doc, IEnumerable<PlannedChange> plan)
    {
        foreach (var change in plan.Where(p => p.Action == ChangeAction.Apply))
        {
            var wall = doc.GetElement(new ElementId(change.WallId)) as Wall ?? throw new InvalidOperationException($"Duvar bulunamadı: {change.WallId}.");
            var source = doc.GetElement(new ElementId(change.SourceTypeId)) as WallType ?? throw new InvalidOperationException("Kaynak duvar tipi bulunamadı.");
            var material = change.MaterialId is long id ? doc.GetElement(new ElementId(id)) as Material : null;
            if (material is null) throw new InvalidOperationException($"Malzeme artık mevcut değil: {change.MaterialName}.");
            var variant = GetOrCreateVariant(doc, source, change.NewTypeName!, material, change.ThicknessMm!.Value);
            wall.WallType = variant;
        }
    }
    private static WallType GetOrCreateVariant(Document doc, WallType source, string name, Material material, double thicknessMm)
    {
        var existing = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (!IsCompliant(existing.GetCompoundStructure(), material.Id, thicknessMm)) throw new InvalidOperationException($"'{name}' tipi beklenen malzeme/kalınlıkla uyuşmuyor. İşlem geri alınacak; tipi kontrol edin.");
            return existing;
        }
        var structure = source.GetCompoundStructure() ?? throw new InvalidOperationException($"'{source.Name}' tipinde CompoundStructure yok.");
        var f1 = new List<int>(); var f2 = new List<int>();
        for (var i = 0; i < structure.LayerCount; i++)
        {
            var fn = structure.GetLayer(i).Function;
            if (fn == MaterialFunctionAssignment.Finish1) f1.Add(i);
            if (fn == MaterialFunctionAssignment.Finish2) f2.Add(i);
        }
        if (f1.Count > 1 || f2.Count > 1) throw new InvalidOperationException($"'{source.Name}' tipinde birden fazla aynı Finish katmanı bulundu; güvenli düzenleme yapılmadı.");
        var width = thicknessMm / 304.8;
        if (f1.Count == 0) structure.InsertLayer(0, new CompoundStructureLayer(width, MaterialFunctionAssignment.Finish1, material.Id));
        if (f2.Count == 0) structure.InsertLayer(structure.LayerCount, new CompoundStructureLayer(width, MaterialFunctionAssignment.Finish2, material.Id));
        for (var i = 0; i < structure.LayerCount; i++)
        {
            var fn = structure.GetLayer(i).Function;
            if (fn is MaterialFunctionAssignment.Finish1 or MaterialFunctionAssignment.Finish2)
            { structure.SetLayerWidth(i, width); structure.SetMaterialId(i, material.Id); }
        }
        var variant = (WallType)source.Duplicate(name);
        variant.SetCompoundStructure(structure);
        return variant;
    }
    private static bool IsCompliant(CompoundStructure? structure, ElementId materialId, double thickness)
    {
        if (structure is null) return false;
        var f1 = 0; var f2 = 0;
        for (var i = 0; i < structure.LayerCount; i++)
        {
            var layer = structure.GetLayer(i);
            if (layer.Function is not (MaterialFunctionAssignment.Finish1 or MaterialFunctionAssignment.Finish2)) continue;
            if (layer.MaterialId != materialId || Math.Abs(layer.Width * 304.8 - thickness) > 0.05) return false;
            if (layer.Function == MaterialFunctionAssignment.Finish1) f1++; else f2++;
        }
        return f1 == 1 && f2 == 1;
    }
}
