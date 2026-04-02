using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.IO;
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

    // Severity color mapping (same as your image 2 example)
    private static string SeverityColor(int severity) => severity switch
    {

        1 => "#2DCC70",  // Green
        2 => "#B7E35D",  // Green

        3 => "#F2D34F",  // Yellow
        4 => "#F79245",  // Orange
        5 => "#FF3131",  // Red
        _ => Colors.White.ToString()
    };

    private static string SeverityTextColor(int severity) => severity switch
    {
        1 => "#000000",
        2 => "#000000",
        3 => "#000000",
        4 => "#000000",
        5 => "#FFFFFF",
        _ => "#000000"
    };

    public void Compose(IContainer container)
    {
        _defect.Anomaly?.UpdatePhysicalDimensionsFromCoordinates();
        if (_defect.Anomaly != null)
        {
            _defect.Anomaly.AreaCm2 = _defect.Anomaly.WidthCm * _defect.Anomaly.HeightCm;
        }

        container.Column(col =>
        {
            col.Item().PaddingBottom(5)
                .AlignCenter()
                .Text($"Blade {_bladeSerial}")
                .FontSize(14).Bold().FontColor("#4CAF50");

            col.Item().PaddingBottom(15)
                .AlignCenter()
                .Text($"Damage {_defect.DefectNo}")
                .FontSize(12).Bold().FontColor("#4CAF50");

            // ── DYNAMIC IMAGE CONTAINER ──
            col.Item().PaddingBottom(20).AlignCenter().Element(c =>
            {
                if (_imageBytes != null && _defect.Anomaly != null)
                {
                    byte[] croppedBytes = CropAndAnnotate(_imageBytes, _defect.Anomaly) ?? _imageBytes;
                    c.MinHeight(200).MaxHeight(250).AlignCenter().AlignMiddle().Image(croppedBytes, ImageScaling.FitArea);
                }
                else
                {
                    c.Height(200).Background("#EEEEEE").AlignCenter().AlignMiddle().Text("Image Not Available").Italic();
                }
            });

            // ── FIRST TABLE: DEFECT DETAILS ──
            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(160);
                    cols.RelativeColumn();
                });

                void Row(string lbl, string val, bool isSeverityRow = false)
                {
                    bool isBladeRow = lbl == "Blade";
                    string bgColor = isSeverityRow ? SeverityColor(_defect.Anomaly?.Severity ?? 0) :
                                    (isBladeRow ? "#98B7C8" : Colors.White);
                    string textColor = isSeverityRow ? SeverityTextColor(_defect.Anomaly?.Severity ?? 0) : "#000000";

                    table.Cell()
                        .Border(2)
                        .BorderColor("#000000")
                        .Background(bgColor)
                        .Padding(8)
                        .AlignCenter()
                        .Text(lbl)
                        .Bold()
                        .FontSize(10)
                        .FontColor(textColor);

                    table.Cell()
                        .Border(2)
                        .BorderColor("#000000")
                        .Background(bgColor)
                        .Padding(8)
                        .AlignCenter()
                        .Text(string.IsNullOrWhiteSpace(val) ? "None" : val)
                        .FontSize(10)
                        .FontColor(textColor);
                }

                Row("Blade", _bladeSerial);
                Row("Side", GetFullViewLabel(_defect.View));
                Row("Material", "Auxiliary Component");
                Row("Type", _defect.Anomaly.GetDefectTypeDisplay() ?? "Leading Edge Protection");
                Row("Subtype", _defect.Anomaly.Classification?.GetDefectSubtypeString() ?? "None");
                Row("Severity", _defect.Anomaly.Severity.ToString(), true); // ← Severity row highlighted
            });

            // Gap between tables
            col.Item().Height(25);

            // ── SECOND TABLE: DIMENSIONS ──
            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(160);
                    cols.RelativeColumn();
                });

                void Row(string lbl, string val, bool isSeverityRow = false)
                {
                    bool isBladeRow = lbl == "Blade";
                    string bgColor = isSeverityRow ? SeverityColor(_defect.Anomaly?.Severity ?? 0) :
                                    (isBladeRow ? "#98B7C8" : Colors.White);
                    string textColor = isSeverityRow ? SeverityTextColor(_defect.Anomaly?.Severity ?? 0) : "#000000";

                    table.Cell()
                        .Border(2)
                        .BorderColor("#000000")
                        .Background(bgColor)
                        .Padding(8)
                        .AlignCenter()
                        .Text(lbl)
                        .Bold()
                        .FontSize(10)
                        .FontColor(textColor);

                    table.Cell()
                        .Border(2)
                        .BorderColor("#000000")
                        .Background(bgColor)
                        .Padding(8)
                        .AlignCenter()
                        .Text(string.IsNullOrWhiteSpace(val) ? "None" : val)
                        .FontSize(10)
                        .FontColor(textColor);
                }

                Row("Blade", _bladeSerial);
                Row("Defect Height (cm)", _defect.Anomaly.HeightCm > 0 ? _defect.Anomaly.HeightCm.ToString("F2") : "—");
                Row("Defect Width (cm)", _defect.Anomaly.WidthCm > 0 ? _defect.Anomaly.WidthCm.ToString("F2") : "—");
                Row("Defect Area (cm²)", _defect.Anomaly.AreaCm2 > 0 ? _defect.Anomaly.AreaCm2.ToString("F2") : "—");
            });
        });
    }

    // ── EXIF AUTO-ORIENTATION LOGIC ──
    private static SKBitmap? DecodeAutoOrient(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        using var codec = SKCodec.Create(stream);
        if (codec == null) return null;

        var bitmap = SKBitmap.Decode(codec);
        if (bitmap == null) return null;

        var origin = codec.EncodedOrigin;
        if (origin == SKEncodedOrigin.TopLeft || origin == SKEncodedOrigin.Default)
            return bitmap;

        bool needsSwap = origin == SKEncodedOrigin.LeftTop ||
                         origin == SKEncodedOrigin.RightTop ||
                         origin == SKEncodedOrigin.RightBottom ||
                         origin == SKEncodedOrigin.LeftBottom;

        var rotated = new SKBitmap(needsSwap ? bitmap.Height : bitmap.Width, needsSwap ? bitmap.Width : bitmap.Height);
        using var canvas = new SKCanvas(rotated);

        canvas.Translate(rotated.Width / 2f, rotated.Height / 2f);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.RightTop: canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftBottom: canvas.RotateDegrees(270); break;
        }

        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawBitmap(bitmap, 0, 0);
        bitmap.Dispose();

        return rotated;
    }

    // ── CROP AND ANNOTATE ──
    private byte[]? CropAndAnnotate(byte[] imageBytes, Anomaly anomaly)
    {
        using var bitmap = DecodeAutoOrient(imageBytes);
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

        float paddingX = Math.Max(defWidth * 1.5f, 150f);
        float paddingY = Math.Max(defHeight * 1.5f, 150f);

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

        canvas.Clear(SKColors.White);

        var sourceRect = SKRect.Create(cropX, cropY, cropW, cropH);
        var destRect = SKRect.Create(0, 0, cropW, cropH);
        canvas.DrawBitmap(bitmap, sourceRect, destRect);

        using var strokePaint = new SKPaint
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(3f, cropW / 250f),
            IsAntialias = true,
            StrokeJoin = SKStrokeJoin.Miter
        };

        if (coords.IsPolygon && coords.PolygonPoints.Count >= 8)
        {
            using var path = new SKPath();

            float startX = (float)(coords.PolygonPoints[0] * scaleX) - cropX;
            float startY = (float)(coords.PolygonPoints[1] * scaleY) - cropY;
            path.MoveTo(startX, startY);

            for (int i = 1; i < 4; i++)
            {
                float px = (float)(coords.PolygonPoints[i * 2] * scaleX) - cropX;
                float py = (float)(coords.PolygonPoints[i * 2 + 1] * scaleY) - cropY;
                path.LineTo(px, py);
            }

            path.Close();
            canvas.DrawPath(path, strokePaint);
        }
        else
        {
            var boxRect = SKRect.Create(minX - cropX, minY - cropY, defWidth, defHeight);
            canvas.DrawRect(boxRect, strokePaint);
        }

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