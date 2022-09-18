using Astro8.Instructions;

namespace Astro8.Yabal;

public class AbsolutePointer : Pointer
{
    public AbsolutePointer(int address, int bank)
    {
        Address = address;
        Bank = bank;
    }

    public override string Name => Address.ToString();

    public override bool IsSmall => Address < InstructionReference.MaxData;

    public override int Bank { get; }

    public override int Address { get; }

    public override int Get(IReadOnlyDictionary<InstructionPointer, int> mappings)
    {
        return Address;
    }

    public override string ToString()
    {
        return $"[{Address}:{Bank}]";
    }
}
