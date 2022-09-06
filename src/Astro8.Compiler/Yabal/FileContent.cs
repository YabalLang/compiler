using System.Collections.Concurrent;
using Astro8.Yabal.Ast;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Astro8.Instructions;

public record FileContent(int Offset, int[] Data)
{
    private static readonly ConcurrentDictionary<(string, FileType), FileContent> Cache = new();

    public static FileContent Get(string path, FileType type)
    {
        return Cache.GetOrAdd((path, type), FileContentFactory);
    }

    private static FileContent FileContentFactory((string, FileType) key)
    {
        var (path, type) = key;
        using var stream = File.OpenRead(path);
        var i = 0;

        int[] content;

        switch (type)
        {
            case FileType.Image:
            {
                using var image = Image.Load<Rgba32>(stream);

                var width = (byte) image.Width;
                var height = (byte) image.Height;

                content = new int[width * height + 1];
                content[i++] = (width << 8) | height;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var pixel = image[x, y];
                        var value = (pixel.A / 16 << 15) | (pixel.R / 8 << 10) | (pixel.G / 8 << 5) | (pixel.B / 8);

                        content[i++] = value;
                    }
                }

                return new FileContent(1, content);
            }
            case FileType.Byte:
            {
                content = new int[stream.Length / 2 + 1];
                content[i++] = (int) (stream.Length / 2);

                var memory = new byte[2];
                int length;

                while ((length = stream.Read(memory, 0, 2)) > 0)
                {
                    if (length == 1)
                    {
                        content[i++] = memory[0] << 8;
                    }
                    else
                    {
                        content[i++] = memory[0] << 8 | memory[1];
                    }
                }

                return new FileContent(1, content);
            }
            default:
                throw new NotSupportedException();
        }
    }
}