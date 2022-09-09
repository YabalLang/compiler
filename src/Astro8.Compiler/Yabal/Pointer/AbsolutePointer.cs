using Astro8.Instructions;

namespace Astro8.Yabal;

public class AbsolutePointer : Pointer
{
    private readonly int _address;

    public AbsolutePointer(int address, int bank)
    {
        _address = address;
        Bank = bank;
    }

    public override string Name => _address.ToString();

    public override bool IsSmall => _address < InstructionReference.MaxData;

    public override int Bank { get; }

    public override int Get(IReadOnlyDictionary<InstructionPointer, int> mappings)
    {
        return _address;
    }
}
