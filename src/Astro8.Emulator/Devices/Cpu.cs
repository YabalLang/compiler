using System.Diagnostics;
using Astro8.Instructions;

namespace Astro8.Devices;

public partial class Cpu<THandler>
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

    public bool Step(int amount = 1)
    {
        if (_halt)
        {
            return false;
        }

        var instructions = _memory.Instruction.AsSpan();
        var memory = _memory.Data.AsSpan();

        for (var i = 0; (amount == 0 || i < amount) && !_halt; i++)
        {
            _memoryIndex = _programCounter;

            if (_memoryIndex >= instructions.Length)
            {
                _halt = true;
                return false;
            }

            _programCounter += 1;

            var context = new StepContext(
                instructions[_memoryIndex],
                memory,
                instructions,
                _memory
            );

            Step(context);
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

    private readonly ref struct StepContext
    {
        private readonly Span<InstructionReference> _instructions;
        private readonly CpuMemory<THandler> _cpuMemory;

        public StepContext(
            InstructionReference instruction,
            Span<int> memory,
            Span<InstructionReference> instructions,
            CpuMemory<THandler> cpuMemory)
        {
            Instruction = instruction;
            Memory = memory;
            _instructions = instructions;
            _cpuMemory = cpuMemory;
        }

        public InstructionReference Instruction { get; }

        public Span<int> Memory { get; }

        public void Set(int address, int value)
        {
            Memory[address] = value;
            _cpuMemory.OnChange(address, value);

            if (address < _instructions.Length)
            {
                _instructions[address] = new InstructionReference(value);
            }
        }
    }
}
