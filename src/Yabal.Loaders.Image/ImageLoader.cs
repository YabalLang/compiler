using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Yabal.Loaders;

public class ImageLoader : IFileLoader
{
    public static readonly IFileLoader Instance = new ImageLoader();

    public async ValueTask<FileContent> LoadAsync(YabalBuilder builder, SourceRange range, string path,
        FileReader reader)
    {
        var (_, bytes) = await reader.ReadAllBytesAsync(range, path);
        using var image = Image.Load<Rgba32>(bytes);

        var width = (byte)image.Width;
        var height = (byte)image.Height;

        var content = new int[width * height + 1];
        var i = 0;
        content[i++] = (width << 8) | height;

        Write(image, content, i, height, width);

        return new FileContent(1, content);
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
