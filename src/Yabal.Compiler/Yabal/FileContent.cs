using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Yabal.Ast;

namespace Yabal;

public record FileContent(int Offset, int[] Data)
{
    internal static Dictionary<char, int> CharMappings = new()
    {
        // Special characters
        [' '] = 0, // space -> blank
        ['▪'] = 1, // f1 -> smaller solid square
        ['■'] = 2, // f2 -> full solid square
        ['+'] = 3, // num+ -> +
        ['-'] = 4, // num- -> -
        ['*'] = 5, // num* -> *
        ['/'] = 6, // num/ -> /
        ['□'] = 7, // f3 -> full hollow square
        ['_'] = 8, // _ -> _
        ['<'] = 9, // l-arr -> <
        ['>'] = 10, // r-arr -> >
        ['|'] = 11, // | -> vertical line |

        // Letters
        ['A'] = 13, // a -> a
        ['B'] = 14, // b -> b
        ['C'] = 15, // c -> c
        ['D'] = 16, // d -> d
        ['E'] = 17, // e -> e
        ['F'] = 18, // f -> f
        ['G'] = 19, // g -> g
        ['H'] = 20, // h -> h
        ['I'] = 21, // i -> i
        ['J'] = 22, // j -> j
        ['K'] = 23, // k -> k
        ['L'] = 24, // l -> l
        ['M'] = 25, // m -> m
        ['N'] = 26, // n -> n
        ['O'] = 27, // o -> o
        ['P'] = 28, // p -> p
        ['Q'] = 29, // q -> q
        ['R'] = 30, // r -> r
        ['S'] = 31, // s -> s
        ['T'] = 32, // t -> t
        ['U'] = 33, // u -> u
        ['V'] = 34, // v -> v
        ['W'] = 35, // w -> w
        ['X'] = 36, // x -> x
        ['Y'] = 37, // y -> y
        ['Z'] = 38, // z -> z

        // Numbers
        ['0'] = 39, // 0 -> 0
        ['1'] = 40, // 1 -> 1
        ['2'] = 41, // 2 -> 2
        ['3'] = 42, // 3 -> 3
        ['4'] = 43, // 4 -> 4
        ['5'] = 44, // 5 -> 5
        ['6'] = 45, // 6 -> 6
        ['7'] = 46, // 7 -> 7
        ['8'] = 47, // 8 -> 8
        ['9'] = 48, // 9 -> 9
    };

    private static readonly Dictionary<(string, FileType), FileContent> Cache = new();

    public static FileContent Get(string path, FileType type)
    {
        if (!Cache.TryGetValue((path, type), out var fileContent))
        {
            throw new InvalidOperationException("File was not loaded");
        }

        return fileContent;
    }

    public static async ValueTask LoadAsync(string path, FileType type)
    {
        var key = (path, type);

        if (Cache.ContainsKey(key))
        {
            return;
        }

        var i = 0;

        int[] content;

        FileContent fileContent;

        switch (type)
        {
            case FileType.Font:
            {
                const int charWidth = 8;
                const int charHeight = 8;
                const int charLength = charWidth * charHeight;
                content = new int[charLength * 49];

                var font = SystemFonts.Get(path).CreateFont(12, FontStyle.Bold);

                var drawingOptions = new DrawingOptions
                {
                    GraphicsOptions = new GraphicsOptions
                    {
                        //Antialias = false
                    }
                };

                var options = new TextOptions(font)
                {
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Origin = new PointF(charWidth / 2f, charHeight / 2f),
                    ColorFontSupport = ColorFontSupport.None,
                };

                var brush = Brushes.Solid(Color.White);

                foreach (var (c, index) in CharMappings)
                {
                    using var image = new Image<Rgba32>(charWidth, charHeight);

                    image.Mutate(x =>
                    {
                        x.DrawText(drawingOptions, options, c.ToString(), brush, null);
                    });

                    WriteFont(image, content, index * charLength, charHeight, charWidth);
                }

                fileContent = new FileContent(1, content);
                break;
            }
            case FileType.Image:
            {
                var bytes = await GetBytes(path);
                using var image = Image.Load<Rgba32>(bytes);

                var width = (byte)image.Width;
                var height = (byte)image.Height;

                content = new int[width * height + 1];
                content[i++] = (width << 8) | height;

                Write(image, content, i, height, width);

                fileContent = new FileContent(1, content);
                break;
            }
            case FileType.Byte:
            {
                var bytes = await GetBytes(path);
                content = new int[bytes.Length / 2 + 1];
                content[i++] = bytes.Length / 2;

                var memory = new byte[2];

                for (var j = 0; j < bytes.Length; j += 2)
                {
                    if (j + 1 < bytes.Length)
                    {
                        content[i++] = memory[0] << 8;
                    }
                    else
                    {
                        content[i++] = memory[0] << 8 | memory[1];
                    }
                }

                fileContent = new FileContent(1, content);
                break;
            }
            default:
                throw new NotSupportedException();
        }

        Cache[key] = fileContent;
    }

    private static async Task<byte[]> GetBytes(string path)
    {
        byte[] bytes;

        if (path.StartsWith("http"))
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(path);
            bytes = await response.Content.ReadAsByteArrayAsync();
        }
        else
        {
            bytes = await File.ReadAllBytesAsync(path);
        }

        return bytes;
    }

    private static void WriteFont(Image<Rgba32> image, int[] content, int i, byte height, byte width)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                var alpha = pixel.A / 255f;
                var r = (int)(pixel.R * alpha);
                var g = (int)(pixel.G * alpha);
                var b = (int)(pixel.B * alpha);
                content[i++] = (r / 8 << 10) | (g / 8 << 5) | (b / 8);
            }
        }
    }

    private static void Write(Image<Rgba32> image, int[] content, int i, byte height, byte width)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                var a = pixel.A > 0 ? 1 : 0;
                var value = (a << 15) | (pixel.R / 8 << 10) | (pixel.G / 8 << 5) | (pixel.B / 8);

                content[i++] = value;
            }
        }
    }
}
