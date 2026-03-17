using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Linq;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class GeneralTurbineOverviewComponent
{
    private readonly InspectionProject _project;
    public GeneralTurbineOverviewComponent(InspectionProject project) => _project = project;

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            //col.Item().PaddingBottom(20)
            //    .AlignCenter() // Center the title
            //    .Text("General Turbine Overview")
            //    .FontSize(14).Bold().FontColor("#4CAF50"); // Green and bold

            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols => {
                    cols.ConstantColumn(80); // Severity
                    foreach (var b in _project.Blades) cols.RelativeColumn();
                });

                // Headers
                table.Cell().BorderBottom(1).Padding(5).Text("Severity").Bold().FontColor("#000000"); // Black and bold
                foreach (var b in _project.Blades)
                    table.Cell().BorderBottom(1).Padding(5).Text($"Blade {b.SerialNumber}").Bold().FontColor("#000000"); // Black and bold

                // Data Rows
                for (int sev = 1; sev <= 5; sev++)
                {
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(sev.ToString()).Bold().FontColor("#333333");
                    foreach (var b in _project.Blades)
                    {
                        int count = b.Anomalies?.Count(a => a.Severity == sev) ?? 0;
                        table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(count > 0 ? count.ToString() : "0").FontColor("#333333");
                    }
                }

                // Totals
                table.Cell().Padding(5).Text("Total").Bold().FontColor("#000000"); // Black and bold
                foreach (var b in _project.Blades)
                {
                    int total = b.Anomalies?.Count ?? 0;
                    table.Cell().Padding(5).Text(total.ToString()).Bold().FontColor("#000000"); // Black and bold
                }
            });

            col.Item().PaddingTop(30).AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(60); // Severity column
                    cols.ConstantColumn(100); // Title column
                    cols.RelativeColumn(); // Description column
                });

                // Header row
                table.Cell().BorderBottom(2).BorderColor("#000000").Padding(8)
                    .AlignCenter().Text("Severity").Bold().FontColor("#000000"); // Black and bold
                table.Cell().BorderBottom(2).BorderColor("#000000").Padding(8)
                    .AlignCenter().Text("Severity Type").Bold().FontColor("#000000"); // Black and bold
                table.Cell().BorderBottom(2).BorderColor("#000000").Padding(8)
                    .AlignCenter().Text("Description").Bold().FontColor("#000000"); // Black and bold

                // Data rows
                void SevRow(string sev, string title, string desc)
                {
                    table.Cell().BorderBottom(1).BorderColor("#CCCCCC").Padding(8)
                        .AlignCenter().Text(sev).Bold().FontColor("#333333");
                    table.Cell().BorderBottom(1).BorderColor("#CCCCCC").Padding(8)
                        .AlignCenter().Text(title).Bold().FontColor("#333333");
                    table.Cell().BorderBottom(1).BorderColor("#CCCCCC").Padding(8)
                        .AlignLeft().Text(desc).FontColor("#333333"); // Align description to the left
                }

                SevRow("1", "Cosmetic", "No intervention or immediate action is required");
                SevRow("2", "Minor", "Repair only if other damages require repair");
                SevRow("3", "Medium", "Repair within 5-6 months.");
                SevRow("4", "Serious", "Repair within 2-3 months.");
                SevRow("5", "Critical", "Immediate corrective action is required to prevent further turbine damage.");
            });
        });
    }
}