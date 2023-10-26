using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Yabal.Loaders;

public class FontLoader : IFileLoader
{
    public static readonly IFileLoader Instance = new FontLoader();

    public async ValueTask<FileContent> LoadAsync(YabalBuilder builder, SourceRange range, string path,
        FileReader reader)
    {
        var settingsIndex = path.IndexOf(';');
        var settings = new FontSettings();

        if (settingsIndex > 0)
        {
            var settingsString = path.Substring(settingsIndex + 1);
            path = path.Substring(0, settingsIndex);

            foreach (var setting in settingsString.Split(','))
            {
                var settingParts = setting.Split('=');
                var settingName = settingParts[0];
                var settingValue = settingParts[1];

                try
                {
                    switch (settingName.ToLowerInvariant())
                    {
                        case "scale":
                            settings.Scale = int.Parse(settingValue);
                            break;
                        case "size":
                            settings.Size = int.Parse(settingValue);
                            break;
                        case "width":
                            settings.Width = int.Parse(settingValue);
                            break;
                        case "height":
                            settings.Height = int.Parse(settingValue);
                            break;
                        case "antialias":
                            settings.Antialias = settingValue switch
                            {
                                "0" => false,
                                "1" => true,
                                _ => bool.Parse(settingValue)
                            };
                            break;
                    }
                }
                catch
                {
                    builder.AddError(ErrorLevel.Warning, range, $"Invalid font setting '{settingName}'");
                }
            }
        }

        var collection = new FontCollection();

        FontFamily fontFamily;
        await using (var result = await reader.GetStreamAsync(range, path))
        {
            fontFamily = collection.Add(result.Stream);
        }

        var scale = settings.Scale;
        var font = fontFamily.CreateFont(settings.Size * scale, FontStyle.Regular);

        var drawingOptions = new DrawingOptions
        {
            GraphicsOptions = new GraphicsOptions
            {
                Antialias = settings.Antialias
            }
        };

        var charWidth = settings.Width;
        var charHeight = settings.Height;
        var charLength = charWidth * charHeight;
        var content = new int[charLength * 49];

        var options = new RichTextOptions(font)
        {
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(charWidth * scale / 2f, charHeight * scale / 2f),
            ColorFontSupport = ColorFontSupport.None,
        };

        var brush = Brushes.Solid(Color.White);

        foreach (var (c, index) in CharMappings)
        {
            using var image = new Image<Rgba32>(charWidth * scale, charHeight * scale);

            image.Mutate(x =>
            {
                x.DrawText(drawingOptions, options, c.ToString(), brush, pen: null);
                if (scale > 1) x.Resize(charWidth, charHeight, KnownResamplers.Robidoux);
            });

            WriteFont(image, content, index * charLength, charHeight, charWidth);
        }

        return new FileContent(1, content);
    }

    private static void WriteFont(Image<Rgba32> image, int[] content, int i, int height, int width)
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

    public class FontSettings
    {
        public int Scale { get; set; } = 1;

        public int Size { get; set; } = 12;

        public int Width { get; set; } = 8;

        public int Height { get; set; } = 8;

        public bool Antialias { get; set; } = true;
    }


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
}
