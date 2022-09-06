using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record struct Address(int Bank, int Offset);

public interface IConstantValue
{
    object? Value { get; }
}

public interface IExpressionToB
{
    void BuildExpressionToB(YabalBuilder builder);

    bool OverwritesA { get; }
}

public abstract class Pointer
{
    public abstract string? Name { get; }

    public abstract bool IsSmall { get; }

    public abstract int Bank { get; }

    public abstract int Get(IReadOnlyDictionary<InstructionPointer, int> mappings);

    public void LoadToA(YabalBuilder builder, int offset)
    {
        if (IsSmall)
        {
            builder.LoadA(this.Add(offset));
        }
        else
        {
            builder.LoadA_Large(this.Add(offset));
        }
    }

    public void CopyTo(YabalBuilder builder, Pointer pointer, int offset)
    {
        LoadToA(builder, offset);
        builder.SetComment("load value");

        if (pointer.IsSmall)
        {
            builder.StoreA(pointer.Add(offset));
        }
        else
        {
            builder.StoreA_Large(pointer.Add(offset));
        }

        builder.SetComment("store value");
    }
}

public static class PointerExtensions
{
    public static Pointer Add(this Pointer pointer, int offset)
    {
        return offset == 0 ? pointer : new PointerWithOffset(pointer, offset);
    }
}

public class AbsolutePointer : Pointer
{
    public AbsolutePointer(Address address)
    {
        Value = address;
    }

    public Address Value { get; }

    public override string Name => Value.ToString();

    public override bool IsSmall => Value.Offset < InstructionReference.MaxDataLength;

    public override int Bank => Value.Bank;

    public override int Get(IReadOnlyDictionary<InstructionPointer, int> mappings)
    {
        return Value.Offset;
    }
}

public class PointerWithOffset : Pointer
{
    private readonly Pointer _pointer;
    private readonly int _offset;

    public PointerWithOffset(Pointer pointer, int offset)
    {
        _pointer = pointer;
        _offset = offset;
    }

    public override string Name => $"{_pointer.Name}+{_offset}";

    public override bool IsSmall => _pointer.IsSmall;

    public override int Bank => _pointer.Bank;

    public override int Get(IReadOnlyDictionary<InstructionPointer, int> mappings)
    {
        var value = _pointer.Get(mappings);

        return value + _offset;
    }
}
