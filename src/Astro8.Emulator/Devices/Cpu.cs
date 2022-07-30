using System.Diagnostics;

namespace Astro8;

public class Cpu
{
    private readonly MicroInstruction[] _microInstructions;
    private readonly Memory _memory;
    private readonly int[] _flags = { 0, 0, 0 };
    private int _bus;
    private int _memoryIndex;
    private int _programCounter;
    private int _instructionReg;
    private bool _halt;

    public Cpu(Memory memory, MicroInstruction[]? microInstructions = null)
    {
        _memory = memory;
        _microInstructions = microInstructions ?? MicroInstruction.DefaultInstructions;
    }

    public bool Running => !_halt;

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

    public void RunThread(int tickDuration = 10)
    {
        var cpuThread = new Thread(() =>
        {
            var sw = Stopwatch.StartNew();
            while (Step())
            {
                while (tickDuration > 0 && sw.ElapsedTicks < tickDuration)
                {
                    // Wait
                }
                sw.Restart();
            }
        });

        cpuThread.Start();
    }

    public void Halt()
    {
        _halt = true;
    }

    public bool Step()
    {
        if (_halt)
        {
            return false;
        }

        for (var step = 0; step < 16; step++)
        {
            if (step == 0)
            {
                _memoryIndex = _programCounter;

                if (_memoryIndex >= _memory.Length)
                {
                    _halt = true;
                    return false;
                }

                _instructionReg = _memory[_memoryIndex];
                _programCounter += 1;
                step = 1;
                continue;
            }

            var instruction = InstructionReference.From(_instructionReg);
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
                _bus = _memory[_memoryIndex];
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
                ExpansionPort = 0;
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
                _memory[_memoryIndex] = _bus;
            }
            else if (microInstruction.IsJ)
            {
                _programCounter = _bus;
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
                _halt = true;
                return false;
            }

            if (microInstruction.IsEI)
            {
                break;
            }
        }

        return true;
    }

    public void Save(Stream stream)
    {
        using var writer = new BinaryWriter(stream);
        writer.Write(A);
        writer.Write(B);
        writer.Write(C);
        writer.Write(ExpansionPort);
        writer.Write(_bus);
        writer.Write(_memoryIndex);
        writer.Write(_programCounter);
        writer.Write(_instructionReg);
        writer.Write(_flags[0]);
        writer.Write(_flags[1]);
        writer.Write(_flags[2]);
        writer.Write(_halt);
        _memory.Save(writer);
        writer.Flush();
    }

    public void Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        A = reader.ReadInt32();
        B = reader.ReadInt32();
        C = reader.ReadInt32();
        ExpansionPort = reader.ReadInt32();
        _bus = reader.ReadInt32();
        _memoryIndex = reader.ReadInt32();
        _programCounter = reader.ReadInt32();
        _instructionReg = reader.ReadInt32();
        _flags[0] = reader.ReadInt32();
        _flags[1] = reader.ReadInt32();
        _flags[2] = reader.ReadInt32();
        _halt = reader.ReadBoolean();
        _memory.Load(reader);
    }
}
