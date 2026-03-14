using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Infrastructure.Reports.Components;

/// <summary>
/// Cover page composited entirely via SkiaSharp into a single image.
/// Avoids all QuestPDF Layers/Extend page-split bugs.
/// Layout: full-bleed photo → dark overlay bands → QUALIMAX branding →
///         report title + year → teal info strip at bottom.
/// </summary>
internal sealed class CoverPageComponent
{
    private readonly InspectionProject _project;
    private readonly byte[]? _logoBytes;
    private readonly byte[]? _coverImageBytes;

    // Render at 2× A4 points for sharp output (595×842 pt → 1190×1684 px)
    private const int W = 1190;
    private const int H = 1684;

    private const string Teal = "#0AABA0";
    private const string White = "#FFFFFF";

    public CoverPageComponent(InspectionProject project, byte[]? logoBytes, byte[]? coverImageBytes)
    {
        _project = project;
        _logoBytes = logoBytes;
        _coverImageBytes = coverImageBytes;
    }

    public void Compose(IContainer container)
    {
        var compositeBytes = BuildCompositeImage();
        // Single image fills the entire zero-margin page — no Layers, no Extend
        container.Image(compositeBytes, ImageScaling.FitArea);
    }

    private byte[] BuildCompositeImage()
    {
        var info = new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var cv = surface.Canvas;

        // ── 1. Background: cover photo or dark green fallback ─────────────
        if (_coverImageBytes != null)
        {
            using var photoBitmap = SKBitmap.Decode(_coverImageBytes);
            if (photoBitmap != null)
            {
                var destRect = SKRect.Create(0, 0, W, H);
                cv.DrawBitmap(photoBitmap, destRect);
            }
            else
            {
                cv.Clear(new SKColor(0x1a, 0x3a, 0x2a));
            }
        }
        else
        {
            cv.Clear(new SKColor(0x1a, 0x3a, 0x2a));
        }

        // ── 2. Dark overlay bands ──────────────────────────────────────────
        int topBandH = Scale(110);
        int bottomBandH = Scale(170);

        using (var p = new SKPaint { Color = new SKColor(0, 0, 0, 140), Style = SKPaintStyle.Fill })
            cv.DrawRect(SKRect.Create(0, 0, W, topBandH), p);

        using (var p = new SKPaint { Color = new SKColor(0, 0, 0, 115), Style = SKPaintStyle.Fill })
            cv.DrawRect(SKRect.Create(0, H - bottomBandH, W, bottomBandH), p);

        // ── 3. Logo (top-left) ────────────────────────────────────────────
        int pad = Scale(18);
        if (_logoBytes is { Length: > 0 })
        {
            using var logoBmp = SKBitmap.Decode(_logoBytes);
            if (logoBmp != null)
            {
                int logoSize = Scale(70);
                var logoRect = SKRect.Create(pad, pad, logoSize, logoSize);
                cv.DrawBitmap(logoBmp, logoRect);
            }
        }

        // ── 4. "QUALIMAX SERVICES" (top-right) ───────────────────────────
        using (var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        using (var paint = new SKPaint { Color = SKColors.White, IsAntialias = true })
        {
            float fontSize = Scale(22);
            using var font = new SKFont(tf, fontSize);

            float lineH = fontSize * 1.25f;
            float textY = pad + lineH;
            float rightX = W - pad;

            DrawRightAligned(cv, font, paint, "QUALIMAX", rightX, textY);
            DrawRightAligned(cv, font, paint, "SERVICES", rightX, textY + lineH);
        }

        // ── 5. Report title + year (bottom-left, above teal strip) ────────
        int tealStripH = Scale(130); // approximate teal strip height
        int titleAreaY = H - bottomBandH;
        int titleAreaH = bottomBandH - tealStripH;

        using (var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        using (var paint = new SKPaint { Color = SKColors.White, IsAntialias = true })
        {
            float smallFs = Scale(11);
            using var smallFont = new SKFont(tf, smallFs);
            float lineH = smallFs * 1.4f;
            float baseY = titleAreaY + titleAreaH - Scale(12);

            cv.DrawText("BLADE INSPECTION REPORT", pad, baseY, smallFont, paint);
            cv.DrawText("DETAILED WIND TURBINE", pad, baseY - lineH, smallFont, paint);

            // Vertical divider
            int divX = Scale(220);
            using (var divPaint = new SKPaint { Color = SKColors.White, StrokeWidth = Scale(2), IsAntialias = true })
                cv.DrawLine(divX, baseY - lineH * 1.5f, divX, baseY + Scale(4), divPaint);

            // Year
            float yearFs = Scale(36);
            using var yearFont = new SKFont(tf, yearFs);
            cv.DrawText(_project.InspectionDate.Year.ToString(),
                        divX + Scale(12), baseY, yearFont, paint);
        }

        // ── 6. Teal info strip (bottom) ────────────────────────────────────
        var tealColor = SKColor.Parse(Teal);
        var stripRect = SKRect.Create(0, H - tealStripH, W, tealStripH);

        using (var p = new SKPaint { Color = tealColor, Style = SKPaintStyle.Fill })
            cv.DrawRect(stripRect, p);

        using (var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        using (var tfBold = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        using (var labelPaint = new SKPaint { Color = SKColors.White, IsAntialias = true })
        using (var valuePaint = new SKPaint { Color = SKColors.White, IsAntialias = true })
        {
            float fs = Scale(9);
            float lineH = fs * 1.8f;
            using var labelFont = new SKFont(tfBold, fs);
            using var valueFont = new SKFont(tf, fs);

            var rows = new[]
            {
                ("CLIENT",          string.IsNullOrWhiteSpace(_project.Client)  ? "NOT SPECIFIED" : _project.Client.ToUpper()),
                ("PARK",            _project.ParkName.ToUpper()),
                ("TURBINE NUMBER",  _project.TurbineId),
                ("MODEL",           string.IsNullOrWhiteSpace(_project.Model)   ? "—" : _project.Model.ToUpper()),
                ("INSPECTION DATE", _project.InspectionDate.ToString("dd MMM yyyy").ToUpper()),
            };

            float colLabel = pad;
            float colDash = W / 2f - Scale(30);
            float colValue = W / 2f;
            float rowY = H - tealStripH + lineH;

            foreach (var (label, value) in rows)
            {
                // Bullet
                cv.DrawText("○", colLabel - Scale(10), rowY, valueFont, valuePaint);
                cv.DrawText(label, colLabel, rowY, labelFont, labelPaint);
                cv.DrawText("—", colDash, rowY, valueFont, valuePaint);
                cv.DrawText(value, colValue, rowY, valueFont, valuePaint);
                rowY += lineH;
            }
        }

        cv.Flush();
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
    }

    /// <summary>Scale a point value to pixel space (2× ratio).</summary>
    private static int Scale(int pts) => pts * 2;
    private static float Scale(float pts) => pts * 2f;

    private static void DrawRightAligned(SKCanvas cv, SKFont font, SKPaint paint, string text, float rightX, float y)
    {
        float w = font.MeasureText(text);
        cv.DrawText(text, rightX - w, y, font, paint);
    }
}