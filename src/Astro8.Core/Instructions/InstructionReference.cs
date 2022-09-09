namespace Astro8.Instructions;

public readonly struct InstructionReference
{
    public const int MaxId = 0b111111;
    public const int MaxData = 0b1111111111;

    public readonly int Id;
    public readonly int Data;
    public readonly int Raw;

    public InstructionReference(int raw)
    {
        Raw = raw;
        Id = raw >> 11;
        Data = raw & MaxData;
    }

    public InstructionReference(int id, int data)
    {
        Id = id;
        Data = data;
        Raw = ((id & MaxId) << 11) | (data & MaxData);
    }

    public Instruction Instruction => Instruction.Default[Id];

    public override string ToString()
    {
        return ToString(true);
    }

    public string ToString(bool withData)
    {
        var instructions = Instruction.Default;

        if (Id >= instructions.Count)
        {
            return Raw.ToString();
        }

        var instruction = instructions[Id];

        if (!withData || !instruction.MicroInstructions.Any(i => i.IsIR))
        {
            return instruction.Name;
        }

        return $"{instruction.Name} {Data}";
    }

    public static InstructionReference Create(int id, int data = 0)
    {
        return new InstructionReference(id, data);
    }

    public static implicit operator int(InstructionReference value) => value.Raw;

    public InstructionReference WithData(int value)
    {
        return new InstructionReference(Id, value);
    }
}
