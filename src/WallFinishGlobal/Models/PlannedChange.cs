namespace WallFinishGlobal.Models;

public enum ChangeAction { Apply, SkipNoMaterial, SkipNoThickness, Conflict, AlreadyCompliant }
public sealed class PlannedChange
{
    public long WallId { get; init; }
    public long SourceTypeId { get; init; }
    public string WallTypeName { get; init; } = "";
    public string? NewTypeName { get; set; }
    public string? RuleCode { get; set; }
    public string? FinishCode { get; set; }
    public string? MaterialName { get; set; }
    public long? MaterialId { get; set; }
    public double? ThicknessMm { get; set; }
    public ChangeAction Action { get; set; }
    public List<string> Warnings { get; } = new();
}
