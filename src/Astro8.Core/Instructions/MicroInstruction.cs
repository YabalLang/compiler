namespace Astro8.Instructions;

public readonly record struct MicroInstruction
{
    private const int AluMask = 0b0000000000001111;
    public const int SU = 0b0000000000000001;
    public const int MU = 0b0000000000000010;
    public const int DI = 0b0000000000000011;
    public const int SL = 0b0000000000000100;
    public const int SR = 0b0000000000000101;
    public const int AND = 0b0000000000000110;
    public const int OR = 0b0000000000000111;
    public const int NOT = 0b0000000000001000;

    private const int ReadMask = 0b0000000001110000;
    public const int RA = 0b0000000000010000;
    public const int RB = 0b0000000000100000;
    public const int RC = 0b0000000000110000;
    public const int RM = 0b0000000001000000;
    public const int IR = 0b0000000001010000;
    public const int CR = 0b0000000001100000;
    public const int RE = 0b0000000001110000;

    private const int WriteMask = 0b0000011110000000;
    public const int WA =  0b0000000010000000;
    public const int WB =  0b0000000100000000;
    public const int WC =  0b0000000110000000;
    public const int IW =  0b0000001000000000;
    public const int DW =  0b0000001010000000;
    public const int WM =  0b0000001100000000;
    public const int J  =  0b0000001110000000;
    public const int AW =  0b0000010000000000;
    public const int WE =  0b0000010010000000;
    public const int BNK = 0b0000010100000000;

    private const int MiscMask = 0b1111100000000000;
    public const int FL = 0b0000100000000000;
    public const int EI = 0b0001000000000000;
    public const int ST = 0b0010000000000000;
    public const int CE = 0b0100000000000000;
    public const int EO = 0b1000000000000000;

    public static readonly IReadOnlyDictionary<string, MicroInstruction> All =
        new Dictionary<string, MicroInstruction>(StringComparer.OrdinalIgnoreCase)
        {
            ["SU"] = SU,
            ["MU"] = MU,
            ["DI"] = DI,
            ["RA"] = RA,
            ["RB"] = RB,
            ["RC"] = RC,
            ["RM"] = RM,
            ["IR"] = IR,
            ["CR"] = CR,
            ["RE"] = RE,
            ["WA"] = WA,
            ["WB"] = WB,
            ["WC"] = WC,
            ["IW"] = IW,
            ["DW"] = DW,
            ["WM"] = WM,
            ["J"] = J,
            ["AW"] = AW,
            ["WE"] = WE,
            ["EO"] = EO,
            ["CE"] = CE,
            ["ST"] = ST,
            ["EI"] = EI,
            ["FL"] = FL,
            ["NOT"] = NOT,
            ["AND"] = AND,
            ["OR"] = OR,
            ["SL"] = SL,
            ["SR"] = SR,
            ["BNK"] = BNK,
        };

    private readonly int _valueAlu;
    private readonly int _valueRead;
    private readonly int _valueWrite;
    private readonly int _valueMisc;
    private static MicroInstruction[]? _default;

    private MicroInstruction(int value)
    {
        Value = value;
        _valueAlu = value & AluMask;
        _valueRead = value & ReadMask;
        _valueWrite = value & WriteMask;
        _valueMisc = value & MiscMask;

        IsSU = _valueAlu == SU;
        IsMU = _valueAlu == MU;
        IsDI = _valueAlu == DI;
        IsSR = _valueAlu == SR;
        IsSL = _valueAlu == SL;
        IsAND = _valueAlu == AND;
        IsOR = _valueAlu == OR;
        IsNOT = _valueAlu == NOT;

        IsRA = _valueRead == RA;
        IsRB = _valueRead == RB;
        IsRC = _valueRead == RC;
        IsRM = _valueRead == RM;
        IsIR = _valueRead == IR;
        IsCR = _valueRead == CR;
        IsRE = _valueRead == RE;

        IsWA = _valueWrite == WA;
        IsWB = _valueWrite == WB;
        IsWC = _valueWrite == WC;
        IsIW = _valueWrite == IW;
        IsDW = _valueWrite == DW;
        IsWM = _valueWrite == WM;
        IsJ  = _valueWrite == J;
        IsAW = _valueWrite == AW;
        IsWE = _valueWrite == WE;
        IsBNK = _valueWrite == BNK;

        IsEO = (_valueMisc & EO) == EO;
        IsCE = (_valueMisc & CE) == CE;
        IsST = (_valueMisc & ST) == ST;
        IsEI = (_valueMisc & EI) == EI;
        IsFL = (_valueMisc & FL) == FL;
    }

    public int Value { get; }

    #region Alu Instruction Special Address

    /// <summary>
    /// Enable subtraction in ALU
    /// </summary>
    public bool IsSU { get; }

    /// <summary>
    /// Enable multiplication in ALU
    /// </summary>
    public bool IsMU { get; }

    /// <summary>
    /// Enable divion in ALU
    /// </summary>
    public bool IsDI { get; }

    /// <summary>
    /// Bit shift left
    /// </summary>
    public bool IsSL { get; }

    /// <summary>
    /// Bit shift right
    /// </summary>
    public bool IsSR { get; }

    /// <summary>
    /// Bitwise AND
    /// </summary>
    public bool IsAND { get; }

    /// <summary>
    /// Bitwise OR
    /// </summary>
    public bool IsOR { get; }

    /// <summary>
    /// Bitwise NOT
    /// </summary>
    public bool IsNOT { get; }

    #endregion

    #region Read Instruction Special Address

    /// <summary>
    /// Read from reg A to bus
    /// </summary>
    public bool IsRA { get; }

    /// <summary>
    /// Read from reg B to bus
    /// </summary>
    public bool IsRB  { get; }

    /// <summary>
    /// Read from reg C to bus
    /// </summary>
    public bool IsRC { get; }

    /// <summary>
    /// Read from memory to bus at the address in mem addr. regter
    /// </summary>
    public bool IsRM { get; }

    /// <summary>
    /// Read from lowest 12 bits of instruction regter to bus
    /// </summary>
    public bool IsIR { get; }

    /// <summary>
    /// Read value from counter to bus
    /// </summary>
    public bool IsCR { get; }

    /// <summary>
    /// Read from expansion port to bus
    /// </summary>
    public bool IsRE { get; }

    #endregion

    #region Write Instruction Special Address

    /// <summary>
    /// Write from bus to reg A
    /// </summary>
    public bool IsWA { get; }

    /// <summary>
    /// Write from bus to reg B
    /// </summary>
    public bool IsWB { get; }

    /// <summary>
    /// Write from bus to reg C
    /// </summary>
    public bool IsWC { get; }

    /// <summary>
    /// Write from bus to instruction regter
    /// </summary>
    public bool IsIW { get; }

    /// <summary>
    /// Write from bus to dplay regter
    /// </summary>
    public bool IsDW { get; }

    /// <summary>
    /// Write from bus to memory
    /// </summary>
    public bool IsWM { get; }

    /// <summary>
    /// Write from bus to counter current value
    /// </summary>
    public bool IsJ { get; }

    /// <summary>
    /// Write lowest 12 bits from bus to mem. addr. regter
    /// </summary>
    public bool IsAW { get; }

    /// <summary>
    /// Write from bus to expansion port
    /// </summary>
    public bool IsWE { get; }

    /// <summary>
    /// Change bank, changes the memory bank register to the value
    /// </summary>
    public bool IsBNK { get; }

    #endregion

    #region Micro instructions

    /// <summary>
    /// Read from ALU to bus
    /// </summary>
    public bool IsEO { get; }

    /// <summary>
    /// Enable incrementing of counter
    /// </summary>
    public bool IsCE { get; }

    /// <summary>
    /// Stop the clock
    /// </summary>
    public bool IsST { get; }

    /// <summary>
    /// End instruction; resets step counter to move to next instruction
    /// </summary>
    public bool IsEI { get; }

    /// <summary>
    /// Update flags regter
    /// </summary>
    public bool IsFL { get; }

    #endregion

    public override string ToString()
    {
        Span<char> chars = stackalloc char[64];
        var offset = 0;

        if (IsDI) Write(ref chars, ref offset, 'D', 'I');
        else if (IsMU) Write(ref chars, ref offset, 'M', 'U');
        else if (IsSU) Write(ref chars, ref offset, 'S', 'U');

        if (IsRE) Write(ref chars, ref offset, 'R', 'E');
        else if (IsCR) Write(ref chars, ref offset, 'C', 'R');
        else if (IsIR) Write(ref chars, ref offset, 'I', 'R');
        else if (IsRC) Write(ref chars, ref offset, 'R', 'C');
        else if (IsRM) Write(ref chars, ref offset, 'R', 'M');
        else if (IsRB) Write(ref chars, ref offset, 'R', 'B');
        else if (IsRA) Write(ref chars, ref offset, 'R', 'A');

        if (IsJ) Write(ref chars, ref offset, 'J', '\0');
        else if (IsWM) Write(ref chars, ref offset, 'W', 'M');
        else if (IsDW) Write(ref chars, ref offset, 'D', 'W');
        else if (IsWC) Write(ref chars, ref offset, 'W', 'C');
        else if (IsWA) Write(ref chars, ref offset, 'W', 'A');
        else if (IsWB) Write(ref chars, ref offset, 'W', 'B');
        else if (IsIW) Write(ref chars, ref offset, 'I', 'W');
        else if (IsAW) Write(ref chars, ref offset, 'A', 'W');
        else if (IsWE) Write(ref chars, ref offset, 'W', 'E');

        if (IsEO) Write(ref chars, ref offset, 'E', 'O');
        if (IsCE) Write(ref chars, ref offset, 'C', 'E');
        if (IsST) Write(ref chars, ref offset, 'S', 'T');
        if (IsEI) Write(ref chars, ref offset, 'E', 'I');
        if (IsFL) Write(ref chars, ref offset, 'F', 'L');

#if NETSTANDARD2_0
        return new string(chars.Slice(0, offset).ToArray());
#else
        return new string(chars[..offset]);
#endif
    }

    private static void Write(ref Span<char> chars, ref int offset, char a, char b)
    {
        if (offset > 0)
        {
            chars[offset++] = ' ';
            chars[offset++] = '|';
            chars[offset++] = ' ';
        }

        chars[offset++] = a;

        if (b is not '\0')
        {
            chars[offset++] = b;
        }
    }

    public static MicroInstruction From(int value) => new(value);

    public static implicit operator MicroInstruction(int value) => From(value);

    public static MicroInstruction operator &(MicroInstruction a, MicroInstruction b) => new(a.Value & b.Value);

    public static MicroInstruction operator |(MicroInstruction a, MicroInstruction b) => new(a.Value | b.Value);

    public static MicroInstruction[] Parse(string content)
    {
        var values = new int[4096];
        HexFile.Load(content, values);

        var instructions = new MicroInstruction[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            instructions[i] = new MicroInstruction(values[i]);
        }

        return instructions;
    }

    public static MicroInstruction[] Parse(IReadOnlyList<Instruction> instructions, int length = 32)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (instructions.Count == 0)
        {
            return Array.Empty<MicroInstruction>();
        }

        if (instructions.Count > length)
        {
            throw new ArgumentException($"Instruction count {instructions.Count} exceeds maximum length {length}");
        }

        var microInstructions = new MicroInstruction[length * 64];
        var span = microInstructions.AsSpan();

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            var offset = i * 64;

            instruction.MicroInstructions.CopyTo(span.Slice(offset, 64));
        }

        for (var i = instructions.Count; i < length; i++)
        {
            instructions[0].MicroInstructions.CopyTo(span.Slice(i * 64, 64));
        }

        return microInstructions;
    }

    internal static MicroInstruction[] Default
    {
        get => _default ??= Parse(Instruction.Default);
        set => _default = value;
    }
}
