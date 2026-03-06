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
            col.Item().PaddingBottom(20).Table(table =>
            {
                table.ColumnsDefinition(cols => {
                    cols.ConstantColumn(80); // Severity
                    foreach (var b in _project.Blades) cols.RelativeColumn();
                });

                // Headers
                table.Cell().BorderBottom(1).Padding(5).Text("Severity").Bold();
                foreach (var b in _project.Blades)
                    table.Cell().BorderBottom(1).Padding(5).Text($"Blade {b.SerialNumber}").Bold();

                // Data Rows
                for (int sev = 1; sev <= 5; sev++)
                {
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(sev.ToString()).Bold();
                    foreach (var b in _project.Blades)
                    {
                        int count = b.Anomalies?.Count(a => a.Severity == sev) ?? 0;
                        table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(count > 0 ? count.ToString() : "0");
                    }
                }

                // Totals
                table.Cell().Padding(5).Text("Total").Bold();
                foreach (var b in _project.Blades)
                {
                    int total = b.Anomalies?.Count ?? 0;
                    table.Cell().Padding(5).Text(total.ToString()).Bold();
                }
            });

            col.Item().PaddingTop(30).Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.ConstantColumn(60); cols.ConstantColumn(100); cols.RelativeColumn(); });

                table.Cell().BorderBottom(1).Padding(5).Text("Severity").Bold();
                table.Cell().BorderBottom(1).Padding(5).Text("");
                table.Cell().BorderBottom(1).Padding(5).Text("Description").Bold();

                void SevRow(string sev, string title, string desc)
                {
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(sev).Bold();
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(title).Bold();
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5).Text(desc);
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