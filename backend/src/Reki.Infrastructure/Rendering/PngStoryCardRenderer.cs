using System.Globalization;
using Reki.Application.Common;
using Reki.Application.Posts;
using SkiaSharp;

namespace Reki.Infrastructure.Rendering;

internal sealed class PngStoryCardRenderer : IStoryCardRenderer, IDisposable
{
    private const int Width = 1080;
    private const int Height = 1920;
    private const int MaxConcurrentRenders = 2;

    private readonly SemaphoreSlim _renderSlots = new(MaxConcurrentRenders, MaxConcurrentRenders);
    private readonly SKTypeface _regularTypeface;
    private readonly SKTypeface _boldTypeface;

    public PngStoryCardRenderer()
    {
        _regularTypeface = LoadTypeface("DejaVuSans.ttf");
        _boldTypeface = LoadTypeface("DejaVuSans-Bold.ttf");
    }

    public async ValueTask RenderAsync(
        StoryCardData card,
        Stream target,
        CancellationToken cancellationToken)
    {
        await _renderSlots.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var surface = SKSurface.Create(new SKImageInfo(
                Width,
                Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul));
            SKCanvas canvas = surface.Canvas;
            DrawBackground(canvas);
            DrawCard(canvas, card);
            canvas.Flush();

            using SKImage image = surface.Snapshot();
            using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 92)
                ?? throw new InvalidOperationException("Unable to encode the story card as PNG.");
            using Stream encodedStream = encoded.AsStream();
            await encodedStream.CopyToAsync(target, cancellationToken);
        }
        finally
        {
            _renderSlots.Release();
        }
    }

    public void Dispose()
    {
        _renderSlots.Dispose();
        DisposeOwnedTypeface(_regularTypeface);
        if (!ReferenceEquals(_regularTypeface, _boldTypeface))
        {
            DisposeOwnedTypeface(_boldTypeface);
        }
    }

    private static SKTypeface LoadTypeface(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "fonts", fileName);
        return File.Exists(path) ? SKTypeface.FromFile(path) : SKTypeface.Default;
    }

    private static void DisposeOwnedTypeface(SKTypeface typeface)
    {
        if (!ReferenceEquals(typeface, SKTypeface.Default))
        {
            typeface.Dispose();
        }
    }

    private static string NormalizeHandle(string handle)
    {
        return handle.StartsWith('@') ? handle : $"@{handle}";
    }

    private static string FormatNumber(long value)
    {
        return value.ToString("N0", CultureInfo.GetCultureInfo("ru-RU"));
    }

    private static void DrawWrappedLine(
        SKCanvas canvas,
        SKFont font,
        SKPaint paint,
        IReadOnlyList<string> words,
        float left,
        float baseline,
        int lineNumber,
        float lineHeight)
    {
        if (words.Count > 0)
        {
            canvas.DrawText(
                string.Join(' ', words),
                left,
                baseline + (lineNumber * lineHeight),
                SKTextAlign.Left,
                font,
                paint);
        }
    }

    private static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        SKFont font,
        SKPaint paint,
        float left,
        float baseline,
        float maxWidth,
        int maxLines,
        float lineHeight)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new List<string>();
        int lineNumber = 0;
        foreach (string word in words)
        {
            string candidate = line.Count == 0 ? word : $"{string.Join(' ', line)} {word}";
            if (font.MeasureText(candidate) <= maxWidth)
            {
                line.Add(word);
                continue;
            }

            DrawWrappedLine(canvas, font, paint, line, left, baseline, lineNumber, lineHeight);
            lineNumber++;
            line.Clear();
            line.Add(word);
            if (lineNumber == maxLines - 1)
            {
                break;
            }
        }

        if (line.Count > 0 && lineNumber < maxLines)
        {
            string value = string.Join(' ', line);
            if (lineNumber == maxLines - 1 && words.Length > line.Count)
            {
                value += "…";
            }

            canvas.DrawText(
                value,
                left,
                baseline + (lineNumber * lineHeight),
                SKTextAlign.Left,
                font,
                paint);
        }
    }

    private void DrawBackground(SKCanvas canvas)
    {
        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(Width, Height),
            [new SKColor(42, 18, 54), new SKColor(14, 14, 19), new SKColor(18, 32, 58)],
            [0f, 0.55f, 1f],
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader, IsAntialias = true };
        canvas.DrawRect(0, 0, Width, Height, paint);
    }

    private void DrawCard(SKCanvas canvas, StoryCardData card)
    {
        DrawText(canvas, "Реки", 82, _boldTypeface, new SKColor(255, 184, 107), 90, 150);
        DrawText(canvas, card.PlayerName, 66, _boldTypeface, SKColors.White, 90, 300);
        DrawText(
            canvas,
            NormalizeHandle(card.Handle),
            38,
            _regularTypeface,
            new SKColor(201, 198, 216),
            90,
            365);
        DrawText(
            canvas,
            FormatNumber(card.Subscribers),
            128,
            _boldTypeface,
            new SKColor(255, 107, 139),
            90,
            570);
        DrawText(canvas, "подписчиков", 42, _regularTypeface, SKColors.White, 94, 635);

        using var cardPaint = new SKPaint
        {
            Color = new SKColor(31, 31, 43, 240),
            IsAntialias = true,
        };
        canvas.DrawRoundRect(new SKRect(70, 730, 1010, 1450), 42, 42, cardPaint);
        DrawText(
            canvas,
            card.Viral ? "ЗАЛЕТЕВШИЙ ПОСТ" : "ПОСТ",
            30,
            _boldTypeface,
            new SKColor(155, 184, 255),
            115,
            810);

        using var postFont = new SKFont(_regularTypeface, 52);
        using var postPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        DrawWrappedText(canvas, card.PostText, postFont, postPaint, 115, 900, 850, 7, 72);

        DrawText(
            canvas,
            $"Просмотры  {FormatNumber(card.Views)}",
            34,
            _regularTypeface,
            new SKColor(201, 198, 216),
            115,
            1350);
        DrawText(
            canvas,
            $"Подписчики  +{FormatNumber(card.SubscriberDelta)}",
            34,
            _boldTypeface,
            new SKColor(126, 231, 135),
            540,
            1350);
        DrawText(canvas, "а ты сколько наберёшь?", 44, _boldTypeface, SKColors.White, 90, 1630);
        DrawText(
            canvas,
            "t.me/reki_game",
            36,
            _regularTypeface,
            new SKColor(201, 198, 216),
            90,
            1695);
    }

    private void DrawText(
        SKCanvas canvas,
        string text,
        float size,
        SKTypeface typeface,
        SKColor color,
        float left,
        float baseline)
    {
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, left, baseline, SKTextAlign.Left, font, paint);
    }
}
