using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.Diagnostics;
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
        // Ensure HeightCm is up-to-date from coordinates before rendering
        _defect.Anomaly?.UpdateHeightFromCoordinates();

        container.Column(col =>
        {
            col.Item().PaddingBottom(5)
                .AlignCenter() // Center the title
                .Text($"Blade {_bladeSerial}")
                .FontSize(14).Bold().FontColor("#4CAF50"); // Green and bold

            col.Item().PaddingBottom(15)
                .AlignCenter() // Center the subtitle
                .Text($"Damage {_defect.DefectNo}")
                .FontSize(12).Bold().FontColor("#4CAF50"); // Green for secondary heading

            // ── DYNAMIC IMAGE CONTAINER ──
            col.Item().PaddingBottom(20).AlignCenter().Element(c =>
            {
                if (_imageBytes != null)
                {
                    byte[] croppedBytes = CropAndAnnotate(_imageBytes, _defect.Anomaly) ?? _imageBytes;
                    c.MinHeight(200).MaxHeight(250).AlignCenter().AlignMiddle().Image(croppedBytes, ImageScaling.FitArea);
                }
                else
                {
                    c.Height(200).Background("#EEEEEE").AlignCenter().AlignMiddle().Text("Image Not Available").Italic();
                }
            });

            // ── CENTERED TABLE ──
            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(160); // Label column
                    cols.RelativeColumn();    // Value column
                });

                void Row(string lbl, string val)
                {
                    table.Cell().Border(2) // Increase border thickness
                        .BorderColor("#CCCCCC") // Slightly darker border color
                        .Padding(8)
                        .AlignCenter()
                        .Text(lbl).Bold().FontSize(10).FontColor("#000000"); // Black and bold for header
                    table.Cell().Border(2) // Increase border thickness
                        .BorderColor("#CCCCCC") // Slightly darker border color
                        .Padding(8)
                        .AlignCenter()
                        .Text(string.IsNullOrWhiteSpace(val) ? "None" : val).FontSize(10).FontColor("#000000");
                }

                Row("Blade", _bladeSerial);
                Row("Side", GetFullViewLabel(_defect.View));
                Row("Material", "Auxiliary Component");
                Row("Type", _defect.Anomaly.GetDefectTypeDisplay() ?? "Leading Edge Protection");
                Row("Subtype", _defect.Anomaly.Recommendation ?? "None");
                Row("Height (cm)", _defect.Anomaly.HeightCm > 0 ? _defect.Anomaly.HeightCm.ToString("F2") : "—");
                Row("Width (cm)", _defect.Anomaly.WidthCm > 0 ? _defect.Anomaly.WidthCm.ToString("F2") : "—");
                Row("Area (cm²)", _defect.Anomaly.AreaCm2 > 0 ? _defect.Anomaly.AreaCm2.ToString("F2") : "—");
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