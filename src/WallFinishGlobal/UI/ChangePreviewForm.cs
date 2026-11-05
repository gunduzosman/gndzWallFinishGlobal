using System.Drawing;
using System.Windows.Forms;
using WallFinishGlobal.Models;
namespace WallFinishGlobal.UI;
public sealed class ChangePreviewForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, AllowUserToAddRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    public ChangePreviewForm(IReadOnlyList<PlannedChange> plan)
    {
        Text = "Duvar Kaplamaları — Önizleme"; Width = 1180; Height = 620; StartPosition = FormStartPosition.CenterParent;
        Add("Durum", "Action"); Add("Duvar Id", "WallId"); Add("Eski tip", "WallTypeName"); Add("Yeni tip", "NewTypeName"); Add("Kural", "RuleCode"); Add("Malzeme", "MaterialName"); Add("Kalınlık mm", "Thickness"); Add("Uyarılar", "Warnings");
        grid.DataSource = plan.Select(p => new Row(p)).ToList();
        grid.CellFormatting += (_, e) => { if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count || grid.Rows[e.RowIndex].DataBoundItem is not Row row) return; if (row.Action is ChangeAction.Conflict or ChangeAction.SkipNoMaterial or ChangeAction.SkipNoThickness) { e.CellStyle.BackColor = Color.MistyRose; e.CellStyle.ForeColor = Color.DarkRed; } else if (row.Action == ChangeAction.AlreadyCompliant) e.CellStyle.BackColor = Color.Honeydew; };
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 46 };
        var cancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 110 };
        var ok = new Button { Text = "Uygula satırlarını onayla", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 220 };
        footer.Controls.Add(cancel); footer.Controls.Add(ok); Controls.Add(grid); Controls.Add(footer); AcceptButton = ok; CancelButton = cancel;
    }
    private void Add(string header, string property) => grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, SortMode = DataGridViewColumnSortMode.Automatic });
    private sealed class Row
    {
        public ChangeAction Action { get; } public long WallId { get; } public string WallTypeName { get; } public string NewTypeName { get; } public string RuleCode { get; } public string MaterialName { get; } public string Thickness { get; } public string Warnings { get; }
        public Row(PlannedChange p) { Action = p.Action; WallId = p.WallId; WallTypeName = p.WallTypeName; NewTypeName = p.NewTypeName ?? "—"; RuleCode = p.RuleCode ?? "—"; MaterialName = p.MaterialName ?? "—"; Thickness = p.ThicknessMm?.ToString("0.##") ?? "—"; Warnings = string.Join(" | ", p.Warnings); }
    }
}
