//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using WindBladeInspector.Core.Entities;

//namespace WindBladeInspector.Infrastructure.Reports.Components;

///// <summary>
///// Renders the per-blade section header and anomaly count summary bar.
///// </summary>
//internal sealed class BladeOverviewComponent
//{
//    private readonly Blade _blade;
//    private readonly int _bladeIndex;

//    private static readonly string NavyHex = "#0D1B2A";
//    private static readonly string TealHex = "#00A896";
//    private static readonly string LightGrayHex = "#E8EDF2";
//    private static readonly string WhiteHex = "#FFFFFF";

//    public BladeOverviewComponent(Blade blade, int bladeIndex)
//    {
//        _blade = blade;
//        _bladeIndex = bladeIndex;
//    }

//    public void Compose(IContainer container)
//    {
//        container.Column(col =>
//        {
//            // ── Blade section header ───────────────────────────────────────────────
//            col.Item().Background(NavyHex).Padding(12).Row(row =>
//            {
//                row.RelativeItem().Column(c =>
//                {
//                    c.Item().Text($"Blade {_blade.SerialNumber} — Inspection Results")
//                        .FontSize(14).FontColor(WhiteHex).Bold();
//                    if (_blade.Length > 0)
//                    {
//                        c.Item().Text($"Blade Length: {_blade.Length:F1} m")
//                            .FontSize(10).FontColor($"{WhiteHex}99");
//                    }
//                });

//                // Condition badge (top-right)
//                string sevColour = ExecutiveSummaryComponent.GetSeverityColour(_blade.Condition);
//                row.ConstantItem(90).AlignRight().AlignMiddle()
//                    .Background(sevColour).Padding(8)
//                    .Text($"SEV {_blade.Condition}")
//                    .FontSize(12).FontColor(WhiteHex).Bold().AlignCenter();
//            });

//            col.Item().Height(3).Background(TealHex);

//            // ── Quick stats strip ──────────────────────────────────────────────────
//            col.Item().Background(LightGrayHex).Padding(10).Row(statsRow =>
//            {
//                QuickStat(statsRow.RelativeItem(), "Total Defects", _blade.Anomalies.Count.ToString());
//                QuickStat(statsRow.RelativeItem(), "Severity 1–2", _blade.Anomalies.Count(a => a.Severity <= 2).ToString());
//                QuickStat(statsRow.RelativeItem(), "Severity 3", _blade.Anomalies.Count(a => a.Severity == 3).ToString());
//                QuickStat(statsRow.RelativeItem(), "Severity 4–5", _blade.Anomalies.Count(a => a.Severity >= 4).ToString());
//                QuickStat(statsRow.RelativeItem(), "Views", string.Join(", ", _blade.Views.Select(v => v.Side)));
//            });
//        });
//    }

//    private static void QuickStat(IContainer container, string label, string value)
//    {
//        container.Column(c =>
//        {
//            c.Item().AlignCenter().Text(value).FontSize(16).FontColor("#0D1B2A").Bold();
//            c.Item().AlignCenter().Text(label).FontSize(8).FontColor("#6C7A89");
//        });
//    }
//}



using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using WindBladeInspector.Core.Entities;
using static WindBladeInspector.Infrastructure.Reports.PdfReportGenerationService;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class BladeOverviewComponent
{
    private readonly Blade _blade;
    private readonly List<DefectEntry> _defects;

    public BladeOverviewComponent(Blade blade, List<DefectEntry> defects)
    {
        _blade = blade;
        _defects = defects;
    }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(20).Text($"Blade {_blade.SerialNumber} Overview").FontSize(14).Bold();

            // Placeholder for the 4-blade diagram logic. Assuming we display a text placeholder if image isn't generated via Skia
            col.Item().PaddingBottom(30).Height(150).Background("#F9F9F9").AlignCenter().AlignMiddle()
               .Text("[ BLADE DIAGRAMS: LE | PS | TE | SS ]\n(Requires Diagram Image Asset)").FontSize(10).FontColor("#999999");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn();
                    cols.RelativeColumn(); cols.RelativeColumn(); cols.ConstantColumn(60);
                });

                void HeaderCell(string text) => table.Cell().BorderBottom(1).Padding(5).Text(text).Bold().FontSize(10);

                HeaderCell("Damage ID");
                HeaderCell("Blade side");
                HeaderCell("Material");
                HeaderCell("Damage Type");
                HeaderCell("Subtype");
                HeaderCell("Severity");

                foreach (var d in _defects)
                {
                    void DataCell(string text) => table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(text).FontSize(10);

                    DataCell(d.DefectId);
                    DataCell(GetViewLabel(d.View));
                    DataCell("Auxiliary Component"); // Replace with actual property if mapped in your domain
                    DataCell(d.Anomaly.GetDefectTypeDisplay() ?? "LEP");
                    DataCell(d.Anomaly.Recommendation ?? "Damaged");
                    DataCell(d.Anomaly.Severity.ToString());
                }
            });
        });
    }

    private static string GetViewLabel(string side) => side switch { "PS" => "PS", "SS" => "SS", "LE" => "LE", "TE" => "TE", _ => side };
}