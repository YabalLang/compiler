using System.Diagnostics.CodeAnalysis;
using System.Text;
using PointerOrData = Astro8.Either<Astro8.Instructions.InstructionPointer, int>;

namespace Astro8.Instructions;

[SuppressMessage("ReSharper", "UseIndexFromEndExpression")]
public class InstructionBuilder
{
    private readonly Dictionary<string, Instruction> _instructions;

    private int _pointerCount;
    private int _labelCount;

    public InstructionBuilder(IEnumerable<Instruction>? instructions = null)
    {
        _instructions = (instructions ?? Instruction.Default).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

    private readonly record struct InstructionItem(InstructionReference? Instruction, InstructionPointer? Label, bool IsRaw = false)
    {
        public override string? ToString()
        {
            if (Instruction is null)
            {
                return $"@{Label?.Name}";
            }

            if (Label is null)
            {
                if (IsRaw)
                {
                    return Instruction?.Raw.ToString();
                }

                return Instruction?.ToString();
            }

            if (IsRaw)
            {
                return $"{Instruction?.Raw} @{Label.Name}";
            }

            return $"{Instruction} @{Label.Name}";
        }
    }

    private readonly List<Either<InstructionPointer, InstructionItem>> _references = new();

    public int Count => _references.Count;

    public InstructionLabel CreateLabel(string? name = null)
    {
        return new InstructionLabel(this, name ?? $"L{_labelCount++}");
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

    public InstructionPointer CreatePointer(int? index = null)
    {
        if (_references.Count == 0)
        {
            throw new InvalidOperationException("No instructions have been added");
        }

        index ??= _references.Count - 1;

        if (_references.Count > 1 && _references[index.Value - 1] is {Left: { } pointer})
        {
            return pointer;
        }

        pointer = new InstructionPointer(this, $"P{_pointerCount++}");
        _references.Insert(index.Value, pointer);
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
            _references.Add(new InstructionItem(new InstructionReference(value.Right), null, true));
        }
        else
        {
            _references.Add(new InstructionItem(null, value.Left, true));
        }

        return this;
    }

    public InstructionPointer EmitRawAt(int index, int value)
    {
        _references.Insert(index, new InstructionItem(new InstructionReference(value), null, true));
        return CreatePointer(index);
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

    public InstructionBuilder Jump(PointerOrData counter)
    {
        Emit("JMP");
        return EmitRaw(counter);
    }

    public InstructionBuilder JumpIfAZero(PointerOrData counter)
    {
        Emit("JMPZ");
        return EmitRaw(counter);
    }

    public InstructionBuilder JumpIfCarryBit(PointerOrData counter)
    {
        Emit("JMPC");
        return EmitRaw(counter);
    }

    public InstructionBuilder JumpToA() => Emit("JREG");

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

            var (instruction, label, _) = either.Right;

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
                var label = either.Left;
                labels[label] = i;
                label.Value = i;
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

        var prefix = "";
        var newLine = false;

        foreach (var reference in _references)
        {
            if (newLine)
            {
                sb.AppendLine();
            }
            else
            {
                newLine = true;
            }

            if (prefix.Length > 0)
            {
                sb.Append(prefix);
            }

            if (reference is { IsLeft: true, Left: { } left })
            {
                if (left is InstructionLabel label)
                {
                    sb.Append(label.Name);
                    sb.Append(':');
                    prefix = "  ";
                }
                else
                {
                    sb.Append('[');
                    sb.Append(left.Name);
                    sb.Append("] ");
                    newLine = false;
                }

                continue;
            }

            sb.Append(reference.ToString());
        }

        return sb.ToString();
    }
}
