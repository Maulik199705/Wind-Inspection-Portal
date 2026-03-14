using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace WindBladeInspector.Infrastructure.Reports.Components;

/// <summary>
/// Back cover page composited entirely via SkiaSharp into a single full-bleed image.
/// Zero overlays — the background photo is fully visible.
/// Text uses a dark drop-shadow stroke for legibility on any background colour.
/// </summary>
internal sealed class BackCoverPageComponent
{
    private readonly byte[]? _logoBytes;
    private readonly byte[]? _backCoverImageBytes;

    private const int W = 1190;
    private const int H = 1684;

    private const string Teal = "#0AABA0";

    public BackCoverPageComponent(byte[]? logoBytes, byte[]? backCoverImageBytes)
    {
        _logoBytes = logoBytes;
        _backCoverImageBytes = backCoverImageBytes;
    }

    public void Compose(IContainer container) =>
        container.Image(BuildCompositeImage(), ImageScaling.FitArea);

    private byte[] BuildCompositeImage()
    {
        var info = new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var cv = surface.Canvas;

        // ── 1. Full-bleed background — no overlay whatsoever ─────────────
        if (_backCoverImageBytes != null)
        {
            using var bmp = SKBitmap.Decode(_backCoverImageBytes);
            if (bmp != null)
                cv.DrawBitmap(bmp, SKRect.Create(0, 0, W, H));
            else
                cv.Clear(SKColor.Parse(Teal));
        }
        else
        {
            cv.Clear(SKColor.Parse(Teal));
        }

        int pad = Scale(18);

        // ── 2. Logo (top-left) ─────────────────────────────────────────────
        if (_logoBytes is { Length: > 0 })
        {
            using var logoBmp = SKBitmap.Decode(_logoBytes);
            if (logoBmp != null)
            {
                int logoSize = Scale(70);
                cv.DrawBitmap(logoBmp, SKRect.Create(pad, pad, logoSize, logoSize));
            }
        }

        // ── 3. "QUALIMAX SERVICES" (top-right) — white + dark shadow ──────
        using (var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        {
            float fontSize = Scale(22);
            using var font = new SKFont(tf, fontSize);
            float lineH = fontSize * 1.25f;
            float textY = pad + lineH;
            float rightX = W - pad;

            // Shadow pass
            using (var shadowPaint = new SKPaint { Color = new SKColor(0, 0, 0, 120), IsAntialias = true })
            {
                DrawRightAligned(cv, font, shadowPaint, "QUALIMAX", rightX + Scale(2), textY + Scale(2));
                DrawRightAligned(cv, font, shadowPaint, "SERVICES", rightX + Scale(2), textY + lineH + Scale(2));
            }
            // White text
            using (var whitePaint = new SKPaint { Color = SKColors.White, IsAntialias = true })
            {
                DrawRightAligned(cv, font, whitePaint, "QUALIMAX", rightX, textY);
                DrawRightAligned(cv, font, whitePaint, "SERVICES", rightX, textY + lineH);
            }
        }

        // ── 4. Centre branding — no pill, just shadowed white text ────────
        float cx = W / 2f;
        float cy = H / 2f;

        using (var tfBold = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        using (var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        {
            // ── Tag line ────────────────────────────────────────────────────
            float tagFs = Scale(12);
            using var tagFont = new SKFont(tf, tagFs);
            string tagLine = "KEEP YOUR BLADES SAFER WITH...";
            float tagW = tagFont.MeasureText(tagLine);
            float tagX = cx - tagW / 2f;
            float tagY = cy - Scale(40);

            using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 130), IsAntialias = true })
                cv.DrawText(tagLine, tagX + Scale(1), tagY + Scale(1), tagFont, shadow);
            using (var white = new SKPaint { Color = SKColors.White, IsAntialias = true })
                cv.DrawText(tagLine, tagX, tagY, tagFont, white);

            // ── Brand name ──────────────────────────────────────────────────
            float brandFs = Scale(30);
            using var brandFont = new SKFont(tfBold, brandFs);
            string brand = "QUALIMAX SERVICES";
            float brandW = brandFont.MeasureText(brand);
            float brandX = cx - brandW / 2f;
            float brandY = cy + Scale(10);

            using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 130), IsAntialias = true })
                cv.DrawText(brand, brandX + Scale(2), brandY + Scale(2), brandFont, shadow);
            using (var white = new SKPaint { Color = SKColors.White, IsAntialias = true })
                cv.DrawText(brand, brandX, brandY, brandFont, white);

            // ── Teal divider line ───────────────────────────────────────────
            float divY = brandY + Scale(20);
            int divHalfW = Scale(120);
            using var divPaint = new SKPaint
            {
                Color = SKColor.Parse(Teal),
                StrokeWidth = Scale(3),
                IsAntialias = true
            };
            cv.DrawLine(cx - divHalfW, divY, cx + divHalfW, divY, divPaint);
        }

        // ── 5. Website URL (bottom) — shadowed white, no pill ─────────────
        using (var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
        {
            float fs = Scale(14);
            using var font = new SKFont(tf, fs);
            string url = "www.qmservice.in";
            float urlW = font.MeasureText(url);
            float urlX = (W - urlW) / 2f;
            float urlY = H - Scale(60);

            using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 130), IsAntialias = true })
                cv.DrawText(url, urlX + Scale(1), urlY + Scale(1), font, shadow);
            using (var white = new SKPaint { Color = SKColors.White, IsAntialias = true })
                cv.DrawText(url, urlX, urlY, font, white);
        }

        cv.Flush();
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
    }

    private static int Scale(int pts) => pts * 2;
    private static float Scale(float pts) => pts * 2f;

    private static void DrawRightAligned(SKCanvas cv, SKFont font, SKPaint paint, string text, float rightX, float y)
    {
        float w = font.MeasureText(text);
        cv.DrawText(text, rightX - w, y, font, paint);
    }
}