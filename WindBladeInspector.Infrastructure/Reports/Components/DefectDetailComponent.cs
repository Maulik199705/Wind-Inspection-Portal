//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using WindBladeInspector.Core.Entities;

//namespace WindBladeInspector.Infrastructure.Reports.Components;

///// <summary>
///// Renders the per-blade defect detail table with all anomaly data rows.
///// Each row: #, Defect Type, Classification Path, Severity badge, Side, Radius, Area, Recommendation.
///// </summary>
//internal sealed class DefectDetailComponent
//{
//    private readonly Blade _blade;
//    private readonly IReadOnlyList<Anomaly> _anomalies;

//    private static readonly string NavyHex = "#0D1B2A";
//    private static readonly string WhiteHex = "#FFFFFF";
//    private static readonly string StripeHex = "#F4F7FA";

//    public DefectDetailComponent(Blade blade)
//    {
//        _blade = blade;
//        _anomalies = blade.Anomalies
//            .OrderByDescending(a => a.Severity)
//            .ThenBy(a => a.RadiusMeters)
//            .ToList();
//    }

//    public void Compose(IContainer container)
//    {
//        if (!_anomalies.Any())
//        {
//            container.Padding(20)
//                .Text("No anomalies recorded for this blade.")
//                .FontSize(11).FontColor("#6C7A89").Italic();
//            return;
//        }

//        container.Table(table =>
//        {
//            // Column widths
//            table.ColumnsDefinition(cols =>
//            {
//                cols.ConstantColumn(28);  // #
//                cols.RelativeColumn(2.5f); // Defect Type
//                cols.RelativeColumn(1.5f); // Classification
//                cols.ConstantColumn(52);  // Severity
//                cols.ConstantColumn(40);  // Side
//                cols.ConstantColumn(58);  // Radius
//                cols.ConstantColumn(55);  // Area
//                cols.RelativeColumn(2f);  // Recommendation
//            });

//            // ── Header row ────────────────────────────────────────────────────────
//            void HeaderCell(string text, float paddingH = 6f)
//            {
//                table.Cell().Background(NavyHex).PaddingVertical(7).PaddingHorizontal(paddingH)
//                    .Text(text).FontSize(8).FontColor(WhiteHex).Bold();
//            }

//            HeaderCell("#");
//            HeaderCell("Defect Type");
//            HeaderCell("Classification");
//            HeaderCell("Severity");
//            HeaderCell("Side");
//            HeaderCell("Radius (m)");
//            HeaderCell("Area (cm²)");
//            HeaderCell("Recommendation");

//            // ── Data rows ─────────────────────────────────────────────────────────
//            for (int i = 0; i < _anomalies.Count; i++)
//            {
//                var anomaly = _anomalies[i];
//                string rowBg = (i % 2 == 0) ? WhiteHex : StripeHex;

//                void DataCell(string text, bool bold = false)
//                {
//                    var cell = table.Cell().Background(rowBg).PaddingVertical(6).PaddingHorizontal(5);
//                    var t = cell.Text(text).FontSize(8).FontColor(NavyHex);
//                    if (bold) t.Bold();
//                }

//                // # column
//                DataCell((i + 1).ToString(), bold: true);

//                // Defect type (uses the smart display method that prefers Classification)
//                DataCell(anomaly.GetDefectTypeDisplay(), bold: true);

//                // Full classification path (if available)
//                string classPath = anomaly.Classification?.GetFullPath() ?? anomaly.Type;
//                DataCell(TruncateText(classPath, 35));

//                // Severity badge (coloured cell)
//                string sevColour = ExecutiveSummaryComponent.GetSeverityColour(anomaly.Severity);
//                string sevLabel = ExecutiveSummaryComponent.GetConditionLabel(anomaly.Severity);
//                table.Cell().Background(rowBg).Padding(4).AlignCenter()
//                    .Background(sevColour).Padding(4)
//                    .Text($"{anomaly.Severity} – {sevLabel}")
//                    .FontSize(7).FontColor(WhiteHex).Bold();

//                // Blade side
//                DataCell(anomaly.BladeSide);

//                // Radius
//                DataCell(anomaly.RadiusMeters > 0 ? $"{anomaly.RadiusMeters:F1}" : "—");

//                // Area
//                DataCell(anomaly.AreaCm2 > 0 ? $"{anomaly.AreaCm2:F2}" : "—");

//                // Recommendation
//                DataCell(string.IsNullOrWhiteSpace(anomaly.Recommendation) ? "Monitor" : TruncateText(anomaly.Recommendation, 60));
//            }
//        });
//    }

//    private static string TruncateText(string text, int maxLength)
//    {
//        if (string.IsNullOrEmpty(text)) return string.Empty;
//        return text.Length <= maxLength ? text : text[..maxLength] + "…";
//    }
//}

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using WindBladeInspector.Core.Entities;
using static WindBladeInspector.Infrastructure.Reports.PdfReportGenerationService;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class DefectDetailComponent
{
    private readonly string _bladeSerial;
    private readonly DefectEntry _defect;
    private readonly byte[]? _imageBytes;

    public DefectDetailComponent(string bladeSerial, DefectEntry defect, byte[]? imageBytes)
    {
        _bladeSerial = bladeSerial;
        _defect = defect;
        _imageBytes = imageBytes;
    }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(5).Text($"Blade {_bladeSerial}").FontSize(14).Bold().FontColor("#333333");
            col.Item().PaddingBottom(15).Text($"Damage {_defect.DefectNo}").FontSize(12).Bold().FontColor("#333333");

            // ── DYNAMIC IMAGE CONTAINER ──
            // Notice: No fixed Height(). FitWidth() forces the image to perfectly
            // span the page margins, naturally adjusting its height with zero blank space.
            col.Item().PaddingBottom(20).Element(c =>
            {
                if (_imageBytes != null)
                {
                    byte[] croppedBytes = CropAndAnnotate(_imageBytes, _defect.Anomaly) ?? _imageBytes;
                    c.Image(croppedBytes).FitWidth();
                }
                else
                {
                    c.Height(200).Background("#EEEEEE").AlignCenter().AlignMiddle().Text("Image Not Available").Italic();
                }
            });

            // ── MINIMALIST TABLE ──
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.ConstantColumn(160); cols.RelativeColumn(); });

                void Row(string lbl, string val)
                {
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").PaddingVertical(8).Text(lbl).Bold().FontSize(11).FontColor("#333333");
                    table.Cell().BorderBottom(1).BorderColor("#EEEEEE").PaddingVertical(8).Text(string.IsNullOrWhiteSpace(val) ? "None" : val).FontSize(11).FontColor("#555555");
                }

                Row("Blade", _bladeSerial);
                Row("Side", GetFullViewLabel(_defect.View));
                Row("Material", "Auxiliary Component");
                Row("Type", _defect.Anomaly.GetDefectTypeDisplay() ?? "Leading Edge Protection");
                Row("Subtype", _defect.Anomaly.Recommendation ?? "None");
                Row("Severity", _defect.Anomaly.Severity.ToString());
            });
        });
    }

    // ── PRECISION ZOOM MATH (NO BLACK BORDERS) ──
    private byte[]? CropAndAnnotate(byte[] imageBytes, Anomaly anomaly)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap == null) return null;

        var coords = anomaly.Coordinates;
        if (coords == null || coords.ReferenceWidth <= 0 || coords.ReferenceHeight <= 0)
            return imageBytes;

        float scaleX = (float)bitmap.Width / (float)coords.ReferenceWidth;
        float scaleY = (float)bitmap.Height / (float)coords.ReferenceHeight;

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

        if (coords.IsPolygon && coords.PolygonPoints.Count >= 8)
        {
            for (int i = 0; i < 4; i++)
            {
                float px = (float)coords.PolygonPoints[i * 2] * scaleX;
                float py = (float)coords.PolygonPoints[i * 2 + 1] * scaleY;
                minX = Math.Min(minX, px); minY = Math.Min(minY, py);
                maxX = Math.Max(maxX, px); maxY = Math.Max(maxY, py);
            }
        }
        else
        {
            minX = (float)coords.X * scaleX;
            minY = (float)coords.Y * scaleY;
            maxX = minX + (float)coords.Width * scaleX;
            maxY = minY + (float)coords.Height * scaleY;
        }

        float defWidth = Math.Max(1f, maxX - minX);
        float defHeight = Math.Max(1f, maxY - minY);

        // Calculate dynamic padding to keep context
        float paddingX = Math.Max(defWidth * 1.5f, 150f);
        float paddingY = Math.Max(defHeight * 1.5f, 150f);

        // EXACT integer clamping strictly within the boundaries of the original image
        // This prevents Skia from trying to read out-of-bounds pixels (which turn black)
        int cropX = (int)Math.Max(0, minX - paddingX);
        int cropY = (int)Math.Max(0, minY - paddingY);
        int cropRight = (int)Math.Min(bitmap.Width, maxX + paddingX);
        int cropBottom = (int)Math.Min(bitmap.Height, maxY + paddingY);

        int cropW = cropRight - cropX;
        int cropH = cropBottom - cropY;

        if (cropW <= 0 || cropH <= 0) return imageBytes;

        var info = new SKImageInfo(cropW, cropH);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // Clear the canvas to pure white before drawing to ensure zero black edge bleeding
        canvas.Clear(SKColors.White);

        var sourceRect = SKRect.Create(cropX, cropY, cropW, cropH);
        var destRect = SKRect.Create(0, 0, cropW, cropH);
        canvas.DrawBitmap(bitmap, sourceRect, destRect);

        // Draw the cleanly padded Red Bounding Box
        using var strokePaint = new SKPaint
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(2.5f, cropW / 250f),
            IsAntialias = true
        };

        var boxRect = SKRect.Create(minX - cropX, minY - cropY, defWidth, defHeight);
        boxRect.Inflate(12, 12);

        // Failsafe: Ensure the red box itself doesn't draw outside our cropped boundaries
        boxRect.Intersect(SKRect.Create(5, 5, cropW - 10, cropH - 10));

        canvas.DrawRect(boxRect, strokePaint);

        canvas.Flush();
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 100);

        return data.ToArray();
    }

    private static string GetFullViewLabel(string side) => side switch
    {
        "PS" => "Pressure Side",
        "SS" => "Suction Side",
        "LE" => "Leading Edge",
        "TE" => "Trailing Edge",
        _ => side
    };
}