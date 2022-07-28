using System.Diagnostics.CodeAnalysis;

namespace Astro8;

public readonly struct Either<TLeft, TRight>
    where TLeft : notnull
    where TRight : notnull
{
    private Either(TLeft left)
    {
        Left    = left;
        Right   = default;
        IsRight = false;
    }

    private Either(TRight right)
    {
        Left    = default;
        Right   = right;
        IsRight = true;
    }

    [MemberNotNullWhen(true, nameof(Right))]
    [MemberNotNullWhen(false, nameof(Left))]
    public bool IsRight { get; }

    public TRight? Right { get; }

    [MemberNotNullWhen(true, nameof(Left))]
    [MemberNotNullWhen(false, nameof(Right))]
    public bool IsLeft => !IsRight;

    public TLeft? Left { get; }

    public static implicit operator Either<TLeft, TRight>(TLeft left) => new(left);
    public static implicit operator Either<TLeft, TRight>(TRight right) => new(right);
}

public class InstructionLabel
{
    private readonly InstructionBuilder _builder;

    public InstructionLabel(InstructionBuilder builder, string? name)
    {
        _builder = builder;
        Name = name;
    }

    public string? Name { get; }

    public void Mark()
    {
        _builder.Mark(this);
    }
}

public class InstructionBuilder
{
    private record struct InstructionItem(Instruction Instruction, InstructionLabel? Label);

    private readonly List<Either<InstructionLabel, InstructionItem>> _instructions = new();

    public int Count => _instructions.Count;

    public InstructionLabel CreateLabel(string? name = null)
    {
        return new InstructionLabel(this, name);
    }

    public void Mark(InstructionLabel label)
    {
        _instructions.Add(label);
    }

    public InstructionBuilder Emit(int id, int data = 0)
    {
        if (id > Instruction.MaxInstructionId)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (data > Instruction.MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        _instructions.Add(new InstructionItem(Instruction.Create(id, data), null));
        return this;
    }

    public InstructionBuilder Emit(int id, InstructionLabel label)
    {
        _instructions.Add(new InstructionItem(Instruction.Create(id), label));
        return this;
    }

    public InstructionBuilder EmitRaw(int value)
    {
        _instructions.Add(new InstructionItem(value, null));
        return this;
    }

    public InstructionBuilder Nop() => Emit(Instruction.NOP);

    public InstructionBuilder LoadA(int address) => Emit(Instruction.AIN, address);

    public InstructionBuilder LoadB(int address) => Emit(Instruction.BIN, address);

    public InstructionBuilder LoadC(int address) => Emit(Instruction.CIN, address);

    public InstructionBuilder SetA(int value) => Emit(Instruction.LDIA, value);

    public InstructionBuilder SetB(int value) => Emit(Instruction.LDIB, value);

    public InstructionBuilder LoadB_FromExpansion() => Emit(Instruction.RDEXP);

    public InstructionBuilder LoadA_FromExpansion() => Emit(Instruction.WREXP);

    public InstructionBuilder StoreA(int address) => Emit(Instruction.STA, address);

    public InstructionBuilder StoreC(int address) => Emit(Instruction.STC, address);

    public InstructionBuilder Add() => Emit(Instruction.ADD);

    public InstructionBuilder Sub() => Emit(Instruction.SUB);

    public InstructionBuilder Mult() => Emit(Instruction.MULT);

    public InstructionBuilder Div() => Emit(Instruction.DIV);

    public InstructionBuilder Jump(int counter) => Emit(Instruction.JMP, counter);

    public InstructionBuilder Jump(InstructionLabel counter) => Emit(Instruction.JMP, counter);

    public InstructionBuilder JumpIfAZero(int counter) => Emit(Instruction.JMPZ, counter);

    public InstructionBuilder JumpIfAZero(InstructionLabel counter) => Emit(Instruction.JMPZ, counter);

    public InstructionBuilder JumpIfCarryBit(int counter) => Emit(Instruction.JMPC, counter);

    public InstructionBuilder JumpIfCarryBit(InstructionLabel counter) => Emit(Instruction.JMPC, counter);

    public InstructionBuilder LoadA() => Emit(Instruction.LDAIN);

    public InstructionBuilder StoreB_ToAddressUsingA() => Emit(Instruction.STAOUT);

    public InstructionBuilder StoreA_LargeAddress(int address)
    {
        Emit(Instruction.STLGE);
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder LoadA_LargeAddress(int address)
    {
        Emit(Instruction.LDLGE);
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder SwapA_B() => Emit(Instruction.SWP);

    public InstructionBuilder SwapA_C() => Emit(Instruction.SWPC);

    public InstructionBuilder Halt() => Emit(Instruction.HLT);

    public int[] ToArray()
    {
        var array = new int[_instructions.Count];
        var labels = new Dictionary<InstructionLabel, int>();
        var i = 0;

        foreach (var either in _instructions)
        {
            if (either is { IsLeft: true })
            {
                labels[either.Left] = i;
            }
            else
            {
                i++;
            }
        }

        i = 0;

        foreach (var either in _instructions)
        {
            if (either is { IsLeft: true })
            {
                continue;
            }

            var (instruction, label) = either.Right;

            if (label is null)
            {
                array[i] = instruction;
            }
            else if (labels.TryGetValue(label, out var index))
            {
                array[i] = instruction with
                {
                    Data = index
                };
            }
            else
            {
                throw new InvalidOperationException("Label not found");
            }

            i++;
        }

        return array;
    }
}
