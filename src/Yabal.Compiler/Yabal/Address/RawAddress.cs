using Yabal.Ast;

namespace Yabal;

public class RawAddress : IAddress
{
    private RawAddress(int? length, LanguageType type, Pointer pointer)
    {
        Length = length;
        Type = type;
        Pointer = pointer;
    }

    public Pointer Pointer { get; }

    public int? Length { get; }

    public LanguageType Type { get; }

    public int? GetValue(int offset)
    {
        return null;
    }

    public static IAddress From(LanguageType type, Pointer pointer, int? length = null) => new RawAddress(length, type, pointer);

    public override string ToString()
    {
        return Pointer.ToString() ?? "";
    }
}
