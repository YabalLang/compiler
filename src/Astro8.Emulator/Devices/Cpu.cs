﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using Astro8.Instructions;

namespace Astro8.Devices;

public sealed partial class Cpu<THandler> : IDisposable
    where THandler : Handler
{
    private readonly Stopwatch _stopwatch;
    private readonly CpuMemory<THandler>[] _banks;
    private readonly THandler _handler;
    private int _steps;
    private bool _halt;
    private CpuContext _context;

    public Cpu(CpuMemory<THandler>[] banks, THandler handler)
    {
        _banks = banks;
        _handler = handler;
        _stopwatch = Stopwatch.StartNew();
    }

    public long TotalSteps { get; set; }

    public bool Running => !_halt;

    public CpuMemory<THandler> Memory => _banks[_context.Bank];

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

    public int[] ExpansionPorts { get; } = new int[4];

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
        var cpuThread = new Thread(() =>
        {
            Run(cycleDuration, instructionsPerCycle);
        });

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
        var activeBank = _banks[activeBankId];
        var programMemory = _banks[0];
        var instructionLength = programMemory.Instruction.Length;

        fixed (int* bankPointer = activeBank.Data)
        fixed (InstructionReference* instructionPointer = programMemory.Instruction)
        {
            var context = new StepContext(
                activeBank,
                bankPointer,
                instructionPointer,
                instructionLength
            );

            try
            {
                // Store current values on the stack
                context.Cpu = _context;

                for (; i < amount && !_halt; i++)
                {
                    context.Cpu.MemoryIndex = context.Cpu.ProgramCounter;

                    if (context.Cpu.MemoryIndex >= instructionLength)
                    {
                        _halt = true;
                        break;
                    }

                    context.Cpu.ProgramCounter += 1;
                    context.Instruction = *(instructionPointer + context.Cpu.MemoryIndex);

                    Step(ref context);
                    steps++;

                    if (context.Cpu.Bank != activeBankId)
                    {
                        // Bank changed, so the pointer should be updated
                        goto fetch_memory;
                    }
                }
            }
            finally
            {
                // Restore values from the stack
                _context = context.Cpu;
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

        writer.Write(ExpansionPorts.Length);
        foreach (var value in ExpansionPorts)
        {
            writer.Write(value);
        }

        writer.Write(_halt);

        foreach (var memory in _banks)
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

        var length = reader.ReadInt32();
        for (var i = 0; i < Math.Min(length, ExpansionPorts.Length); i++)
        {
            ExpansionPorts[i] = reader.ReadInt32();
        }

        _halt = reader.ReadBoolean();

        foreach (var memory in _banks)
        {
            memory.Load(reader);
        }

        _context = CpuContext.Load(reader);
    }

    private unsafe ref struct StepContext
    {
        private readonly CpuMemory<THandler> _bank;
        private readonly int* _memoryPointer;
        private readonly InstructionReference* _instructionPointer;
        private readonly int _instructionLength;

        public StepContext(
            CpuMemory<THandler> bank,
            int* memoryPointer,
            InstructionReference* instructionPointer,
            int instructionLength)
        {
            _bank = bank;
            _memoryPointer = memoryPointer;
            _instructionPointer = instructionPointer;
            _instructionLength = instructionLength;
        }

        public CpuContext Cpu;
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
            _bank.OnChange(address, value);

            // TODO(Performance): Other than bank 0 the instructions shouldn't be updated.
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
