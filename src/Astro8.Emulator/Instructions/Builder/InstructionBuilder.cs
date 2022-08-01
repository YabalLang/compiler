using System.Text;
using PointerOrData = Astro8.Either<Astro8.Instructions.InstructionPointer, int>;

namespace Astro8.Instructions;

public class InstructionBuilder
{
    private readonly Dictionary<string, Instruction> _instructions;

    private int _pointerCount;
    private int _labelCount;

    public InstructionBuilder(IEnumerable<Instruction>? instructions = null)
    {
        _instructions = (instructions ?? Instruction.Default).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

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

    private readonly List<Either<InstructionPointer, InstructionItem>> _references = new();

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
        if (_references.Count > 1 && _references[^2] is {Left: { } pointer})
        {
            return pointer;
        }

        pointer = new InstructionPointer(this, $"pointer {_pointerCount++}");
        _references.Insert(_references.Count - 1, pointer);
        return pointer;
    }

    public InstructionBuilder CreatePointer(out InstructionPointer pointer)
    {
        pointer = CreatePointer();
        return this;
    }

    public InstructionBuilder Mark(InstructionLabel pointer)
    {
        _references.Add(pointer);
        return this;
    }

    private int GetIndex(string name)
    {
        if (!_instructions.TryGetValue(name, out var instruction))
        {
            throw new ArgumentException($"Unknown instruction '{name}'", nameof(name));
        }

        return instruction.Id;
    }

    public InstructionBuilder Emit(string name, int data = 0, int? index = null)
    {
        if (data > InstructionReference.MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        var value = new InstructionItem(InstructionReference.Create(GetIndex(name), data), null);

        if (index.HasValue)
        {
            _references.Insert(index.Value, value);
        }
        else
        {
            _references.Add(value);
        }

        return this;
    }

    public InstructionBuilder Emit(string name, InstructionPointer pointer, int? index = null)
    {
        var value = new InstructionItem(InstructionReference.Create(GetIndex(name)), pointer);

        if (index.HasValue)
        {
            _references.Insert(index.Value, value);
        }
        else
        {
            _references.Add(value);
        }

        return this;
    }

    public InstructionBuilder Emit(string name, PointerOrData either, int? index = null)
    {
        if (either.IsRight)
        {
            Emit(name, either.Right, index);
        }
        else
        {
            Emit(name, either.Left, index);
        }

        return this;
    }

    public InstructionBuilder EmitRaw(PointerOrData value)
    {
        if (value.IsRight)
        {
            _references.Add(new InstructionItem(new InstructionReference(value.Right), null));
        }
        else
        {
            _references.Add(new InstructionItem(null, value.Left));
        }

        return this;
    }

    public InstructionBuilder Nop() => EmitRaw(0);

    public InstructionBuilder LoadA(PointerOrData address) => Emit("AIN", address);

    public InstructionBuilder LoadB(PointerOrData address) => Emit("BIN", address);

    public InstructionBuilder LoadC(PointerOrData address) => Emit("CIN", address);

    public InstructionBuilder SetA(PointerOrData value) => Emit("LDIA", value);

    public InstructionBuilder SetB(PointerOrData value) => Emit("LDIB", value);

    public InstructionBuilder MovExpansionToA() => Emit("RDEXP");

    public InstructionBuilder MovAToExpansion() => Emit("WREXP");

    public InstructionBuilder StoreA(PointerOrData address) => Emit("STA", address);

    public InstructionBuilder StoreC(PointerOrData address) => Emit("STC", address);

    public InstructionBuilder Add() => Emit("ADD");

    public InstructionBuilder Sub() => Emit("SUB");

    public InstructionBuilder Mult() => Emit("MULT");

    public InstructionBuilder Div() => Emit("DIV");

    public InstructionBuilder Jump(PointerOrData counter) => Emit("JMP", counter);

    public InstructionBuilder Jump(InstructionPointer counter) => Emit("JMP", counter);

    public InstructionBuilder JumpIfAZero(PointerOrData counter) => Emit("JMPZ", counter);

    public InstructionBuilder JumpIfAZero(InstructionPointer counter) => Emit("JMPZ", counter);

    public InstructionBuilder JumpIfCarryBit(PointerOrData counter) => Emit("JMPC", counter);

    public InstructionBuilder JumpIfCarryBit(InstructionPointer counter) => Emit("JMPC", counter);

    public InstructionBuilder LoadA() => Emit("LDAIN");

    public InstructionBuilder StoreB_ToAddressUsingA() => Emit("STAOUT");

    public InstructionBuilder StoreA_Large(int address)
    {
        Emit("STLGE");
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder LoadA_Large(InstructionPointer address)
    {
        Emit("LDLGE");
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder SwapA_B() => Emit("SWP");

    public InstructionBuilder SwapA_C() => Emit("SWPC");

    public InstructionBuilder Halt() => Emit("HLT");

    public int[] ToArray()
    {
        var labels = GetLabels(out var length);
        var array = new int[length];

        FillArray(labels, array);

        return array;
    }

    public int[] ToArray(int[] array)
    {
        var labels = GetLabels(out var length);

        if (array.Length < length)
        {
            throw new ArgumentException("Array is too small", nameof(array));
        }

        FillArray(labels, array);

        return array;
    }

    private void FillArray(Dictionary<InstructionPointer, int> labels, int[] array)
    {
        var i = 0;

        foreach (var either in _references)
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
    }

    private Dictionary<InstructionPointer, int> GetLabels(out int i)
    {
        var labels = new Dictionary<InstructionPointer, int>();
        i = 0;

        foreach (var either in _references)
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

        return labels;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < _references.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine();
            }

            sb.Append(_references[i].ToString());
        }

        return sb.ToString();
    }
}
