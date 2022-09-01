﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Astro8.Yabal.Ast;

namespace Astro8.Instructions;

public abstract class InstructionBuilderBase
{
    int? Index { get; set; }

    public abstract int Count { get; }

    public abstract InstructionLabel CreateLabel(string? name = null);

    public abstract InstructionPointer CreatePointer(string? name = null, int? index = null);

    public abstract void Mark(InstructionPointer pointer);

    public abstract void Emit(string name, PointerOrData either = default, int? index = null);

    public abstract void EmitRaw(PointerOrData value, string? comment = null);

    public void Nop() => EmitRaw(0);

    public void LoadA(PointerOrData address) => Emit("AIN", address);

    public void LoadB(PointerOrData address) => Emit("BIN", address);

    public void LoadC(PointerOrData address) => Emit("CIN", address);

    public void SetA(PointerOrData value) => Emit("LDIA", value);

    public void SetA_Large(PointerOrData value)
    {
        Emit("LDW");
        EmitRaw(value);
    }

    public void SetB(PointerOrData value) => Emit("LDIB", value);

    public void MovExpansionToB() => Emit("RDEXP");

    public void MovBToExpansion() => Emit("WREXP");

    public void StoreA(PointerOrData address, int? index = null) => Emit("STA", address, index);

    public void StoreC(PointerOrData address) => Emit("STC", address);

    public void Add() => Emit("ADD");

    public void Sub() => Emit("SUB");

    public void Mult() => Emit("MULT");

    public void Div() => Emit("DIV");

    public void And() => Emit("AND");

    public void Or() => Emit("OR");

    public void Not() => Emit("NOT");

    public void BitShiftLeft() => Emit("BSL");

    public void BitShiftRight() => Emit("BSR");

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
    private readonly List<Either<InstructionPointer, InstructionItem>> _references = new();
    private readonly Dictionary<string, Instruction> _instructions;

    private int _pointerCount;
    private int _labelCount;

    public InstructionBuilder(IEnumerable<Instruction>? instructions = null)
    {
        _instructions = (instructions ?? Instruction.Default).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

    public int? Index { get; set; }

    private readonly record struct InstructionItem(InstructionReference? Instruction, InstructionPointer? Pointer, bool IsRaw = false, string? Comment = null, int PointerOffset = 0)
    {
        public override string? ToString()
        {
            if (Instruction is null)
            {
                return $"@{Pointer?.Name}";
            }

            if (Pointer is null)
            {
                if (IsRaw)
                {
                    return Instruction?.Raw.ToString();
                }

                return Instruction?.ToString();
            }

            if (IsRaw)
            {
                return $"{Instruction?.Raw} @{Pointer.Name}";
            }

            return $"{Instruction} @{Pointer.Name}";
        }
    }

    public override int Count => _references.Count;

    public void SetComment(string comment)
    {
        var lastIndex = _references.Count - 1;

        if (lastIndex < 0)
        {
            return;
        }

        var item = _references[lastIndex];

        if (item.IsLeft)
        {
            return;
        }

        _references[lastIndex] = item.Right with
        {
            Comment = comment
        };
    }

    public void SetPointerOffset(int offset)
    {
        var lastIndex = _references.Count - 1;

        if (lastIndex < 0)
        {
            throw new IndexOutOfRangeException();
        }

        var item = _references[lastIndex];

        if (item is { IsRight: false } or { Right.Pointer: null })
        {
            throw new InvalidOperationException();
        }

        _references[lastIndex] = item.Right with
        {
            PointerOffset = offset
        };
    }

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

    public override void Emit(string name, PointerOrData data = default, int? index = null)
    {
        if (data.IsRight && data.Right > InstructionReference.MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        if (!_instructions.TryGetValue(name, out var instruction))
        {
            throw new ArgumentException($"Unknown instruction '{name}'", nameof(name));
        }

        // Register
        var value = data.IsRight
            ? new InstructionItem(InstructionReference.Create(instruction.Id, data.Right), null)
            : new InstructionItem(InstructionReference.Create(instruction.Id), data.Left);

        if (index.HasValue)
        {
            _references.Insert(index.Value, value);
        }
        else
        {
            Add(value);
        }
    }

    public override void EmitRaw(PointerOrData value, string? comment = null)
    {
        if (value.IsRight)
        {
            Add(new InstructionItem(new InstructionReference(value.Right), null, true, comment));
        }
        else
        {
            Add(new InstructionItem(null, value.Left, true, comment));
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
        var labels = GetPointers(0, out var length);
        var array = new int[length];
        var i = 0;

        foreach (var value in GetBytes(labels))
        {
            array[i++] = value;
        }

        return array;
    }

    public void CopyTo(int[] array, int offset)
    {
        var labels = GetPointers(offset, out var length);

        if (array.Length < offset + length)
        {
            throw new ArgumentException("Array is too small", nameof(array));
        }

        var i = 0;

        foreach (var value in GetBytes(labels))
        {
            array[offset + i++] = value;
        }
    }

    public void ToHex(StreamWriter writer)
    {
        var labels = GetPointers(0, out _);
        var addNewLine = false;

        foreach (var value in GetBytes(labels))
        {
            if (addNewLine)
            {
                writer.WriteLine();
            }
            else
            {
                addNewLine = true;
            }

            writer.Write(value.ToString("x4"));
        }
    }

    public void ToLogisimFile(StreamWriter writer, int minSize = 0)
    {
        var labels = GetPointers(0, out _);

        writer.WriteLine();
        writer.Write("v3.0 hex words addressed");

        const int perLine = 8;
        var i = 0;

        foreach (var value in GetBytes(labels))
        {
            if (i % perLine == 0)
            {
                writer.WriteLine();
                writer.Write($"{i:x3}: ");
            }

            writer.Write(value.ToString("x4"));
            writer.Write(' ');
            i++;
        }

        for (; i < minSize; i++)
        {
            if (i % perLine == 0)
            {
                writer.WriteLine();
                writer.Write($"{i:x3}: ");
            }

            writer.Write("0000 ");
        }
    }

    public void ToAssembly(StreamWriter writer, bool addComments = false)
    {
        var pointers = GetPointers(0, out _);

        foreach (var either in _references)
        {
            if (either is { IsLeft: true })
            {
                continue;
            }

            var (instruction, pointer, raw, comment, offset) = either.Right;

            if (!instruction.HasValue)
            {
                if (pointer is null)
                {
                    throw new InvalidOperationException("Invalid instruction");
                }

                if (!pointers.TryGetValue(pointer, out var index))
                {
                    throw new InvalidOperationException($"Pointer {pointer.Name} not found");
                }

                writer.Write($"HERE {index + offset}");
            }
            else if (pointer is null)
            {
                writer.Write(raw ? $"HERE {instruction.Value.Raw}" : instruction.Value.ToString());
            }
            else if (pointers.TryGetValue(pointer, out var index))
            {
                instruction = instruction.Value with
                {
                    Data = index + offset
                };

                writer.Write(raw ? $"HERE {instruction.Value.Raw}" : instruction.Value.ToString());
            }
            else
            {
                throw new InvalidOperationException($"Pointer {pointer.Name} not found");
            }

            if (addComments && comment != null)
            {
                writer.Write(" , ");
                writer.Write(comment);
            }

            writer.WriteLine();
        }
    }

    private IEnumerable<int> GetBytes(Dictionary<InstructionPointer, int> labels, int i = 0)
    {
        foreach (var either in _references)
        {
            if (either is { IsLeft: true })
            {
                continue;
            }

            var (instruction, pointer, _, _, offset) = either.Right;

            if (!instruction.HasValue)
            {
                if (pointer is null)
                {
                    throw new InvalidOperationException("Invalid instruction");
                }

                if (!labels.TryGetValue(pointer, out var index))
                {
                    throw new InvalidOperationException($"Pointer {pointer.Name} not found");
                }

                yield return index + offset;
            }
            else if (pointer is null)
            {
                yield return instruction.Value;
            }
            else if (labels.TryGetValue(pointer, out var index))
            {
                yield return instruction.Value with
                {
                    Data = index + offset
                };
            }
            else
            {
                throw new InvalidOperationException($"Pointer {pointer.Name} not found");
            }

            i++;
        }
    }

    private Dictionary<InstructionPointer, int> GetPointers(int i, out int length)
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
