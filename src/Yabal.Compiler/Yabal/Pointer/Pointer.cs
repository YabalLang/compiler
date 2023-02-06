using Yabal.Instructions;

namespace Yabal;

public abstract class Pointer
{
    public abstract string? Name { get; }

    public abstract bool IsSmall { get; }

    public abstract int Bank { get; }

    public abstract int Address { get; }

    public abstract int Get(IReadOnlyDictionary<InstructionPointer, int> mappings);

    public void LoadToA(YabalBuilder builder, int offset = 0)
    {
        if (IsSmall)
        {
            if (Bank > 0) builder.SetBank(Bank);
            builder.LoadA(this.Add(offset));
            if (Bank > 0) builder.SetBank(0);
        }
        else if (Bank > 0)
        {
            // When switching banks you cannot use LDLGE since this instruction reads the large value from the current bank
            // so we need to store the value in the register before switching banks

            builder.SetA_Large(this.Add(offset));
            builder.SetBank(Bank);
            builder.LoadA_FromAddressUsingA();
            builder.SetBank(0);
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
            if (Bank > 0) builder.SetBank(Bank);
            builder.StoreA(this.Add(offset));
            if (Bank > 0) builder.SetBank(0);
        }
        else if (Bank > 0)
        {
            // When switching banks you cannot use STAOUT since this instruction reads the large value from the current bank
            // so we need to store the value in the register before switching banks

            builder.SwapA_B();
            builder.SetA_Large(this.Add(offset));
            builder.SetBank(Bank);
            builder.StoreB_ToAddressInA();
            builder.SetBank(0);
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
