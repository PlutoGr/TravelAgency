using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using TravelAgency.Shared.Contracts.Seeding;

namespace TravelAgency.Media.Infrastructure.Seeding;

/// <summary>
/// Демо-фото без чужих прав: градиент и подпись, нарисованные в процессе.
/// Из сети ничего не скачивается.
/// </summary>
public static class DemoTourImage
{
    public const int Width = DemoSeedIds.PhotoWidthPx;
    public const int Height = 900;
    public const string ContentType = "image/webp";
    public const int MaxBytes = 400 * 1024;

    public static byte[] Create(string caption, int variant)
    {
        using var image = new Image<Rgba32>(Width, Height);
        var (from, to) = Palette(variant);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var v = y / (float)(accessor.Height - 1);
                for (var x = 0; x < row.Length; x++)
                {
                    var u = x / (float)(row.Length - 1);
                    var mix = (u + v) / 2f;
                    row[x] = new Rgba32(
                        Lerp(from.R, to.R, mix),
                        Lerp(from.G, to.G, mix),
                        Lerp(from.B, to.B, mix));
                }
            }
        });

        DrawCaption(image, caption);
        using var buffer = new MemoryStream();
        image.SaveAsWebp(buffer, new WebpEncoder { Quality = 70 });
        var bytes = buffer.ToArray();
        if (bytes.Length > MaxBytes)
            throw new InvalidOperationException($"Demo image is {bytes.Length} bytes, limit is {MaxBytes}.");

        return bytes;
    }

    private static void DrawCaption(Image<Rgba32> image, string caption)
    {
        const int scale = 4;
        var text = caption.ToUpperInvariant();
        var x = 48;
        var y = Height - 48 - (7 * scale);
        foreach (var ch in text)
        {
            if (ch == ' ')
            {
                x += 4 * scale;
                continue;
            }

            if (!Glyphs.TryGetValue(ch, out var rows))
                continue;

            for (var row = 0; row < rows.Length; row++)
            {
                var bits = rows[row];
                for (var col = 0; col < bits.Length; col++)
                {
                    if (bits[col] != '1')
                        continue;

                    Fill(image, x + col * scale, y + row * scale, scale, new Rgba32(255, 255, 255));
                }
            }

            x += (5 + 1) * scale;
        }
    }

    private static void Fill(Image<Rgba32> image, int left, int top, int size, Rgba32 color)
    {
        for (var y = top; y < top + size; y++)
        {
            if (y < 0 || y >= image.Height)
                continue;
            for (var x = left; x < left + size; x++)
            {
                if (x < 0 || x >= image.Width)
                    continue;
                image[x, y] = color;
            }
        }
    }

    private static byte Lerp(byte from, byte to, float t) =>
        (byte)(from + (to - from) * t);

    private static (Rgba32 From, Rgba32 To) Palette(int variant)
    {
        var palettes = new (Rgba32 From, Rgba32 To)[]
        {
            (new Rgba32(8, 90, 140), new Rgba32(120, 210, 190)),
            (new Rgba32(180, 90, 30), new Rgba32(240, 190, 80)),
            (new Rgba32(40, 50, 120), new Rgba32(180, 140, 200)),
            (new Rgba32(20, 110, 70), new Rgba32(170, 210, 90)),
            (new Rgba32(90, 40, 20), new Rgba32(220, 160, 90)),
        };
        var pair = palettes[Math.Abs(variant) % palettes.Length];
        return pair;
    }

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
        ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
        ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        ['G'] = ["01111", "10000", "10000", "10011", "10001", "10001", "01110"],
        ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['J'] = ["00111", "00010", "00010", "00010", "10010", "10010", "01100"],
        ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        ['W'] = ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
        ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
        ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
        ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
        ['5'] = ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
        ['6'] = ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
        ['-'] = ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
    };
}
