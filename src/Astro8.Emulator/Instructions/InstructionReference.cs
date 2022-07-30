namespace Astro8;

public readonly record struct InstructionReference(int Raw)
{
    public const int MaxInstructionId = 0b111111;
    public const int MaxDataLength = 2047;

    public const int NOP    = 0b00000;
    public const int AIN    = 0b00001;
    public const int BIN    = 0b00010;
    public const int CIN    = 0b00011;
    public const int LDIA   = 0b00100;
    public const int LDIB   = 0b00101;
    public const int RDEXP  = 0b00110;
    public const int WREXP  = 0b00111;
    public const int STA    = 0b01000;
    public const int STC    = 0b01001;
    public const int ADD    = 0b01010;
    public const int SUB    = 0b01011;
    public const int MULT   = 0b01100;
    public const int DIV    = 0b01101;
    public const int JMP    = 0b01110;
    public const int JMPZ   = 0b01111;
    public const int JMPC   = 0b10000;
    public const int LDAIN  = 0b10001;
    public const int STAOUT = 0b10010;
    public const int LDLGE  = 0b10011;
    public const int STLGE  = 0b10100;
    public const int SWP    = 0b10101;
    public const int SWPC   = 0b10110;
    public const int HLT    = 0b10111;
    public const int OUT    = 0b11000;

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
