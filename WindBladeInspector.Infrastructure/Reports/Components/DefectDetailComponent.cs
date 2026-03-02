using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

/// <summary>
/// Renders the per-blade defect detail table with all anomaly data rows.
/// Each row: #, Defect Type, Classification Path, Severity badge, Side, Radius, Area, Recommendation.
/// </summary>
internal sealed class DefectDetailComponent
{
    private readonly Blade _blade;
    private readonly IReadOnlyList<Anomaly> _anomalies;

    private static readonly string NavyHex = "#0D1B2A";
    private static readonly string WhiteHex = "#FFFFFF";
    private static readonly string StripeHex = "#F4F7FA";

    public DefectDetailComponent(Blade blade)
    {
        _blade = blade;
        _anomalies = blade.Anomalies
            .OrderByDescending(a => a.Severity)
            .ThenBy(a => a.RadiusMeters)
            .ToList();
    }

    public void Compose(IContainer container)
    {
        if (!_anomalies.Any())
        {
            container.Padding(20)
                .Text("No anomalies recorded for this blade.")
                .FontSize(11).FontColor("#6C7A89").Italic();
            return;
        }

        container.Table(table =>
        {
            // Column widths
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28);  // #
                cols.RelativeColumn(2.5f); // Defect Type
                cols.RelativeColumn(1.5f); // Classification
                cols.ConstantColumn(52);  // Severity
                cols.ConstantColumn(40);  // Side
                cols.ConstantColumn(58);  // Radius
                cols.ConstantColumn(55);  // Area
                cols.RelativeColumn(2f);  // Recommendation
            });

            // ── Header row ────────────────────────────────────────────────────────
            void HeaderCell(string text, float paddingH = 6f)
            {
                table.Cell().Background(NavyHex).PaddingVertical(7).PaddingHorizontal(paddingH)
                    .Text(text).FontSize(8).FontColor(WhiteHex).Bold();
            }

            HeaderCell("#");
            HeaderCell("Defect Type");
            HeaderCell("Classification");
            HeaderCell("Severity");
            HeaderCell("Side");
            HeaderCell("Radius (m)");
            HeaderCell("Area (cm²)");
            HeaderCell("Recommendation");

            // ── Data rows ─────────────────────────────────────────────────────────
            for (int i = 0; i < _anomalies.Count; i++)
            {
                var anomaly = _anomalies[i];
                string rowBg = (i % 2 == 0) ? WhiteHex : StripeHex;

                void DataCell(string text, bool bold = false)
                {
                    var cell = table.Cell().Background(rowBg).PaddingVertical(6).PaddingHorizontal(5);
                    var t = cell.Text(text).FontSize(8).FontColor(NavyHex);
                    if (bold) t.Bold();
                }

                // # column
                DataCell((i + 1).ToString(), bold: true);

                // Defect type (uses the smart display method that prefers Classification)
                DataCell(anomaly.GetDefectTypeDisplay(), bold: true);

                // Full classification path (if available)
                string classPath = anomaly.Classification?.GetFullPath() ?? anomaly.Type;
                DataCell(TruncateText(classPath, 35));

                // Severity badge (coloured cell)
                string sevColour = ExecutiveSummaryComponent.GetSeverityColour(anomaly.Severity);
                string sevLabel = ExecutiveSummaryComponent.GetConditionLabel(anomaly.Severity);
                table.Cell().Background(rowBg).Padding(4).AlignCenter()
                    .Background(sevColour).Padding(4)
                    .Text($"{anomaly.Severity} – {sevLabel}")
                    .FontSize(7).FontColor(WhiteHex).Bold();

                // Blade side
                DataCell(anomaly.BladeSide);

                // Radius
                DataCell(anomaly.RadiusMeters > 0 ? $"{anomaly.RadiusMeters:F1}" : "—");

                // Area
                DataCell(anomaly.AreaCm2 > 0 ? $"{anomaly.AreaCm2:F2}" : "—");

                // Recommendation
                DataCell(string.IsNullOrWhiteSpace(anomaly.Recommendation) ? "Monitor" : TruncateText(anomaly.Recommendation, 60));
            }
        });
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
