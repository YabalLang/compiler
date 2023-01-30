using System.Diagnostics;
using System.Runtime.CompilerServices;
using Astro8.Instructions;

namespace Astro8.Devices;

public sealed partial class Cpu<THandler> : IDisposable
    where THandler : Handler
{
    private readonly Stopwatch _stopwatch;
    private readonly THandler _handler;
    private readonly Address? _keyboardAddress;
    private readonly Address? _mouseAddress;
    private int _steps;
    private bool _halt;
    private CpuContext _context;

    public Cpu(
        CpuMemory<THandler>[] banks,
        THandler handler,
        Address? keyboardAddress = null,
        Address? mouseAddress = null
    )
    {
        Banks = banks;
        _handler = handler;
        _keyboardAddress = keyboardAddress;
        _mouseAddress = mouseAddress;
        _stopwatch = Stopwatch.StartNew();
    }

    public long TotalSteps { get; set; }

    public bool Running => !_halt;

    public CpuMemory<THandler>[] Banks { get; }

    public CpuMemory<THandler> Memory => Banks[_context.Bank];

    public int A
    {
        get => _context.A;
        set => _context = _context with { A = value };
    }

    public int B
    {
        get => _context.B;
        set => _context = _context with { B = value };
    }

    public int C
    {
        get => _context.C;
        set => _context = _context with { C = value };
    }

    public int ProgramCounter
    {
        get => _context.ProgramCounter;
        set => _context = _context with { ProgramCounter = value };
    }

    public int Bank
    {
        get => _context.Bank;
        set => _context = _context with { Bank = value };
    }

    public CpuContext Context => _context;

    public void Run(int cycleDuration = 5, int instructionsPerCycle = 100)
    {
        var sw = Stopwatch.StartNew();
        while (!_halt)
        {
            Step(instructionsPerCycle);

            while (sw.ElapsedTicks < cycleDuration)
            {
                // Wait
            }

            sw.Restart();
        }
    }

    public void RunThread(int cycleDuration = 0, int instructionsPerCycle = 1)
    {
        var cpuThread = new Thread(() => { Run(cycleDuration, instructionsPerCycle); });

        cpuThread.Start();
    }

    public void Halt()
    {
        _halt = true;
    }

    public unsafe int? Step(int amount)
    {
        if (_halt)
        {
            return default;
        }

        var steps = 0;
        var i = 0;

        fetch_memory:
        var activeBankId = _context.Bank;
        var activeBank = Banks[activeBankId];
        var programMemory = Banks[0];

        fixed (int* bankPointer = activeBank.Data)
        fixed (InstructionReference* instructionPointer = programMemory.Instruction)
        {
            var context = new StepContext(
                _context,
                activeBank,
                bankPointer,
                instructionPointer
            );

            try
            {
                for (; i < amount && !_halt; i++)
                {
                    var index = context.ProgramCounter;
                    context.MemoryIndex = index;

                    if (index >= 0x3FFE)
                    {
                        _halt = true;
                        break;
                    }

                    context.ProgramCounter += 1;

                    // Get instruction
                    var instruction = instructionPointer + index;
                    var id = instruction->Id;

                    if (id == 0)
                    {
                        // Instruction has been modified in memory; recompile
                        var raw = programMemory[index];

                        if (raw == 0)
                        {
                            context.InstructionId = 0;
                            context.InstructionData = 0;
                        }
                        else
                        {
                            var instructionReference = new InstructionReference(raw);

                            context.InstructionId = instructionReference.Id;
                            context.InstructionData = instructionReference.Data;

                            *instruction = instructionReference;
                        }
                    }
                    else
                    {
                        context.InstructionId = id;
                        context.InstructionData = instruction->Data;
                    }

                    Step(ref context);
                    steps++;

                    if (context.Bank != activeBankId)
                    {
                        // Bank changed, so the pointer should be updated
                        goto fetch_memory;
                    }
                }
            }
            finally
            {
                // Restore values from the stack
                _context = context.ToCpuContext();
            }
        }

        TotalSteps += steps;

        if (_stopwatch.ElapsedMilliseconds <= 1000)
        {
            _steps += steps;
            return default;
        }

        var stepsPerSecond = (float)steps / _stopwatch.ElapsedMilliseconds * 1000;
        _handler.LogSpeed(steps, stepsPerSecond);
        _steps = 0;
        _stopwatch.Restart();

        return steps;
    }

    /// <summary>
    /// This method is generated by the source generator 'Astro8.SourceGenerator'.
    /// </summary>
    /// <param name="context"></param>
    private partial void Step(ref StepContext context);

    public void Save(Stream stream)
    {
        using var writer = new BinaryWriter(stream);
        writer.Write(A);
        writer.Write(B);
        writer.Write(C);

        writer.Write(_halt);

        foreach (var memory in Banks)
        {
            memory.Save(writer);
        }

        _context.Save(writer);
        writer.Flush();
    }

    public void Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        A = reader.ReadInt32();
        B = reader.ReadInt32();
        C = reader.ReadInt32();

        _halt = reader.ReadBoolean();

        foreach (var memory in Banks)
        {
            memory.Load(reader);
        }

        _context = CpuContext.Load(reader);
    }

    private unsafe ref struct StepContext
    {
        private readonly CpuMemory<THandler> _memory;
        private readonly int* _memoryPointer;
        private readonly InstructionReference* _instructionPointer;

        public int InstructionId;
        public int InstructionData;

        public int A;
        public int B;
        public int C;
        public int Bus;
        public int MemoryIndex;
        public bool FlagA;
        public bool FlagB;
        public int ProgramCounter;
        public int Bank;

        public StepContext(
            CpuContext context,
            CpuMemory<THandler> memory,
            int* memoryPointer,
            InstructionReference* instructionPointer)
        {
            _memory = memory;
            _memoryPointer = memoryPointer;
            _instructionPointer = instructionPointer;
            A = context.A;
            B = context.B;
            C = context.C;
            Bus = context.Bus;
            ProgramCounter = context.ProgramCounter;
            MemoryIndex = context.MemoryIndex;
            FlagA = context.FlagA;
            FlagB = context.FlagB;
            Bank = context.Bank;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int id)
        {
            return *(_memoryPointer + id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int address, int value)
        {
            *(_memoryPointer + address) = value;
            _memory.OnChange(address, value);
            *(_instructionPointer + address) = default;
        }

        public CpuContext ToCpuContext()
        {
            return new CpuContext(
                A,
                B,
                C,
                Bus,
                MemoryIndex,
                FlagA,
                FlagB,
                ProgramCounter,
                Bank
            );
        }
    }

    public void Dispose()
    {
        Halt();
    }

    public void SetKeyboard(int value)
    {
        if (_keyboardAddress is not { Bank: var bank, Offset: var offset })
        {
            return;
        }

        Banks[bank].Data[offset] = value;
    }

    public void SetMouseButton(MouseButton button, bool pressed)
    {
        if (_mouseAddress is not { Bank: var bank, Offset: var offset })
        {
            return;
        }

        var span = Banks[bank].Data.AsSpan();

        switch (button)
        {
            case MouseButton.Left when pressed:
                span[offset] |= 0b0100_0000_0000_0000;
                break;
            case MouseButton.Left:
                span[offset] &= ~0b0100_0000_0000_0000;
                break;

            case MouseButton.Right when pressed:
                span[offset] |= 0b1000_0000_0000_0000;
                break;
            case MouseButton.Right:
                span[offset] &= ~0b1000_0000_0000_0000;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(button), button, null);
        }
    }

    public void SetMousePosition(int x, int y)
    {
        if (_mouseAddress is not { Bank: var bank, Offset: var offset })
        {
            return;
        }

        var span = Banks[bank].Data.AsSpan();
        var value = span[offset];

        span[offset] = ((x & 0b1111111) << 7) | (y & 0b1111111) | (value & 0b1100_0000_0000_0000);
    }
}

public enum MouseButton
{
    Left,
    Right
}
