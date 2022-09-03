using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public class FileAddress : IAddress
{
    private readonly string _path;
    private readonly FileType _type;

    public FileAddress(string path, FileType type)
    {
        _path = path;
        _type = type;
    }

    public Either<int, InstructionPointer>? Get(YabalBuilder builder) => builder.GetFile(_path, _type);

    public int? Length
    {
        get
        {
            var (offset, content) = FileContent.Get(_path, _type);
            return content.Length - offset;
        }
    }

    public int? GetValue(int offset)
    {
        var (contentOffset, content) = FileContent.Get(_path, _type);
        return content[contentOffset + offset];
    }

    public static IAddress From(string path, FileType type) => new FileAddress(path, type);
}
