
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class CoverPageComponent
{
    private readonly InspectionProject _project;
    private readonly byte[]? _logoBytes;

    public CoverPageComponent(InspectionProject project, byte[]? logoBytes)
    {
        _project = project;
        _logoBytes = logoBytes;
    }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(100).Text("QUALIMAX\nSERVICES").FontSize(28).Bold();
            col.Item().PaddingTop(20).Text("DETAILED WIND TURBINE\nBLADE INSPECTION REPORT").FontSize(22).Bold();
            col.Item().PaddingTop(20).Text("2026").FontSize(16).Bold();

            col.Item().PaddingTop(100).Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.ConstantColumn(150); cols.RelativeColumn(); });

                void Row(string label, string value)
                {
                    table.Cell().PaddingBottom(8).Text($"• {label}").FontSize(12).Bold();
                    table.Cell().PaddingBottom(8).Text(value).FontSize(12);
                }

                // Change the Row for CLIENT to use the dynamic field
                Row("CLIENT", string.IsNullOrWhiteSpace(_project.Client) ? "NOT SPECIFIED" : _project.Client.ToUpper());
                Row("PARK", _project.ParkName.ToUpper());
                Row("TURBINE NUMBER", _project.TurbineId);
                Row("MODEL", _project.Model.ToUpper());
                Row("INSPECTION DATE", _project.InspectionDate.ToString("dd MMM yyyy").ToUpper());
            });
        });
    }
}
