using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

/// <summary>
/// Renders the page footer on every content page: project ID, turbine ID, and page number.
/// </summary>
internal sealed class ReportFooterComponent
{
    private readonly InspectionProject _project;

    private static readonly string NavyHex = "#0D1B2A";
    private static readonly string TealHex = "#00A896";

    public ReportFooterComponent(InspectionProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Call this inside a QuestPDF Page() footer slot.
    /// </summary>
    public void Compose(IContainer container)
    {
        container
            .BorderTop(1).BorderColor(TealHex)
            .PaddingTop(6)
            .Row(row =>
            {
                // Left: company + project info
                row.RelativeItem()
                    .Text(txt =>
                    {
                        txt.Span("QualiMax  |  ").FontSize(8).FontColor(NavyHex).Bold();
                        txt.Span($"Park: {_project.ParkName}  ·  Turbine: {_project.TurbineId}")
                            .FontSize(8).FontColor("#6C7A89");
                    });

                // Right: page number
                row.ConstantItem(60).AlignRight()
                    .Text(txt =>
                    {
                        txt.Span("Page ").FontSize(8).FontColor("#6C7A89");
                        txt.CurrentPageNumber().FontSize(8).FontColor(NavyHex).Bold();
                        txt.Span(" / ").FontSize(8).FontColor("#6C7A89");
                        txt.TotalPages().FontSize(8).FontColor(NavyHex).Bold();
                    });
            });
    }
}
