namespace Astro8;

public readonly record struct MicroInstruction
{
    private const int AluMask = 0b00000_0000_000_11;
    public static readonly MicroInstruction SU = new(0b00000_0000_000_01);
    public static readonly MicroInstruction MU = new(0b00000_0000_000_10);
    public static readonly MicroInstruction DI = new(0b00000_0000_000_11);

    private const int ReadMask = 0b00000_0000_111_00;
    public static readonly MicroInstruction RA = new(0b00000_0000_001_00);
    public static readonly MicroInstruction RB = new(0b00000_0000_010_00);
    public static readonly MicroInstruction RC = new(0b00000_0000_011_00);
    public static readonly MicroInstruction RM = new(0b00000_0000_100_00);
    public static readonly MicroInstruction IR = new(0b00000_0000_101_00);
    public static readonly MicroInstruction CR = new(0b00000_0000_110_00);
    public static readonly MicroInstruction RE = new(0b00000_0000_111_00);

    private const int WriteMask = 0b00000_1111_000_00;
    public static readonly MicroInstruction WA = new(0b00000_0001_000_00);
    public static readonly MicroInstruction WB = new(0b00000_0010_000_00);
    public static readonly MicroInstruction WC = new(0b00000_0011_000_00);
    public static readonly MicroInstruction IW = new(0b00000_0100_000_00);
    public static readonly MicroInstruction DW = new(0b00000_0101_000_00);
    public static readonly MicroInstruction WM = new(0b00000_0110_000_00);
    public static readonly MicroInstruction J  = new(0b00000_0111_000_00);
    public static readonly MicroInstruction AW = new(0b00000_1000_000_00);
    public static readonly MicroInstruction WE = new(0b00000_1001_000_00);

    private const int MiscMask = 0b11111_0000_000_00;
    public static readonly MicroInstruction EO = new(0b10000_0000_000_00);
    public static readonly MicroInstruction CE = new(0b01000_0000_000_00);
    public static readonly MicroInstruction ST = new(0b00100_0000_000_00);
    public static readonly MicroInstruction EI = new(0b00010_0000_000_00);
    public static readonly MicroInstruction FL = new(0b00001_0000_000_00);

    private readonly int _valueAlu;
    private readonly int _valueRead;
    private readonly int _valueWrite;
    private readonly int _valueMisc;
    private static MicroInstruction[]? _default;

    public MicroInstruction(int value)
    {
        Value = value;
        _valueAlu = value & AluMask;
        _valueRead = value & ReadMask;
        _valueWrite = value & WriteMask;
        _valueMisc = value & MiscMask;

        IsSU = _valueAlu == SU.Value;
        IsMU = _valueAlu == MU.Value;
        IsDI = _valueAlu == DI.Value;

        IsRA = _valueRead == RA.Value;
        IsRB = _valueRead == RB.Value;
        IsRC = _valueRead == RC.Value;
        IsRM = _valueRead == RM.Value;
        IsIR = _valueRead == IR.Value;
        IsCR = _valueRead == CR.Value;
        IsRE = _valueRead == RE.Value;

        IsWA = _valueWrite == WA.Value;
        IsWB = _valueWrite == WB.Value;
        IsWC = _valueWrite == WC.Value;
        IsIW = _valueWrite == IW.Value;
        IsDW = _valueWrite == DW.Value;
        IsWM = _valueWrite == WM.Value;
        IsJ  = _valueWrite == J.Value;
        IsAW = _valueWrite == AW.Value;
        IsWE = _valueWrite == WE.Value;

        IsEO = (_valueMisc & EO.Value) == EO.Value;
        IsCE = (_valueMisc & CE.Value) == CE.Value;
        IsST = (_valueMisc & ST.Value) == ST.Value;
        IsEI = (_valueMisc & EI.Value) == EI.Value;
        IsFL = (_valueMisc & FL.Value) == FL.Value;
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

        return new string(chars[..offset]);
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

    internal static MicroInstruction[] DefaultInstructions =>
        _default ??= HexFile.Load(typeof(HexFile).Assembly.GetManifestResourceStream("Astro8.native.microinstructions_cpu")!)
            .Select(From)
            .ToArray();
}
