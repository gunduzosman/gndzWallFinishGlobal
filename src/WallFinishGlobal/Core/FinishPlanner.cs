using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using WallFinishGlobal.Models;

namespace WallFinishGlobal.Core;

public static class FinishPlanner
{
    private const double MmPerFoot = 304.8, ToleranceMm = 0.05;
    public static IReadOnlyList<PlannedChange> BuildPlan(Document doc, FinishRuleSet rules, IReadOnlyList<RoomWallPair> pairs, MaterialResolver materialResolver)
    {
        var byWall = new Dictionary<long, List<(Room Room, FinishRule Rule)>>();
        foreach (var (room, wall) in pairs)
        {
            var roomType = ReadRoomType(room, rules.RoomTypeParameterName);
            var levelName = (doc.GetElement(wall.LevelId) as Level)?.Name;
            var wallType = doc.GetElement(wall.GetTypeId()) as WallType;
            if (wallType is null) continue;
            foreach (var rule in RuleEngine.ResolveAll(rules, roomType, room.Name, levelName, wallType.Name))
            {
                if (!byWall.TryGetValue(wall.Id.Value, out var list)) byWall[wall.Id.Value] = list = new();
                list.Add((room, rule));
            }
        }
        var plan = new List<PlannedChange>();
        foreach (var entry in byWall.OrderBy(p => p.Key))
        {
            var wall = doc.GetElement(new ElementId(entry.Key)) as Wall;
            if (wall is null) continue;
            var type = doc.GetElement(wall.GetTypeId()) as WallType;
            if (type is null) continue;
            var change = new PlannedChange { WallId = wall.Id.Value, SourceTypeId = type.Id.Value, WallTypeName = type.Name, Action = ChangeAction.Apply };
            var signatures = entry.Value.Select(x => (Code: x.Rule.Finish.FinishCode.Trim().ToUpperInvariant(), Material: x.Rule.Finish.MaterialName.Trim().ToUpperInvariant(), Thickness: x.Rule.Finish.ThicknessMm))
                .Distinct().ToList();
            if (signatures.Count > 1)
            {
                change.Action = ChangeAction.Conflict;
                change.Warnings.Add("Çakışan bitişler: " + string.Join(" | ", signatures.Select(s => $"{s.Code}/{s.Material}/{s.Thickness?.ToString("0.##") ?? "korunacak"}mm")));
                change.Warnings.Add("Kural sessizce seçilmedi; duvar atlandı."); plan.Add(change); continue;
            }
            var rule = entry.Value.OrderByDescending(x => x.Rule.Priority).First().Rule;
            change.RuleCode = rule.Code; change.FinishCode = rule.Finish.FinishCode; change.MaterialName = rule.Finish.MaterialName;
            var material = materialResolver.Resolve(doc, rule.Finish);
            if (material is null)
            {
                change.Action = ChangeAction.SkipNoMaterial; change.Warnings.Add($"Malzeme bulunamadı: {rule.Finish.MaterialName} ({rule.Finish.FinishCode})."); plan.Add(change); continue;
            }
            change.MaterialId = material.Id.Value;
            var structure = type.GetCompoundStructure();
            if (structure is null)
            {
                change.Action = ChangeAction.SkipNoThickness; change.Warnings.Add("Duvar tipinde CompoundStructure bulunamadı."); plan.Add(change); continue;
            }
            var finishes = FinishLayers(structure); var thickness = rule.Finish.ThicknessMm;
            if (thickness is null)
            {
                if (finishes.Count == 0)
                {
                    change.Action = ChangeAction.SkipNoThickness; change.Warnings.Add("Kuralda kalınlık yok ve mevcut bitiş katmanı bulunamadı."); plan.Add(change); continue;
                }
                if (finishes.Any(f => Math.Abs(f.WidthMm - finishes[0].WidthMm) > ToleranceMm))
                {
                    change.Action = ChangeAction.SkipNoThickness; change.Warnings.Add("Finish1/Finish2 kalınlıkları farklı; simetrik uygulama atlandı."); plan.Add(change); continue;
                }
                thickness = finishes[0].WidthMm;
            }
            change.ThicknessMm = thickness;
            if (IsCompliant(structure, material.Id, thickness.Value))
            {
                change.Action = ChangeAction.AlreadyCompliant; change.NewTypeName = type.Name; plan.Add(change); continue;
            }
            change.NewTypeName = TypeVariantManager.GenerateVariantName(rules, TypeVariantManager.GetBaseTypeName(type.Name, rules), rule.Finish.FinishCode);
            plan.Add(change);
        }
        return plan;
    }
    private static string? ReadRoomType(Room room, string parameterName)
    {
        var p = room.LookupParameter(parameterName);
        if (p is null || !p.HasValue) return null;
        return p.StorageType switch { StorageType.String => p.AsString(), StorageType.Integer => p.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture), _ => p.AsValueString() };
    }
    private static List<(MaterialFunctionAssignment Function, double WidthMm)> FinishLayers(CompoundStructure structure)
    {
        var result = new List<(MaterialFunctionAssignment, double)>();
        for (var i = 0; i < structure.LayerCount; i++)
        {
            var layer = structure.GetLayer(i);
            if (layer.Function is MaterialFunctionAssignment.Finish1 or MaterialFunctionAssignment.Finish2) result.Add((layer.Function, layer.Width * MmPerFoot));
        }
        return result;
    }
    private static bool IsCompliant(CompoundStructure structure, ElementId materialId, double thickness)
    {
        var f1 = 0; var f2 = 0;
        for (var i = 0; i < structure.LayerCount; i++)
        {
            var layer = structure.GetLayer(i);
            if (layer.Function is not (MaterialFunctionAssignment.Finish1 or MaterialFunctionAssignment.Finish2)) continue;
            if (layer.MaterialId != materialId || Math.Abs(layer.Width * MmPerFoot - thickness) > ToleranceMm) return false;
            if (layer.Function == MaterialFunctionAssignment.Finish1) f1++; else f2++;
        }
        return f1 == 1 && f2 == 1;
    }
}
