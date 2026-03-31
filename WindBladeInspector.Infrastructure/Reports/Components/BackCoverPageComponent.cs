using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using QuestPDF.Helpers;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class BackCoverPageComponent
{
    private readonly byte[]? _logoBytes;
    private readonly byte[]? _backCoverImageBytes;

    private const int W = 1190;
    private const int H = 1684;
    private const string Teal = "#0AABA0";
    private const string DarkBlue = "#00008B";  // DARK BLUE (standard dark blue hex)

    public BackCoverPageComponent(byte[]? logoBytes, byte[]? backCoverImageBytes)
    {
        _logoBytes = logoBytes;
        _backCoverImageBytes = backCoverImageBytes;
    }

    public void Compose(IContainer container) =>
        container.Image(BuildCompositeImage()!);

    private byte[] BuildCompositeImage()
    {
        var info = new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var cv = surface.Canvas;

        // 1. Background
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

        // ── CLEAN LAYOUT ──

        // 1. Top text - BLACK
        float topTextY = Scale(60);
        DrawCenteredText(cv, "KEEP YOUR BLADES SAFER WITH...", Scale(20), topTextY, SKColors.Black, false);

        // 2. QUALIMAX - DARK BLUE + Right aligned
        float qualimaxY = Scale(95);
        float rightEdge = W - Scale(25);
        DrawRightAlignedText(cv, "QUALIMAX", Scale(28), qualimaxY, SKColor.Parse(DarkBlue), true, rightEdge);

        // 3. SERVICES - DARK BLUE + Right aligned
        float servicesY = Scale(130);
        DrawRightAlignedText(cv, "SERVICES", Scale(28), servicesY, SKColor.Parse(DarkBlue), true, rightEdge);

        // 4. URL - Light black + Bottom
        float urlY = H - Scale(65);
        DrawCenteredText(cv, "www.qmservice.in", Scale(18), urlY, new SKColor(60, 60, 60), false);

        cv.Flush();
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
    }

    private static void DrawCenteredText(SKCanvas cv, string text, float fontSize, float y, SKColor color, bool isBold)
    {
        using var typeface = SKTypeface.FromFamilyName("Arial");
        using var font = new SKFont(typeface, fontSize);
        if (isBold) font.Embolden = true;

        float textW = font.MeasureText(text);
        float textX = (W - textW) / 2f;

        // Shadow
        using var shadow = new SKPaint { Color = new SKColor(255, 255, 255, 120), IsAntialias = true };
        cv.DrawText(text, textX + Scale(1), y + Scale(1), font, shadow);

        using var paint = new SKPaint { Color = color, IsAntialias = true };
        cv.DrawText(text, textX, y, font, paint);
    }

    private static void DrawRightAlignedText(SKCanvas cv, string text, float fontSize, float y, SKColor color, bool isBold, float rightEdge)
    {
        using var typeface = SKTypeface.FromFamilyName("Arial");
        using var font = new SKFont(typeface, fontSize);
        if (isBold) font.Embolden = true;

        float textW = font.MeasureText(text);
        float textX = rightEdge - textW;

        // Simple shadow
        using var shadow = new SKPaint { Color = new SKColor(100, 100, 100, 100), IsAntialias = true };
        cv.DrawText(text, textX + Scale(1), y + Scale(1), font, shadow);

        using var paint = new SKPaint { Color = color, IsAntialias = true };
        cv.DrawText(text, textX, y, font, paint);
    }

    private static int Scale(int pts) => pts * 2;
    private static float Scale(float pts) => pts * 2f;
}