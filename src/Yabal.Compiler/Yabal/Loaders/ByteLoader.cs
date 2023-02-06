namespace Yabal.Loaders;

public class ByteLoader : IFileLoader
{
    public static readonly IFileLoader Instance = new ByteLoader();

    public async ValueTask<FileContent> LoadAsync(YabalBuilder builder, SourceRange range, string path,
        FileReader reader)
    {
        var (_, bytes) = await reader.ReadAllBytesAsync(range, path);
        var content = new int[bytes.Length / 2 + 1];
        var i = 0;
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

        return new FileContent(1, content);
    }
}
