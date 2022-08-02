using System.Diagnostics;
using System.Runtime.CompilerServices;
using Astro8.Instructions;

namespace Astro8.Devices;

public sealed partial class Cpu<THandler> : IDisposable
    where THandler : Handler
{
    private readonly CpuMemory<THandler> _memory;
    private int _bus;
    private int _memoryIndex;
    private int _programCounter;
    private bool _flagA;
    private bool _flagB;
    private bool _halt;

    public Cpu(CpuMemory<THandler> memory)
    {
        _memory = memory;
    }

    public bool Running => !_halt;

    public int A { get; set; }

    public int B { get; set; }

    public int C { get; set; }

    public int ExpansionPort { get; set; }

    public void Run()
    {
        Step(0);
    }

    public void RunThread(int tickDuration = 10)
    {
        var cpuThread = new Thread(() =>
        {
            Step(0);
        });

        cpuThread.Start();
    }

    public void Halt()
    {
        _halt = true;
    }

    public unsafe void Step(int amount = 1)
    {
        if (_halt)
        {
            return;
        }

        var instructionLength = _memory.Instruction.Length;

        fixed (int* dataPointer = _memory.Data)
        fixed (InstructionReference* instructionPointer = _memory.Instruction)
        {
            var context = new StepContext(
                _memory,
                dataPointer,
                instructionPointer,
                instructionLength
            );

            // Store current values on the stack
            context.A = A;
            context.B = B;
            context.C = C;
            context.FlagA = _flagA;
            context.FlagB = _flagB;
            context.Bus = _bus;

            for (var i = 0; (amount == 0 || i < amount) && !_halt; i++)
            {
                _memoryIndex = _programCounter;

                if (_memoryIndex >= instructionLength)
                {
                    _halt = true;
                    break;
                }

                _programCounter += 1;
                context.Instruction = *(instructionPointer + _memoryIndex);

                Step(ref context);
            }

            // Restore values from the stack
            A = context.A;
            B = context.B;
            C = context.C;
            _flagA = context.FlagA;
            _flagB = context.FlagB;
            _bus = context.Bus;
        }
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
        writer.Write(_flagA);
        writer.Write(_flagB);
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
        _flagA = reader.ReadBoolean();
        _flagB = reader.ReadBoolean();
        _halt = reader.ReadBoolean();
        _memory.Load(reader);
    }

    private unsafe ref struct StepContext
    {
        private readonly CpuMemory<THandler> _cpuMemory;
        private readonly int* _memoryPointer;
        private readonly InstructionReference* _instructionPointer;
        private readonly int _instructionLength;

        public StepContext(
            CpuMemory<THandler> cpuMemory,
            int* memoryPointer,
            InstructionReference* instructionPointer,
            int instructionLength)
        {
            _cpuMemory = cpuMemory;
            _memoryPointer = memoryPointer;
            _instructionPointer = instructionPointer;
            _instructionLength = instructionLength;
        }

        public int A;
        public int B;
        public int C;
        public int Bus;
        public bool FlagA;
        public bool FlagB;
        public InstructionReference Instruction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int id)
        {
            return *(_memoryPointer + id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int address, int value)
        {
            *(_memoryPointer + address) = value;
            _cpuMemory.OnChange(address, value);

            if (address < _instructionLength)
            {
                *(_instructionPointer + address) = new InstructionReference(value);
            }
        }
    }

    public void Dispose()
    {
        Halt();
    }
}
