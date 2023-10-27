using Yabal.Instructions;

namespace Yabal;

public abstract class Pointer
{
    public abstract string? Name { get; }

    public abstract bool IsSmall { get; }

    public abstract int Bank { get; }

    public abstract int Address { get; }

    public abstract int Get(IReadOnlyDictionary<InstructionPointer, int> mappings);

    public void CopyTo(YabalBuilder builder, Pointer pointer, int offset)
    {
        builder.LoadA(this.Add(offset));
        builder.SetComment("load value");
        builder.StoreA(pointer.Add(offset));
        builder.SetComment("store value");
    }
}
