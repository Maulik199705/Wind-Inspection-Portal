using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class InspectionDetailsComponent
{
    private readonly InspectionProject _project;
    public InspectionDetailsComponent(InspectionProject project) => _project = project;

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(20)
                .Text("Inspection Details")
                .FontSize(14).Bold().FontColor("#4CAF50"); // Green for secondary heading

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });

                table.Cell().BorderBottom(1).Padding(5)
                    .Text("Turbine").Bold().FontColor("#000000"); // Black and bold
                table.Cell().BorderBottom(1).Padding(5)
                    .Text("Details").Bold().FontColor("#000000"); // Black and bold

                void Row(string lbl, string val)
                {
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                        .Text(lbl).FontColor("#000000");
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                        .Text(val).FontColor("#000000");
                }

                Row("Wind Farm Name", _project.ParkName);
                Row("WTG Number", _project.TurbineId);
                Row("WTG Model", _project.Model);
                Row("Customer", string.IsNullOrWhiteSpace(_project.Client) ? "Not Specified" : _project.Client);
                Row("Location", string.IsNullOrWhiteSpace(_project.Location) ? "Not Specified" : _project.Location);
                Row("Report Number", $"IV-{_project.InspectionDate:ddMMyy}-004");
                Row("Inspection Date", _project.InspectionDate.ToString("dd MMMM yyyy"));
                Row("Report date", DateTime.Now.ToString("dd MMMM yyyy"));
            });
        });
    }
}