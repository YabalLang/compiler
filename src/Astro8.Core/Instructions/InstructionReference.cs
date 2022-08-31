using System.Runtime.InteropServices;

namespace Astro8.Instructions;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public readonly record struct InstructionReference
{
    public const int MaxInstructionId = 63;
    public const int MaxDataLength = 2047;

    [FieldOffset(0)] private readonly int _id;
    [FieldOffset(4)] private readonly int _data;
    [FieldOffset(8)] private readonly int _raw;

    public InstructionReference(int raw)
    {
        _raw = raw;
        _id = BitRange(raw, 11, 5);
        _data = BitRange(raw, 0, 11);
    }

    public int Id
    {
        get => _id;
        init
        {
            _id = value;
            Raw = (Id << 11) | Data;
        }
    }

    public int Data
    {
        get => _data;
        init
        {
            _data = value;
            Raw = (_id << 11) | value;
        }
    }

    public int Raw
    {
        get => _raw;
        init
        {
            _raw = value;
            _id = BitRange(value, 11, 5);
            _data = BitRange(value, 0, 11);
        }
    }

    public override string ToString()
    {
        var instructions = Instruction.Default;

        if (Id >= instructions.Count)
        {
            return Raw.ToString();
        }

        var instruction = instructions[Id];

        if (!instruction.MicroInstructions.Any(i => i.IsIR))
        {
            return instruction.Name;
        }

        return $"{instruction.Name} {Data}";
    }

    public static int BitRange(int value, int offset, int n)
    {
        value >>= offset;
        var mask = (1 << n) - 1;
        return value & mask;
    }

    public static InstructionReference Create(int id, int data = 0)
    {
        return new InstructionReference((id << 11) | data);
    }

    public static implicit operator int(InstructionReference value) => value.Raw;
}
