//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using WindBladeInspector.Core.Entities;

//namespace WindBladeInspector.Infrastructure.Reports.Components;

///// <summary>
///// Renders the full-bleed cover page content into an IContainer.
///// The page wrapping (margins, size) is handled by the orchestrator.
///// </summary>
//internal sealed class CoverPageComponent
//{
//    private readonly InspectionProject _project;
//    private readonly byte[] _logoBytes;
//    private readonly byte[]? _coverImageBytes;

//    private const string NavyHex = "#0D1B2A";
//    private const string TealHex = "#00A896";
//    private const string WhiteHex = "#FFFFFF";
//    private const string LightGrayHex = "#E8EDF2";

//    public CoverPageComponent(InspectionProject project, byte[] logoBytes, byte[]? coverImageBytes)
//    {
//        _project = project;
//        _logoBytes = logoBytes;
//        _coverImageBytes = coverImageBytes;
//    }

//    public void Compose(IContainer container)
//    {
//        container.Column(col =>
//        {
//            // ── Full-bleed photo area ──────────────────────────────────────────
//            col.Item().Height(340).Layers(layers =>
//            {
//                // Background: cover photo or solid navy (PRIMARY LAYER - REQUIRED!)
//                if (_coverImageBytes != null && _coverImageBytes.Length > 0)
//                {
//                    layers.PrimaryLayer().Image(_coverImageBytes).FitArea();
//                }
//                else
//                {
//                    layers.PrimaryLayer().Background(NavyHex);
//                }

//                // Semi-transparent navy overlay so text is always readable
//                layers.Layer().Background($"{NavyHex}CC");

//                // Logo + company name top-left
//                layers.Layer().Padding(30).Column(c =>
//                {
//                    c.Item().Row(row =>
//                    {
//                        row.ConstantItem(90).Image(_logoBytes).FitArea();
//                        row.RelativeItem().PaddingLeft(12).AlignMiddle()
//                            .Text("QualiMax")
//                            .FontSize(28).FontColor(WhiteHex).Bold();
//                    });
//                });

//                // Centre title block
//                layers.Layer().AlignCenter().AlignMiddle().Column(c =>
//                {
//                    c.Item().AlignCenter()
//                        .Text("Wind Turbine Blade")
//                        .FontSize(26).FontColor(WhiteHex).Bold();

//                    c.Item().AlignCenter()
//                        .Text("Inspection Report")
//                        .FontSize(26).FontColor(TealHex).Bold();

//                    c.Item().PaddingTop(10).AlignCenter()
//                        .Text($"Turbine: {_project.TurbineId}")
//                        .FontSize(15).FontColor(LightGrayHex);
//                });
//            });

//            // ── Teal accent strip ──────────────────────────────────────────────
//            col.Item().Height(6).Background(TealHex);

//            // ── Project details card ───────────────────────────────────────────
//            col.Item().Padding(40).Column(details =>
//            {
//                details.Item().PaddingBottom(20)
//                    .Text("Inspection Details")
//                    .FontSize(18).FontColor(NavyHex).Bold();

//                details.Item().Table(table =>
//                {
//                    table.ColumnsDefinition(cols =>
//                    {
//                        cols.ConstantColumn(160);
//                        cols.RelativeColumn();
//                    });

//                    void Row(string label, string value)
//                    {
//                        table.Cell().Padding(8).Background(LightGrayHex)
//                            .Text(label).FontSize(11).FontColor(NavyHex).Bold();
//                        table.Cell().Padding(8)
//                            .Text(value).FontSize(11).FontColor(NavyHex);
//                    }

//                    Row("Wind Park / Farm", _project.ParkName);
//                    Row("Turbine ID", _project.TurbineId);
//                    Row("Model", _project.Model);
//                    Row("Inspection Date", _project.InspectionDate.ToString("dd MMMM yyyy"));
//                    Row("Total Anomalies", _project.TotalAnomalies.ToString());
//                    Row("Overall Condition", $"Severity {_project.OverallCondition} / 5");
//                    Row("Data Capture Status", _project.DataCaptureStatus);
//                    Row("Analysis Status", _project.AnalysisStatus);
//                });
//            });

//            // ── Spacer ─────────────────────────────────────────────────────────
//            col.Item().Extend();

//            // ── Bottom footer band ─────────────────────────────────────────────
//            col.Item().Height(50).Background(NavyHex)
//                .Padding(15)
//                .Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}  |  Confidential")
//                .FontSize(9).FontColor($"{WhiteHex}99").AlignCenter();
//        });
//    }
//}



using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class CoverPageComponent
{
    private readonly InspectionProject _project;
    public CoverPageComponent(InspectionProject project) => _project = project;

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

                Row("CLIENT", "ALERION"); // Defaulting based on sample, update via Project entity if available
                Row("PARK", _project.ParkName.ToUpper());
                Row("TURBINE NUMBER", _project.TurbineId);
                Row("MODEL", _project.Model.ToUpper());
                Row("INSPECTION DATE", _project.InspectionDate.ToString("dd MMM yyyy").ToUpper());
            });
        });
    }
}
