using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public class FileAddress : IAddress
{
    private readonly string _path;
    private readonly FileType _type;

    public FileAddress(string path, FileType type, Pointer pointer)
    {
        _path = path;
        _type = type;
        Pointer = pointer;
    }

    public Pointer Pointer { get; }

    public int? Length
    {
        get
        {
            var (offset, content) = FileContent.Get(_path, _type);
            return content.Length - offset;
        }
    }

    public LanguageType Type => LanguageType.Integer;

    public int? GetValue(int offset)
    {
        var (contentOffset, content) = FileContent.Get(_path, _type);
        return content[contentOffset + offset];
    }

    public static IAddress From(string path, FileType type, Pointer pointer) => new FileAddress(path, type, pointer);

    public override string ToString()
    {
        return Pointer.ToString() ?? "";
    }
}
