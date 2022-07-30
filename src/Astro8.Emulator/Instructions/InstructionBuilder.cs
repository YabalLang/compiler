using System.Diagnostics.CodeAnalysis;
using System.Text;
using PointerOrData = Astro8.Either<Astro8.InstructionPointer, int>;

namespace Astro8;

public readonly struct Either<TLeft, TRight> : IEquatable<Either<TLeft, TRight>>
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

    public override string? ToString()
    {
        return IsRight ? Right.ToString() : Left.ToString();
    }

    public bool Equals(Either<TLeft, TRight> other)
    {
        return IsRight == other.IsRight &&
               EqualityComparer<TRight?>.Default.Equals(Right, other.Right) &&
               EqualityComparer<TLeft?>.Default.Equals(Left, other.Left);
    }

    public override bool Equals(object? obj)
    {
        return obj is Either<TLeft, TRight> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsRight, Right, Left);
    }

    public static bool operator ==(Either<TLeft, TRight> left, Either<TLeft, TRight> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Either<TLeft, TRight> left, Either<TLeft, TRight> right)
    {
        return !left.Equals(right);
    }
}

public class InstructionPointer
{
    private readonly InstructionBuilder _builder;

    public InstructionPointer(InstructionBuilder builder, string? name)
    {
        Name = name;
        _builder = builder;
    }

    public string? Name { get; }

    public override string? ToString()
    {
        return $":{Name}";
    }
}

public class InstructionLabel : InstructionPointer
{
    private readonly InstructionBuilder _builder;

    public InstructionLabel(InstructionBuilder builder, string? name)
        : base(builder, name)
    {
        _builder = builder;
    }

    public void Mark()
    {
        _builder.Mark(this);
    }
}

public class InstructionBuilder
{
    private int _referenceCount;
    private int _labelCount;

    private record struct InstructionItem(InstructionReference? Instruction, InstructionPointer? Label)
    {
        public override string? ToString()
        {
            if (Instruction is null)
            {
                return $"@{Label?.Name}";
            }

            if (Label is null)
            {
                return Instruction?.ToString();
            }

            return $"{Instruction} @{Label.Name}";
        }
    }

    private readonly List<Either<InstructionPointer, InstructionItem>> _instructions = new();

    public InstructionLabel CreateLabel(string? name = null)
    {
        return new InstructionLabel(this, name ?? $"Label {_labelCount++}");
    }

    public InstructionBuilder CreateLabel(string name, out InstructionLabel label)
    {
        label = CreateLabel(name);
        return this;
    }

    public InstructionBuilder CreateLabel(out InstructionLabel label)
    {
        label = CreateLabel();
        return this;
    }

    public InstructionPointer CreatePointer()
    {
        if (_instructions.Count > 1 && _instructions[^2] is {Left: { } pointer})
        {
            return pointer;
        }

        pointer = new InstructionPointer(this, $"pointer {_referenceCount++}");
        _instructions.Insert(_instructions.Count - 1, pointer);
        return pointer;
    }

    public InstructionBuilder CreatePointer(out InstructionPointer pointer)
    {
        pointer = CreatePointer();
        return this;
    }

    public InstructionBuilder Mark(InstructionLabel pointer)
    {
        _instructions.Add(pointer);
        return this;
    }

    public InstructionBuilder Emit(int id, int data = 0, int? index = null)
    {
        if (id > InstructionReference.MaxInstructionId)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (data > InstructionReference.MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        var value = new InstructionItem(InstructionReference.Create(id, data), null);

        if (index.HasValue)
        {
            _instructions.Insert(index.Value, value);
        }
        else
        {
            _instructions.Add(value);
        }

        return this;
    }

    public InstructionBuilder Emit(int id, InstructionPointer pointer, int? index = null)
    {
        var value = new InstructionItem(InstructionReference.Create(id), pointer);

        if (index.HasValue)
        {
            _instructions.Insert(index.Value, value);
        }
        else
        {
            _instructions.Add(value);
        }

        return this;
    }

    public InstructionBuilder Emit(int id, PointerOrData either, int? index = null)
    {
        if (either.IsRight)
        {
            Emit(id, either.Right, index);
        }
        else
        {
            Emit(id, either.Left, index);
        }

        return this;
    }

    public InstructionBuilder EmitRaw(PointerOrData value)
    {
        if (value.IsRight)
        {
            _instructions.Add(new InstructionItem(new InstructionReference(value.Right), null));
        }
        else
        {
            _instructions.Add(new InstructionItem(null, value.Left));
        }

        return this;
    }

    public InstructionBuilder Nop() => Emit(InstructionReference.NOP);

    public InstructionBuilder LoadA(PointerOrData address) => Emit(InstructionReference.AIN, address);

    public InstructionBuilder LoadB(PointerOrData address) => Emit(InstructionReference.BIN, address);

    public InstructionBuilder LoadC(PointerOrData address) => Emit(InstructionReference.CIN, address);

    public InstructionBuilder SetA(PointerOrData value) => Emit(InstructionReference.LDIA, value);

    public InstructionBuilder SetB(PointerOrData value) => Emit(InstructionReference.LDIB, value);

    public InstructionBuilder MovExpansionToA() => Emit(InstructionReference.RDEXP);

    public InstructionBuilder MovAToExpansion() => Emit(InstructionReference.WREXP);

    public InstructionBuilder StoreA(PointerOrData address) => Emit(InstructionReference.STA, address);

    public InstructionBuilder StoreC(PointerOrData address) => Emit(InstructionReference.STC, address);

    public InstructionBuilder Add() => Emit(InstructionReference.ADD);

    public InstructionBuilder Sub() => Emit(InstructionReference.SUB);

    public InstructionBuilder Mult() => Emit(InstructionReference.MULT);

    public InstructionBuilder Div() => Emit(InstructionReference.DIV);

    public InstructionBuilder Jump(PointerOrData counter) => Emit(InstructionReference.JMP, counter);

    public InstructionBuilder Jump(InstructionPointer counter) => Emit(InstructionReference.JMP, counter);

    public InstructionBuilder JumpIfAZero(PointerOrData counter) => Emit(InstructionReference.JMPZ, counter);

    public InstructionBuilder JumpIfAZero(InstructionPointer counter) => Emit(InstructionReference.JMPZ, counter);

    public InstructionBuilder JumpIfCarryBit(PointerOrData counter) => Emit(InstructionReference.JMPC, counter);

    public InstructionBuilder JumpIfCarryBit(InstructionPointer counter) => Emit(InstructionReference.JMPC, counter);

    public InstructionBuilder LoadA() => Emit(InstructionReference.LDAIN);

    public InstructionBuilder StoreB_ToAddressUsingA() => Emit(InstructionReference.STAOUT);

    public InstructionBuilder StoreA_Large(int address)
    {
        Emit(InstructionReference.STLGE);
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder LoadA_Large(InstructionPointer address)
    {
        Emit(InstructionReference.LDLGE);
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder SwapA_B() => Emit(InstructionReference.SWP);

    public InstructionBuilder SwapA_C() => Emit(InstructionReference.SWPC);

    public InstructionBuilder Halt() => Emit(InstructionReference.HLT);

    public int[] ToArray()
    {
        var labels = new Dictionary<InstructionPointer, int>();
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

        var array = new int[i];
        i = 0;

        foreach (var either in _instructions)
        {
            if (either is { IsLeft: true })
            {
                continue;
            }

            var (instruction, label) = either.Right;

            if (!instruction.HasValue)
            {
                if (label is null)
                {
                    throw new InvalidOperationException("Invalid instruction");
                }

                if (!labels.TryGetValue(label, out var index))
                {
                    throw new InvalidOperationException("Label not found");
                }

                array[i] = index;
            }
            else if (label is null)
            {
                array[i] = instruction.Value;
            }
            else if (labels.TryGetValue(label, out var index))
            {
                array[i] = instruction.Value with
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

    public override string ToString()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < _instructions.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine();
            }

            sb.Append(_instructions[i].ToString());
        }

        return sb.ToString();
    }
}
