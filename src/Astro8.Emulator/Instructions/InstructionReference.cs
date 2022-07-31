namespace Astro8.Instructions;

public readonly record struct InstructionReference(int Raw)
{
    public const int MaxInstructionId = 0b111111;
    public const int MaxDataLength = 2047;

    private readonly int _microInstructionId = BitRange(Raw, 11, 5);
    private readonly int _data = BitRange(Raw, 0, 11);

    public int MicroInstructionId
    {
        get => _microInstructionId;
        init => Raw = (value << 11) | _data;
    }

    public int Data
    {
        get => _data;
        init => Raw = (_microInstructionId << 11) | value;
    }

    public override string ToString()
    {
        var instructions = Instruction.Default;

        if (MicroInstructionId >= instructions.Count)
        {
            return Raw.ToString();
        }

        var instruction = instructions[MicroInstructionId].Name;

        if (_data == 0)
        {
            return $"{instruction}";
        }

        return $"{instruction} {Data}";
    }

    public static int BitRange(int value, int offset, int n)
    {
        value >>= offset;
        var mask = (1 << n) - 1;
        return value & mask;
    }

    public static InstructionReference From(int value)
    {
        return new InstructionReference(value);
    }

    public static InstructionReference Create(int id, int data = 0)
    {
        return new InstructionReference((id << 11) | data);
    }

    public static implicit operator InstructionReference(int value) => From(value);

    public static implicit operator int(InstructionReference value) => value.Raw;
}
