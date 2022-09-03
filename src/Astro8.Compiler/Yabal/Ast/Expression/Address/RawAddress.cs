using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public class RawAddress : IAddress
{
    private RawAddress(int value, int? length)
    {
        Value = value;
        Length = length;
    }

    public int Value { get; }

    public Either<int, InstructionPointer>? Get(YabalBuilder builder) => Value;

    public int? Length { get; }

    public int? GetValue(int offset)
    {
        return null;
    }

    public static IAddress From(int value, int? length = null) => new RawAddress(value, length);
}
