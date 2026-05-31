using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

/// <summary>
/// Renders a blade inspection image with defect annotation boxes overlaid using SkiaSharp.
/// Produces a composite image: blade photo + numbered red/coloured rectangles over each defect.
/// </summary>
internal sealed class DefectAnnotatedImageComponent
{
    private readonly byte[] _sourceImageBytes;
    private readonly IReadOnlyList<(Anomaly Anomaly, int DefectNo)> _anomalies;

    public DefectAnnotatedImageComponent(
        byte[] sourceImageBytes,
        IReadOnlyList<(Anomaly, int)> anomalies)
    {
        _sourceImageBytes = sourceImageBytes;
        _anomalies = anomalies;
    }

    /// <summary>
    /// Renders the annotated image into a QuestPDF container.
    /// The image has red numbered boxes drawn over each defect.
    /// </summary>
    public void Compose(IContainer container)
    {
        var annotatedBytes = DrawAnnotations(_sourceImageBytes, _anomalies);

        container
            .Border(1).BorderColor("#CCCCCC")
            .Image(annotatedBytes)
            .FitWidth();
    }

    // ── SkiaSharp annotation rendering ───────────────────────────────────────

    private static byte[] DrawAnnotations(
        byte[] imageBytes,
        IReadOnlyList<(Anomaly Anomaly, int DefectNo)> anomalies)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap == null) return imageBytes; // fallback: original

        // Work on a copy at original resolution
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;

        // Draw original image
        using var img = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(img, 0, 0);

        foreach (var (anomaly, defectNo) in anomalies)
        {
            var coords = anomaly.Coordinates;
            if (coords == null || coords.ReferenceWidth <= 0 || coords.ReferenceHeight <= 0)
                continue;

            // Scale coordinates to the actual bitmap size
            float scaleX = (float)bitmap.Width / (float)coords.ReferenceWidth;
            float scaleY = (float)bitmap.Height / (float)coords.ReferenceHeight;

            float x = (float)coords.X * scaleX;
            float y = (float)coords.Y * scaleY;
            float w = (float)coords.Width * scaleX;
            float h = (float)coords.Height * scaleY;

            var boxColor = GetSeverityColor(anomaly.Severity);

            // ── Filled semi-transparent box ─────────────────────────────────
            using var fillPaint = new SKPaint
            {
                Color = boxColor.WithAlpha(60),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(x, y, w, h, fillPaint);

            // ── Solid border ────────────────────────────────────────────────
            using var strokePaint = new SKPaint
            {
                Color = boxColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(1f, bitmap.Width / 800f)
            };
            canvas.DrawRect(x, y, w, h, strokePaint);

            // ── Label badge (top-left of box) ───────────────────────────────
            var label = $"#{defectNo}";
            float fontSize = Math.Max(14f, bitmap.Width / 120f);

            using var badgePaint = new SKPaint { Color = boxColor, Style = SKPaintStyle.Fill };
            float badgeW = fontSize * 2.2f;
            float badgeH = fontSize * 1.4f;
            float badgeX = x;
            float badgeY = Math.Max(0, y - badgeH);
            canvas.DrawRect(badgeX, badgeY, badgeW, badgeH, badgePaint);

            using var textFont = new SKFont { Size = fontSize };
            using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawText(label, badgeX + 4f, badgeY + badgeH - 4f, textFont, textPaint);
        }

        canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 92);
        return data.ToArray();
    }

    private static SKColor GetSeverityColor(int severity) => severity switch
    {
        1 => new SKColor(76, 175, 80),   // Green
        2 => new SKColor(139, 195, 74),  // Light green
        3 => new SKColor(255, 152, 0),   // Orange
        4 => new SKColor(244, 67, 54),   // Red
        5 => new SKColor(183, 28, 28),   // Dark red
        _ => new SKColor(244, 67, 54)
    };
}
