using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

/// <summary>
/// Renders the executive summary page: key statistics and per-blade condition overview table.
/// </summary>
internal sealed class ExecutiveSummaryComponent
{
    private readonly InspectionProject _project;

    private static readonly string NavyHex = "#0D1B2A";
    private static readonly string TealHex = "#00A896";
    private static readonly string LightGrayHex = "#E8EDF2";
    private static readonly string WhiteHex = "#FFFFFF";

    public ExecutiveSummaryComponent(InspectionProject project)
    {
        _project = project;
    }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            // ── Section header ──────────────────────────────────────────────────────
            col.Item().Background(NavyHex).Padding(14).Row(row =>
            {
                row.RelativeItem()
                    .Text("Executive Summary")
                    .FontSize(16).FontColor(WhiteHex).Bold();
            });

            col.Item().Height(2).Background(TealHex);

            col.Item().Padding(20).Column(inner =>
            {
                // ── Key stats row ──────────────────────────────────────────────────
                inner.Item().PaddingBottom(20).Row(statsRow =>
                {
                    StatCard(statsRow.RelativeItem(), "Total Blades", _project.Blades.Count.ToString(), TealHex);
                    statsRow.ConstantItem(10);
                    StatCard(statsRow.RelativeItem(), "Total Anomalies", _project.TotalAnomalies.ToString(), GetSeverityColour(_project.OverallCondition));
                    statsRow.ConstantItem(10);
                    StatCard(statsRow.RelativeItem(), "Worst Severity", $"{_project.OverallCondition} / 5", GetSeverityColour(_project.OverallCondition));
                    statsRow.ConstantItem(10);
                    StatCard(statsRow.RelativeItem(), "Inspection Date", _project.InspectionDate.ToString("dd MMM yyyy"), NavyHex);
                });

                // ── Blade condition table ──────────────────────────────────────────
                inner.Item().PaddingBottom(8)
                    .Text("Blade Condition Overview")
                    .FontSize(13).Bold().FontColor("#000000"); // Black and bold

                inner.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(60);   // Blade
                        cols.ConstantColumn(80);   // Serial
                        cols.RelativeColumn();     // Anomalies  
                        cols.RelativeColumn();     // Worst Severity
                        cols.RelativeColumn();     // Status
                    });

                    // Header
                    void HeaderCell(string text) =>
                        table.Cell().Background("#F5F5F5").Padding(8)
                            .Text(text).FontSize(10).FontColor("#000000").Bold(); // Black and bold

                    HeaderCell("Blade");
                    HeaderCell("Serial");
                    HeaderCell("Anomalies");
                    HeaderCell("Worst Severity");
                    HeaderCell("Condition");

                    bool alt = false;
                    foreach (var blade in _project.Blades)
                    {
                        string bg = alt ? LightGrayHex : WhiteHex;
                        alt = !alt;

                        void DataCell(string text, bool bold = false)
                        {
                            var cell = table.Cell().Background(bg).Padding(8);
                            var t = cell.Text(text).FontSize(10).FontColor(NavyHex);
                            if (bold) t.Bold();
                        }

                        DataCell($"Blade {blade.SerialNumber}", bold: true);
                        DataCell(blade.SerialNumber);
                        DataCell(blade.Anomalies.Count.ToString());

                        // Severity badge cell
                        string sevColour = GetSeverityColour(blade.Condition);
                        table.Cell().Background(bg).Padding(5)
                            .AlignCenter()
                            .Background(sevColour)
                            .Padding(4)
                            .Text($"  {blade.Condition}  ")
                            .FontSize(10).FontColor(WhiteHex).Bold();

                        DataCell(GetConditionLabel(blade.Condition));
                    }
                });

                // ── Severity legend ────────────────────────────────────────────────
                inner.Item().PaddingTop(20).PaddingBottom(8)
                    .Text("Severity Reference")
                    .FontSize(11).FontColor(NavyHex).Bold();

                inner.Item().Row(legRow =>
                {
                    for (int sev = 1; sev <= 5; sev++)
                    {
                        LegendItem(legRow.RelativeItem(), sev);
                        if (sev < 5) legRow.ConstantItem(6);
                    }
                });
            });
        });
    }

    private static void StatCard(IContainer container, string label, string value, string accentColour)
    {
        container
            .Border(1).BorderColor("#D0D8E0")
            .Column(c =>
            {
                c.Item().Background(accentColour).Padding(6)
                    .AlignCenter()
                    .Text(label).FontSize(9).FontColor(WhiteHex).Bold();
                c.Item().Padding(10).AlignCenter()
                    .Text(value).FontSize(20).FontColor(accentColour).Bold();
            });
    }

    private static void LegendItem(IContainer container, int severity)
    {
        string colour = GetSeverityColour(severity);
        string label = GetConditionLabel(severity);

        container.Column(c =>
        {
            c.Item().Background(colour).Padding(6).AlignCenter()
                .Text($"{severity}").FontSize(12).FontColor("#FFFFFF").Bold();
            c.Item().PaddingTop(3).AlignCenter()
                .Text(label).FontSize(8).FontColor("#6C7A89");
        });
    }

    internal static string GetSeverityColour(int severity) => severity switch
    {
        1 => "#4CAF50",
        2 => "#8BC34A",
        3 => "#FF9800",
        4 => "#F44336",
        5 => "#B71C1C",
        _ => "#9E9E9E"
    };

    internal static string GetConditionLabel(int severity) => severity switch
    {
        1 => "Cosmetic",
        2 => "Minor",
        3 => "Medium",
        4 => "Serious",
        5 => "Very Serious",
        _ => "Unknown"
    };
}
