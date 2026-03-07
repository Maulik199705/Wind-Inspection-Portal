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
            table.ColumnsDefinition(cols => {
                cols.ConstantColumn(40);  // Numbering column (02, 03)
                cols.ConstantColumn(200); // Title column
                cols.RelativeColumn();    // Page number column
            });

            table.Cell().Text("").FontSize(14).Bold();
            table.Cell().Text("Introduction").FontSize(14).Bold();
            table.Cell().Text("01").FontSize(14);

            int page = 4; // Intro ends at 3, Blade 1 starts at 4
            int sectionNum = 2; // Introduction is 01, Blade A is 02

            foreach (var blade in _project.Blades)
            {
                // Main Blade Header (e.g., "02    Blade A")
                table.Cell().PaddingTop(15).Text(sectionNum.ToString("D2")).FontSize(14).Bold();
                table.Cell().PaddingTop(15).Text($"Blade {blade.SerialNumber}").FontSize(14).Bold();
                table.Cell().PaddingTop(15).Text("");

                // Blade Overview row
                table.Cell().Text("");
                table.Cell().PaddingTop(5).Text($"Blade {blade.SerialNumber} Overview").FontSize(12).FontColor("#333333");
                table.Cell().PaddingTop(5).Text(page.ToString("D2")).FontSize(12).FontColor("#333333");
                page++;

                // Blade Annotations row
                table.Cell().Text("");
                table.Cell().PaddingTop(5).Text($"Blade {blade.SerialNumber} Annotation Details").FontSize(12).FontColor("#333333");
                table.Cell().PaddingTop(5).Text(page.ToString("D2")).FontSize(12).FontColor("#333333");

                // Add pages for however many defects there are
                page += blade.Anomalies?.Count ?? 0;
                sectionNum++;
            }
        });
    }
}