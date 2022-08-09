using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Astro8.Instructions;

public abstract class InstructionBuilderBase
{
    int? Index { get; set; }

    public abstract int Count { get; }

    public abstract InstructionBuilder.RegisterWatch WatchRegister();

    public abstract InstructionLabel CreateLabel(string? name = null);

    public abstract InstructionPointer CreatePointer(string? name = null, int? index = null);

    public abstract void Mark(InstructionPointer pointer);

    public abstract void Emit(string name, PointerOrData either = default, int? index = null);

    public abstract void EmitRaw(PointerOrData value);

    public void Nop() => EmitRaw(0);

    public void LoadA(PointerOrData address) => Emit("AIN", address);

    public void LoadB(PointerOrData address) => Emit("BIN", address);

    public void LoadC(PointerOrData address) => Emit("CIN", address);

    public void SetA(PointerOrData value) => Emit("LDIA", value);

    public void SetB(PointerOrData value) => Emit("LDIB", value);

    public void MovExpansionToB() => Emit("RDEXP");

    public void MovBToExpansion() => Emit("WREXP");

    public void StoreA(PointerOrData address, int? index = null) => Emit("STA", address, index);

    public void StoreC(PointerOrData address) => Emit("STC", address);

    public void Add() => Emit("ADD");

    public void Sub() => Emit("SUB");

    public void Mult() => Emit("MULT");

    public void Div() => Emit("DIV");

    public void CounterToA() => Emit("CTRA");

    public void Jump(PointerOrData counter)
    {
        Emit("JMP");
        EmitRaw(counter);
    }

    public void JumpIfZero(PointerOrData counter)
    {
        Emit("JMPZ");
        EmitRaw(counter);
    }

    public void JumpIfCarryBit(PointerOrData counter)
    {
        Emit("JMPC");
        EmitRaw(counter);
    }

    public void JumpToA() => Emit("JREG");

    public void LoadA_FromAddressUsingA() => Emit("LDAIN");

    public void StoreB_ToAddressInA() => Emit("STAOUT");

    public void StoreA_Large(PointerOrData address)
    {
        Emit("STLGE");
        EmitRaw(address);
    }

    public void LoadA_Large(PointerOrData address)
    {
        Emit("LDLGE");
        EmitRaw(address);
    }

    public void SwapA_B() => Emit("SWP");

    public void SwapA_C() => Emit("SWPC");
}

[SuppressMessage("ReSharper", "UseIndexFromEndExpression")]
public class InstructionBuilder : InstructionBuilderBase, IProgram
{
    public sealed class RegisterWatch : IDisposable
    {
        private readonly InstructionBuilder _builder;

        public RegisterWatch(InstructionBuilder builder)
        {
            _builder = builder;
        }

        public bool A { get; set; }

        public bool B { get; set; }

        public bool C { get; set; }

        public void Reset()
        {
            A = false;
            B = false;
            C = false;
        }

        public void Dispose()
        {
            _builder._watchStack.Pop();
        }
    }

    private readonly List<Either<InstructionPointer, InstructionItem>> _references = new();
    private readonly Dictionary<string, Instruction> _instructions;

    private int _pointerCount;
    private int _labelCount;
    private readonly Stack<RegisterWatch> _watchStack = new();

    public InstructionBuilder(IEnumerable<Instruction>? instructions = null)
    {
        _instructions = (instructions ?? Instruction.Default).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

    public int? Index { get; set; }

    public override RegisterWatch WatchRegister()
    {
        var watch = new RegisterWatch(this);
        _watchStack.Push(watch);
        return watch;
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

    public override int Count => _references.Count;

    private void Add(Either<InstructionPointer, InstructionItem> instruction)
    {
        if (Index.HasValue)
        {
            _references.Insert(Index.Value, instruction);
            Index++;
            return;
        }

        _references.Add(instruction);
    }

    public override InstructionLabel CreateLabel(string? name = null)
    {
        return new InstructionLabel(this, name ?? $"L{_labelCount++}");
    }

    public void CreateLabel(string name, out InstructionLabel label)
    {
        label = CreateLabel(name);
    }

    public void CreateLabel(out InstructionLabel label)
    {
        label = CreateLabel();
    }

    public override InstructionPointer CreatePointer(string? name = null, int? index = null)
    {
        index ??= _references.Count - 1;

        var pointer = new InstructionPointer(name ?? $"P{_pointerCount++}");

        if (_references.Count == 0)
        {
            return pointer;
        }

        _references.Insert(index.Value, pointer);
        return pointer;
    }

    public void CreatePointer(out InstructionPointer pointer)
    {
        pointer = CreatePointer();
    }

    public override void Mark(InstructionPointer pointer)
    {
        Either<InstructionPointer, InstructionItem> value = pointer;

        var index = _references.IndexOf(value);

        if (index != -1)
        {
            _references.RemoveAt(index);
        }

        Add(pointer);
    }

    private int GetIndex(string name)
    {
        if (!_instructions.TryGetValue(name, out var instruction))
        {
            throw new ArgumentException($"Unknown instruction '{name}'", nameof(name));
        }

        return instruction.Id;
    }

    public void Emit(string name, int data, int? index = null)
    {
        if (data > InstructionReference.MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        if (!_instructions.TryGetValue(name, out var instruction))
        {
            throw new ArgumentException($"Unknown instruction '{name}'", nameof(name));
        }

        // Update watchers
        foreach (var microInstruction in instruction.MicroInstructions)
        {
            if (microInstruction.IsWA)
            {
                foreach (var watch in _watchStack)
                {
                    watch.A = true;
                }
            }
            else if (microInstruction.IsWA)
            {
                foreach (var watch in _watchStack)
                {
                    watch.B = true;
                }
            }
            else if (microInstruction.IsWC)
            {
                foreach (var watch in _watchStack)
                {
                    watch.C = true;
                }
            }
        }

        // Register
        var value = new InstructionItem(InstructionReference.Create(instruction.Id, data), null);

        if (index.HasValue)
        {
            _references.Insert(index.Value, value);
        }
        else
        {
            Add(value);
        }
    }

    public void Emit(string name, InstructionPointer pointer, int? index = null)
    {
        var value = new InstructionItem(InstructionReference.Create(GetIndex(name)), pointer);

        if (index.HasValue)
        {
            _references.Insert(index.Value, value);
        }
        else
        {
            Add(value);
        }
    }

    public override void Emit(string name, PointerOrData either = default, int? index = null)
    {
        if (either.IsRight)
        {
            Emit(name, either.Right, index);
        }
        else
        {
            Emit(name, either.Left, index);
        }
    }

    public override void EmitRaw(PointerOrData value)
    {
        if (value.IsRight)
        {
            Add(new InstructionItem(new InstructionReference(value.Right), null, true));
        }
        else
        {
            Add(new InstructionItem(null, value.Left, true));
        }
    }

    public InstructionPointer EmitRawAt(int index, int value, string? name = null)
    {
        _references.Insert(index, new InstructionItem(new InstructionReference(value), null, true));
        return CreatePointer(name, index: index);
    }

    public int IndexOf(InstructionPointer pointer)
    {
        for (var i = 0; i < _references.Count; i++)
        {
            if (_references[i].IsLeft && _references[i].Left == pointer)
            {
                return i;
            }
        }

        return -1;
    }

    public int[] ToArray()
    {
        var labels = GetLabels(0, out var length);
        var array = new int[length];

        FillArray(labels, array);

        return array;
    }

    public void CopyTo(int[] array, int offset)
    {
        var labels = GetLabels(offset, out var length);

        if (array.Length < offset + length)
        {
            throw new ArgumentException("Array is too small", nameof(array));
        }

        FillArray(labels, array);
    }

    private void FillArray(Dictionary<InstructionPointer, int> labels, int[] array, int i = 0)
    {
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
                    throw new InvalidOperationException($"Label {label.Name} not found");
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
                throw new InvalidOperationException($"Label {label.Name} not found");
            }

            i++;
        }
    }

    private Dictionary<InstructionPointer, int> GetLabels(int i, out int length)
    {
        var labels = new Dictionary<InstructionPointer, int>();

        length = 0;

        foreach (var either in _references)
        {
            if (either is { IsLeft: true })
            {
                var label = either.Left;
                labels[label] = i;
                label.Address = i;
            }
            else
            {
                i++;
                length++;
            }
        }

        return labels;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        var prefix = "";
        var newLine = false;
        var addPrefix = true;
        var index = 0;

        void AddPrefix()
        {
            var length = index == 0 ? 1 : Math.Floor(Math.Log10(index) + 1);

            for (var i = length; i < 3; i++)
            {
                sb.Append(' ');
            }

            sb.Append(index);
            sb.Append(" | ");

            if (prefix.Length > 0)
            {
                sb.Append(prefix);
            }
        }

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

            if (reference is { IsLeft: true, Left: { } left })
            {
                if (left is InstructionLabel label)
                {
                    sb.Append("    | ");
                    sb.Append(label.Name);
                    sb.Append(':');
                    prefix = "  ";
                }
                else
                {
                    if (addPrefix)
                    {
                        AddPrefix();
                    }
                    else
                    {
                        sb.Append(' ');
                    }

                    sb.Append('[');
                    sb.Append(left.Name);
                    sb.Append("] ");
                    newLine = false;
                    addPrefix = false;
                }

                continue;
            }

            if (addPrefix)
            {
                AddPrefix();
            }
            else
            {
                addPrefix = true;
            }

            sb.Append(reference.ToString());
            index++;
        }

        return sb.ToString();
    }

    public void AddRange(InstructionBuilder builder)
    {
        _pointerCount += builder._pointerCount;
        _labelCount += builder._labelCount;
        _references.AddRange(builder._references);
    }
}
