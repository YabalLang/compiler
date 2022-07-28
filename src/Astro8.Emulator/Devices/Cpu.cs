namespace Astro8;

public class Cpu
{
    private readonly MicroInstruction[] _microInstructions;
    private readonly Memory _program;
    private readonly int[] _flags = { 0, 0, 0 };
    private int _bus;
    private int _memoryIndex;
    private int _programCounter;
    private int _instructionReg;

    public Cpu(Memory program, MicroInstruction[]? microInstructions = null)
    {
        _program = program;
        _microInstructions = microInstructions ?? MicroInstruction.DefaultInstructions;
    }

    public int A { get; set; }

    public int B { get; set; }

    public int C { get; set; }

    public int ExpansionPort { get; set; }

    public void Run()
    {
        while (Step())
        {
            // Run until we hit a halt instruction
        }
    }

    public bool Step()
    {
        for (var step = 0; step < 16; step++)
        {
            if (step == 0)
            {
                _memoryIndex = _programCounter;

                if (_memoryIndex >= _program.Length)
                {
                    return false;
                }

                _instructionReg = _program[_memoryIndex];
                _programCounter += 1;
                step = 1;
                continue;
            }

            var instruction = Instruction.From(_instructionReg);
            var offset = (instruction.MicroInstructionId * 64) + (step * 4) + (_flags[0] * 2) + _flags[1];
            var microInstruction = _microInstructions[offset];

            // Read Instruction
            if (microInstruction.IsRA)
            {
                _bus = A;
            }
            else if (microInstruction.IsRB)
            {
                _bus = B;
            }
            else if (microInstruction.IsRC)
            {
                _bus = C;
            }
            else if (microInstruction.IsRM)
            {
                _bus = _program[_memoryIndex];
            }
            else if (microInstruction.IsIR)
            {
                _bus = instruction.Data;
            }
            else if (microInstruction.IsCR)
            {
                _bus = _programCounter;
            }
            else if (microInstruction.IsRE)
            {
                _bus = ExpansionPort;
            }

            // Ungrouped
            if (microInstruction.IsEO)
            {
                if (microInstruction.IsSU)
                {
                    _flags[0] = 0;
                    _flags[1] = 1;

                    if (A - B == 0)
                    {
                        _flags[0] = 1;
                    }

                    _bus = A - B;

                    if (_bus < 0)
                    {
                        _bus = 65535 + _bus;
                        _flags[1] = 0;
                    }
                }
                else if (microInstruction.IsMU)
                {
                    _flags[0] = 0;
                    _flags[1] = 0;

                    if (A * B == 0)
                    {
                        _flags[0] = 1;
                    }

                    _bus = A * B;

                    if (_bus >= 65535)
                    {
                        _bus -= 65535;
                        _flags[1] = 1;
                    }
                }
                else if (microInstruction.IsDI)
                {
                    _flags[0] = 0;
                    _flags[1] = 0;

                    // Dont divide by zero
                    if (B != 0)
                    {
                        if (A / B == 0)
                        {
                            _flags[0] = 1;
                        }

                        _bus = A / B;
                    }
                    else
                    {
                        _flags[0] = 1;
                        _bus = 0;
                    }

                    if (_bus >= 65535)
                    {
                        _bus -= 65535;
                        _flags[1] = 1;
                    }
                }
                else
                {
                    _flags[0] = 0;
                    _flags[1] = 0;

                    if (A + B == 0)
                    {
                        _flags[0] = 1;
                    }

                    _bus = A + B;
                    if (_bus >= 65535)
                    {
                        _bus -= 65535;
                        _flags[1] = 1;
                    }
                }
            }

            // Write instructions
            if (microInstruction.IsWA)
            {
                A = _bus;
            }
            else if (microInstruction.IsWB)
            {
                B = _bus;
            }
            else if (microInstruction.IsWC)
            {
                C = _bus;
            }
            else if (microInstruction.IsIW)
            {
                _instructionReg = _bus;
            }
            else if (microInstruction.IsWM)
            {
                _program[_memoryIndex] = _bus;
            }
            else if (microInstruction.IsJ)
            {
                _programCounter = instruction.Data;
            }
            else if (microInstruction.IsAW)
            {
                _memoryIndex = _bus;
            }
            else if (microInstruction.IsWE)
            {
                ExpansionPort = _bus;
            }

            // Standalone micro instructions
            if (microInstruction.IsCE)
            {
                _programCounter += 1;
            }

            else if (microInstruction.IsST)
            {
                return false;
            }

            if (microInstruction.IsEI)
            {
                break;
            }
        }

        return true;
    }
}
