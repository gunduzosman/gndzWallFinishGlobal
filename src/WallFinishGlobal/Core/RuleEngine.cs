using System.Text.Json;
using WallFinishGlobal.Models;

namespace WallFinishGlobal.Core;

public static class RuleEngine
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
    public static FinishRuleSet Load(string jsonPath)
    {
        if (!File.Exists(jsonPath)) throw new FileNotFoundException("Kaplama kural dosyası bulunamadı.", jsonPath);
        var result = JsonSerializer.Deserialize<FinishRuleSet>(File.ReadAllText(jsonPath), Options) ?? throw new InvalidDataException("Kural JSON dosyası boş/geçersiz.");
        Validate(result);
        return result;
    }
    public static IReadOnlyList<FinishRule> ResolveAll(FinishRuleSet rules, string? roomType, string? roomName, string? levelName, string? wallTypeName)
        => rules.Rules.Where(r => r.Matches(roomType, roomName, levelName, wallTypeName, rules.Matching.FallbackToRoomName)).ToList();
    private static void Validate(FinishRuleSet r)
    {
        if (r.Version != 1) throw new InvalidDataException($"Desteklenmeyen kural şema sürümü: {r.Version}.");
        if (!string.Equals(r.ConflictPolicy, "warnAndSkip", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("v0.1 yalnızca conflictPolicy=warnAndSkip destekler.");
        if (string.IsNullOrWhiteSpace(r.VariantPrefix) || string.IsNullOrWhiteSpace(r.VariantSeparator)) throw new InvalidDataException("Varyant prefix/separator boş olamaz.");
        if (!string.Equals(r.Matching.NameMatchMode, "wildcard", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("v0.1 yalnızca wildcard eşleştirmeyi destekler.");
        if (r.Rules.Count == 0) throw new InvalidDataException("En az bir kural tanımlanmalıdır.");
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in r.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Code) || !codes.Add(rule.Code)) throw new InvalidDataException("Kural kodları boş ve benzersiz olmalıdır.");
            if (string.IsNullOrWhiteSpace(rule.Finish.FinishCode) || string.IsNullOrWhiteSpace(rule.Finish.MaterialName)) throw new InvalidDataException($"{rule.Code}: finishCode ve materialName zorunlu.");
            if (rule.Finish.ThicknessMm is <= 0) throw new InvalidDataException($"{rule.Code}: thicknessMm sıfırdan büyük olmalı.");
        }
    }
}
