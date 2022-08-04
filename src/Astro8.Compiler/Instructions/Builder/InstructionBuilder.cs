using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Astro8.Instructions;

[SuppressMessage("ReSharper", "UseIndexFromEndExpression")]
public class InstructionBuilder : ICollection<int>
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
    private Stack<RegisterWatch> _watchStack = new();

    public InstructionBuilder(IEnumerable<Instruction>? instructions = null)
    {
        _instructions = (instructions ?? Instruction.Default).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

    public RegisterWatch WatchRegister()
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

    void ICollection<int>.Add(int item) => throw new NotSupportedException();

    void ICollection<int>.Clear() => throw new NotSupportedException();

    bool ICollection<int>.Contains(int item) => _references.Any(i => i.Right.Instruction?.Raw == item);

    bool ICollection<int>.Remove(int item) => throw new NotSupportedException();

    public int Count => _references.Count;

    bool ICollection<int>.IsReadOnly => true;

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

    public InstructionPointer CreatePointer(string? name = null, int? index = null)
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

    public InstructionBuilder CreatePointer(out InstructionPointer pointer)
    {
        pointer = CreatePointer();
        return this;
    }

    public InstructionBuilder Mark(InstructionPointer pointer)
    {
        Either<InstructionPointer, InstructionItem> value = pointer;

        var index = _references.IndexOf(value);

        if (index != -1)
        {
            _references.RemoveAt(index);
        }

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
        return CreatePointer(index: index);
    }

    public InstructionBuilder Nop() => EmitRaw(0);

    public InstructionBuilder LoadA(PointerOrData address) => Emit("AIN", address);

    public InstructionBuilder LoadB(PointerOrData address) => Emit("BIN", address);

    public InstructionBuilder LoadC(PointerOrData address) => Emit("CIN", address);

    public InstructionBuilder SetA(PointerOrData value) => Emit("LDIA", value);

    public InstructionBuilder SetB(PointerOrData value) => Emit("LDIB", value);

    public InstructionBuilder MovExpansionToB() => Emit("RDEXP");

    public InstructionBuilder MovBToExpansion() => Emit("WREXP");

    public InstructionBuilder StoreA(PointerOrData address, int? index = null) => Emit("STA", address, index);

    public InstructionBuilder StoreC(PointerOrData address) => Emit("STC", address);

    public InstructionBuilder Add() => Emit("ADD");

    public InstructionBuilder Sub() => Emit("SUB");

    public InstructionBuilder Mult() => Emit("MULT");

    public InstructionBuilder Div() => Emit("DIV");

    public InstructionBuilder CounterToA() => Emit("CTRA");

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

    public InstructionBuilder LoadA_FromAddressUsingA() => Emit("LDAIN");

    public InstructionBuilder StoreB_ToAddressInA() => Emit("STAOUT");

    public InstructionBuilder StoreA_Large(PointerOrData address)
    {
        Emit("STLGE");
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder LoadA_Large(PointerOrData address)
    {
        Emit("LDLGE");
        EmitRaw(address);
        return this;
    }

    public InstructionBuilder SwapA_B() => Emit("SWP");

    public InstructionBuilder SwapA_C() => Emit("SWPC");

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

    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        return _references
            .Where(either => either is { IsRight: true, Right.Instruction: not null })
            .Select(either => either.Right.Instruction!.Value.Raw)
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<int>)this).GetEnumerator();
    }
}
