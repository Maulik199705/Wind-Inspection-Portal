using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;
using System.Collections.Generic;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class ContentsPageComponent
{
    private readonly InspectionProject _project;
    private readonly Dictionary<string, int> _pageNumbers;

    public ContentsPageComponent(InspectionProject project, Dictionary<string, int> pageNumbers)
    {
        _project = project;
        _pageNumbers = pageNumbers;
    }

    public void Compose(IContainer container)
    {
        container.PaddingTop(20).Column(col =>
        {
            col.Item().PaddingBottom(15).Text("Table of Contents")
                .FontSize(18).Bold().FontColor("#000000");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(40);
                    cols.RelativeColumn();
                    cols.ConstantColumn(60);
                });

                // Introduction Section
                table.Cell().Text("01").FontSize(14).Bold();
                table.Cell().Hyperlink("IntroSection")
                    .Text("Introduction").FontSize(14).FontColor("#000000");
                table.Cell().AlignRight()
                    .Text(_pageNumbers.GetValueOrDefault("IntroSection", 3).ToString())
                    .FontSize(14).FontColor("#333333");

                int sectionNum = 2;

                foreach (var blade in _project.Blades)
                {
                    // Blade Section Header
                    table.Cell().PaddingTop(15).Text(sectionNum.ToString("D2")).FontSize(14).Bold();
                    table.Cell().PaddingTop(15).Text($"Blade {blade.SerialNumber}")
                        .FontSize(14).Bold().FontColor("#000000");
                    table.Cell().PaddingTop(15).Text("");

                    // Blade Overview
                    table.Cell().PaddingLeft(20).Text("");
                    table.Cell().PaddingTop(5).PaddingLeft(10)
                        .Hyperlink($"BladeOverview_{blade.SerialNumber}")
                        .Text($"Blade {blade.SerialNumber} Overview")
                        .FontSize(12).FontColor("#333333");
                    table.Cell().PaddingTop(5).AlignRight()
                        .Text(_pageNumbers.GetValueOrDefault($"BladeOverview_{blade.SerialNumber}", 0).ToString())
                        .FontSize(12).FontColor("#333333");

                    // Blade Annotation Details
                    table.Cell().PaddingLeft(20).Text("");

                    if (blade.Anomalies != null && blade.Anomalies.Count > 0)
                    {
                        table.Cell().PaddingTop(5).PaddingLeft(10)
                            .Hyperlink($"BladeDetails_{blade.SerialNumber}")
                            .Text($"Blade {blade.SerialNumber} Annotation Details")
                            .FontSize(12).FontColor("#333333");
                        table.Cell().PaddingTop(5).AlignRight()
                            .Text(_pageNumbers.GetValueOrDefault($"BladeDetails_{blade.SerialNumber}", 0).ToString())
                            .FontSize(12).FontColor("#333333");
                    }
                    else
                    {
                        table.Cell().PaddingTop(5).PaddingLeft(10)
                            .Text($"Blade {blade.SerialNumber} Annotation Details")
                            .FontSize(12).FontColor("#999999").Italic();
                        table.Cell().PaddingTop(5).AlignRight()
                            .Text("-")
                            .FontSize(12).FontColor("#999999");
                    }

                    sectionNum++;
                }
            });

            col.Item().PaddingTop(20)
                .Text("Click on any section title to navigate directly to that page.")
                .FontSize(9).FontColor("#666666").Italic();
        });
    }
}