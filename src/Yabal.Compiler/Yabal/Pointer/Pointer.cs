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

        if (pointer is { Bank: 0, IsSmall: true })
        {
            builder.StoreA(pointer.Add(offset));
        }
        else if (pointer.Bank == 0)
        {
            builder.StoreA_Large(pointer.Add(offset));
        }
        else
        {
            builder.SwapA_B();
            builder.SetA_Large(pointer.Add(offset));

            if (pointer.Bank > 0) builder.SetBank(pointer.Bank);
            builder.StoreB_ToAddressInA();
            if (pointer.Bank > 0) builder.SetBank(0);
        }

        builder.SetComment("store value");
    }
}
