using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public class StringAddress : IAddress
{
    private StringAddress(string value, Pointer pointer)
    {
        Value = value;
        Pointer = pointer;
    }

    public string Value { get; }

    public Pointer? Pointer { get; }

    public int? Length => Value.Length;

    public LanguageType Type => LanguageType.Integer;

    public int? GetValue(int offset)
    {
        return offset < Value.Length ? Character.CharToInt[Value[offset]] : null;
    }

    public static IAddress From(string value, Pointer pointer) => new StringAddress(value, pointer);
}
