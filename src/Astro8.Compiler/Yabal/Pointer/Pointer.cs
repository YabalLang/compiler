using Astro8.Instructions;

namespace Astro8.Yabal;

public abstract class Pointer
{
    public abstract string? Name { get; }

    public abstract bool IsSmall { get; }

    public abstract int Bank { get; }

    public abstract int Get(IReadOnlyDictionary<InstructionPointer, int> mappings);

    public void LoadToA(YabalBuilder builder, int offset = 0)
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

    public void StoreA(YabalBuilder builder, int offset = 0)
    {
        if (IsSmall)
        {
            builder.StoreA(this.Add(offset));
        }
        else
        {
            builder.StoreA_Large(this.Add(offset));
        }
    }

    public void CopyTo(YabalBuilder builder, Pointer pointer, int offset)
    {
        LoadToA(builder, offset);
        builder.SetComment("load value");

        if (pointer.Bank == 0 && pointer.IsSmall)
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
