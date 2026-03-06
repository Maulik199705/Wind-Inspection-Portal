using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class ContentsPageComponent
{
    private readonly InspectionProject _project;
    public ContentsPageComponent(InspectionProject project) => _project = project;

    public void Compose(IContainer container)
    {
        container.PaddingTop(20).Table(table =>
        {
            table.ColumnsDefinition(cols => { cols.ConstantColumn(200); cols.RelativeColumn(); });

            table.Cell().Text("Introduction").FontSize(14).Bold();
            table.Cell().Text("01").FontSize(14);

            int page = 4; // Simulating page numbers based on the flow
            foreach (var blade in _project.Blades)
            {
                table.Cell().PaddingTop(15).Text($"Blade {blade.SerialNumber}").FontSize(14).Bold();
                table.Cell().PaddingTop(15).Text("");

                table.Cell().PaddingTop(5).Text($"Blade {blade.SerialNumber} Overview").FontSize(12);
                table.Cell().PaddingTop(5).Text(page.ToString("D2")).FontSize(12);
                page++;

                table.Cell().PaddingTop(5).Text($"Blade {blade.SerialNumber} Annotation Details").FontSize(12);
                table.Cell().PaddingTop(5).Text(page.ToString("D2")).FontSize(12);
                page += blade.Anomalies?.Count ?? 0;
            }
        });
    }
}