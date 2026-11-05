using System.Text.RegularExpressions;

namespace WallFinishGlobal.Models;

public sealed class FinishRule
{
    public string Code { get; set; } = "";
    public int Priority { get; set; }
    public MatchSpec Match { get; set; } = new();
    public FinishSpec Finish { get; set; } = new();

    public bool Matches(string? roomType, string? roomName, string? levelName, string? wallTypeName, bool fallbackToRoomName)
    {
        if (Match.Levels.Count > 0 && !Wildcard.Any(Match.Levels, levelName)) return false;
        if (Match.WallTypesAnyOf.Count > 0 && !Wildcard.Any(Match.WallTypesAnyOf, wallTypeName)) return false;
        var key = !string.IsNullOrWhiteSpace(roomType) ? roomType : fallbackToRoomName ? roomName : null;
        if (key is null) return false;
        if (Match.ExcludeRoomTypesAnyOf.Count > 0 && Wildcard.Any(Match.ExcludeRoomTypesAnyOf, key)) return false;
        return Match.RoomTypesAnyOf.Count == 0 || Wildcard.Any(Match.RoomTypesAnyOf, key);
    }
}

public sealed class MatchSpec
{
    public List<string> RoomTypesAnyOf { get; set; } = new();
    public List<string> ExcludeRoomTypesAnyOf { get; set; } = new();
    public List<string> Levels { get; set; } = new();
    public List<string> WallTypesAnyOf { get; set; } = new();
}
public sealed class FinishSpec
{
    public string FinishCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public double? ThicknessMm { get; set; }
}
public sealed class FinishRuleSet
{
    public int Version { get; set; }
    public VariantNaming VariantNaming { get; set; } = new();
    public Matching Matching { get; set; } = new();
    public List<FinishRule> Rules { get; set; } = new();
    public string ConflictPolicy { get; set; } = "";
    public string VariantPrefix => VariantNaming.Prefix;
    public string VariantSeparator => VariantNaming.Separator;
    public string RoomTypeParameterName => Matching.RoomTypeParameterName;
}
public sealed class VariantNaming { public string Prefix { get; set; } = "ORJ"; public string Separator { get; set; } = "__"; }
public sealed class Matching
{
    public string RoomTypeParameterName { get; set; } = "FinishRoomType";
    public bool FallbackToRoomName { get; set; } = true;
    public string NameMatchMode { get; set; } = "wildcard";
}
public static class Wildcard
{
    public static bool Any(IEnumerable<string> patterns, string? value)
    {
        if (value is null) return false;
        foreach (var pattern in patterns)
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            if (Regex.IsMatch(value, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))) return true;
        }
        return false;
    }
}
