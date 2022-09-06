using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public class RawAddress : IAddress
{
    private RawAddress(int value, int? length, LanguageType type)
    {
        Value = value;
        Length = length;
        Type = type;
    }

    public int Value { get; }

    public Either<int, InstructionPointer>? Get(YabalBuilder builder) => Value;

    public int? Length { get; }

    public LanguageType Type { get; }

    public int? GetValue(int offset)
    {
        return null;
    }

    public static IAddress From(int value, LanguageType type, int? length = null) => new RawAddress(value, length, type);
}
