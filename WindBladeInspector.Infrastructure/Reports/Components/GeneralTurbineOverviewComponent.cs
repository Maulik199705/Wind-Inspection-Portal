using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class GeneralTurbineOverviewComponent
{
    private readonly InspectionProject _project;

    public GeneralTurbineOverviewComponent(InspectionProject project)
    {
        _project = project;
    }

    private static string SeverityBackground(int severity) => severity switch
    {
        1 => "#B7E35D",
        2 => "#2DCC70",
        3 => "#F2D34F",
        4 => "#F79245",
        5 => "#FF3131",
        _ => "#DDDDDD"
    };

    private static string SeverityTextColor(int severity) => severity switch
    {
        1 => "#000000",
        2 => "#000000",
        3 => "#000000",
        4 => "#000000",
        5 => "#FFFFFF",
        _ => "#000000"
    };

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            // ── TOP SUMMARY TABLE ─────────────────────────────────────────
            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(120); // Severity column wider like sample
                    foreach (var blade in _project.Blades)
                        cols.RelativeColumn();
                });

                static IContainer HeaderCell(IContainer c) =>
                    c.BorderBottom(2)
                     .BorderColor(Colors.Black)
                     .PaddingVertical(10)
                     .PaddingHorizontal(8)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer BodyCell(IContainer c) =>
                    c.BorderBottom(1)
                     .BorderColor("#BDBDBD")
                     .PaddingVertical(12)
                     .PaddingHorizontal(8)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer SeverityCell(IContainer c, string bgColor) =>
                    c.BorderBottom(1)
                     .BorderRight(1)
                     .BorderColor("#BDBDBD")
                     .Background(bgColor)
                     .PaddingVertical(12)
                     .PaddingHorizontal(8)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer TotalCell(IContainer c) =>
                    c.BorderTop(2)
                     .BorderColor(Colors.Black)
                     .PaddingVertical(12)
                     .PaddingHorizontal(8)
                     .AlignCenter()
                     .AlignMiddle();

                table.Cell().Element(HeaderCell)
                    .AlignLeft()
                    .Text("Severity")
                    .Bold()
                    .FontSize(10)
                    .FontColor("#666666");

                foreach (var blade in _project.Blades)
                {
                    table.Cell().Element(HeaderCell)
                        .Text($"Blade {blade.SerialNumber}")
                        .Bold()
                        .FontSize(10)
                        .FontColor("#000000");
                }

                for (int sev = 1; sev <= 5; sev++)
                {
                    table.Cell()
                        .Element(c => SeverityCell(c, SeverityBackground(sev)))
                        .Text(sev.ToString())
                        .Bold()
                        .FontSize(11)
                        .FontColor(SeverityTextColor(sev));

                    foreach (var blade in _project.Blades)
                    {
                        int count = blade.Anomalies?.Count(a => a.Severity == sev) ?? 0;

                        table.Cell()
                            .Element(BodyCell)
                            .Text(count.ToString())
                            .FontSize(10)
                            .FontColor("#333333");
                    }
                }

                table.Cell().Element(TotalCell)
                    .AlignLeft()
                    .Text("Total")
                    .Bold()
                    .FontSize(10)
                    .FontColor("#666666");

                foreach (var blade in _project.Blades)
                {
                    int total = blade.Anomalies?.Count ?? 0;

                    table.Cell().Element(TotalCell)
                        .Text(total.ToString())
                        .Bold()
                        .FontSize(10)
                        .FontColor("#333333");
                }
            });

            // ── GAP BETWEEN TABLES ────────────────────────────────────────
            col.Item().PaddingTop(28);

            // ── BOTTOM LEGEND TABLE ───────────────────────────────────────
            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(90);   // Severity
                    cols.ConstantColumn(180);  // Type
                    cols.RelativeColumn();     // Description
                });

                static IContainer LegendHeaderLeft(IContainer c) =>
                    c.Border(2)
                     .BorderColor(Colors.Black)
                     .Padding(10)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer LegendHeaderRight(IContainer c) =>
                    c.BorderTop(2)
                     .BorderBottom(2)
                     .BorderRight(2)
                     .BorderColor(Colors.Black)
                     .Padding(10)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer LegendSeverityCell(IContainer c, string bgColor) =>
                    c.BorderLeft(1)
                     .BorderRight(1)
                     .BorderBottom(1)
                     .BorderColor("#BDBDBD")
                     .Background(bgColor)
                     .PaddingVertical(12)
                     .PaddingHorizontal(8)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer LegendTypeCell(IContainer c) =>
                    c.BorderRight(1)
                     .BorderBottom(1)
                     .BorderColor("#BDBDBD")
                     .Padding(10)
                     .AlignCenter()
                     .AlignMiddle();

                static IContainer LegendDescriptionCell(IContainer c) =>
                    c.BorderRight(1)
                     .BorderBottom(1)
                     .BorderColor("#BDBDBD")
                     .Padding(10)
                     .AlignLeft()
                     .AlignMiddle();

                table.Cell().Element(LegendHeaderLeft)
                    .Text("Severity")
                    .Bold()
                    .FontSize(10)
                    .FontColor("#666666");

                table.Cell().ColumnSpan(2).Element(LegendHeaderRight)
                    .Text("Description")
                    .Bold()
                    .FontSize(10)
                    .FontColor("#000000");

                void SevRow(int sev, string type, string desc)
                {
                    table.Cell()
                        .Element(c => LegendSeverityCell(c, SeverityBackground(sev)))
                        .Text(sev.ToString())
                        .Bold()
                        .FontSize(11)
                        .FontColor(SeverityTextColor(sev));

                    table.Cell()
                        .Element(LegendTypeCell)
                        .Text(type)
                        .Bold()
                        .FontSize(10)
                        .FontColor("#000000");

                    table.Cell()
                        .Element(LegendDescriptionCell)
                        .Text(desc)
                        .FontSize(10)
                        .FontColor("#333333");
                }

                SevRow(1, "Cosmetic", "No intervention or immediate action is required");
                SevRow(2, "Minor", "Repair only if other damages require repair");
                SevRow(3, "Medium", "Repair within 5-6 months.");
                SevRow(4, "Serious", "Repair within 2-3 months.");
                SevRow(5, "Critical", "Immediate corrective action is required to prevent further turbine damage.");
            });
        });
    }
}