namespace Astro8;

public readonly record struct Instruction(int Raw)
{
    public const int MaxInstructionId = 63;
    public const int MaxDataLength = 2047;

    public const int NOP = 0;
    public const int AIN = 1;
    public const int BIN = 2;
    public const int CIN = 3;
    public const int LDIA = 4;
    public const int LDIB = 5;
    public const int RDEXP = 6;
    public const int WREXP = 7;
    public const int STA = 8;
    public const int STC = 9;
    public const int ADD = 10;
    public const int SUB = 11;
    public const int MULT = 12;
    public const int DIV = 13;
    public const int JMP = 14;
    public const int JMPZ = 15;
    public const int JMPC = 16;
    public const int LDAIN = 17;
    public const int STAOUT = 18;
    public const int LDLGE = 19;
    public const int STLGE = 20;
    public const int SWP = 21;
    public const int SWPC = 22;
    public const int HLT = 23;
    public const int OUT = 24;

    private readonly int _microInstructionId = BitRange(Raw, 11, 5);

    public int MicroInstructionId
    {
        get => _microInstructionId;
        init => Raw = (value << 11) | Data;
    }

    public int Data
    {
        get => BitRange(Raw, 0, 11);
        init => Raw = (MicroInstructionId << 11) | value;
    }

    public override string ToString()
    {
        return MicroInstructionId switch
        {
            0 => nameof(NOP),
            1 => nameof(AIN),
            2 => nameof(BIN),
            3 => nameof(CIN),
            4 => nameof(LDIA),
            5 => nameof(LDIB),
            6 => nameof(RDEXP),
            7 => nameof(WREXP),
            8 => nameof(STA),
            9 => nameof(STC),
            10 => nameof(ADD),
            11 => nameof(SUB),
            12 => nameof(MULT),
            13 => nameof(DIV),
            14 => nameof(JMP),
            15 => nameof(JMPZ),
            16 => nameof(JMPC),
            17 => nameof(LDAIN),
            18 => nameof(STAOUT),
            19 => nameof(LDLGE),
            20 => nameof(STLGE),
            21 => nameof(SWP),
            22 => nameof(SWPC),
            23 => nameof(HLT),
            24 => nameof(OUT),
            _ => "UNKNOWN",
        };
    }

    private static int BitRange(int value, int offset, int n)
    {
        value >>= offset;
        var mask = (1 << n) - 1;
        return value & mask;
    }

    public static Instruction From(int value)
    {
        return new Instruction(value);
    }

    public static Instruction Create(int id, int data = 0)
    {
        return new Instruction((id << 11) | data);
    }

    public static implicit operator Instruction(int value) => From(value);
    public static implicit operator int(Instruction value) => value.Raw;
}
