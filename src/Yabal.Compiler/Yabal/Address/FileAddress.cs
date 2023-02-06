using Yabal.Ast;
using Yabal.Instructions;

namespace Yabal;

public class FileAddress : IAddress
{
    private readonly string _path;
    private readonly FileType _type;
    private FileContent? _content;

    public FileAddress(string path, FileType type, InstructionPointer pointer)
    {
        _path = path;
        _type = type;
        Pointer = pointer;
    }

    Pointer IAddress.Pointer => Pointer;

    public InstructionPointer Pointer { get; }

    public FileContent Content
    {
        get => _content ?? throw new InvalidOperationException("File content not loaded");
        set => _content = value;
    }

    public int? Length
    {
        get
        {
            var (offset, bytes) = Content;
            return bytes.Length - offset;
        }
    }

    public LanguageType Type => LanguageType.Integer;

    public int? GetValue(int offset)
    {
        var (contentOffset, bytes) = Content;
        return bytes[contentOffset + offset];
    }

    public static IAddress From(string path, FileType type, InstructionPointer pointer) => new FileAddress(path, type, pointer);

    public override string ToString()
    {
        return Pointer.ToString() ?? "";
    }
}
