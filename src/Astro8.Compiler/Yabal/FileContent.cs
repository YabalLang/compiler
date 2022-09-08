using System.Collections.Concurrent;
using Astro8.Yabal.Ast;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Astro8.Instructions;

public record FileContent(int Offset, int[] Data)
{
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

        var i = 0;

        int[] content;

        FileContent fileContent;

        switch (type)
        {
            case FileType.Image:
            {
                using var image = Image.Load<Rgba32>(bytes);

                var width = (byte)image.Width;
                var height = (byte)image.Height;

                content = new int[width * height + 1];
                content[i++] = (width << 8) | height;

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

                fileContent = new FileContent(1, content);
                break;
            }
            case FileType.Byte:
            {
                content = new int[bytes.Length / 2 + 1];
                content[i++] = bytes.Length / 2;

                var memory = new byte[2];
                int length;

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
}
