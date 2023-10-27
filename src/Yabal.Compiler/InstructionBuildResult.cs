using Yabal.Instructions;

namespace Yabal;

public class InstructionBuildResult : IProgram
{
    private readonly int _offset;
    private readonly ReferenceList _references;
    private readonly Dictionary<InstructionPointer, int> _pointerOffsets;
    private readonly int _length;

    public InstructionBuildResult(ReferenceList list, int offset = 0)
    {
        _offset = offset;
        _references = new ReferenceList(list);
        (_pointerOffsets, _length) = GetPointers(_references, offset);
    }

    public int Length => _references.Count;

    public void ToAssembly(TextWriter writer, bool addComments = false)
    {
        foreach (var either in _references)
        {
            if (either is { IsLeft: true })
            {
                if (addComments)
                {
                    writer.Write(", ");
                    writer.Write(either.Left is InstructionLabel ? "label " : "reference ");

                    writer.Write(either.Left.ToString());

                    if (either.Left is { Comment: { } pointerComment })
                    {
                        writer.Write(" (");
                        writer.Write(pointerComment);
                        writer.Write(") ");
                    }

                    writer.WriteLine();
                }

                continue;
            }

            var (instructionRef, pointer, raw, comment) = either.Right;

            if (!raw && !instructionRef.HasValue && pointer is null && comment is not null && comment.StartsWith(", "))
            {
                writer.WriteLine(comment);
                continue;
            }

            if (!instructionRef.HasValue)
            {
                if (pointer is null)
                {
                    throw new InvalidOperationException("Invalid instruction");
                }

                writer.Write("HERE ");
                writer.Write(pointer.Get(_pointerOffsets));
            }
            else
            {
                if (pointer is not null)
                {
                    instructionRef = instructionRef.Value.WithData(pointer.Get(_pointerOffsets));
                }

                if (raw)
                {
                    writer.Write("HERE ");
                    writer.Write(instructionRef.Value.Raw);
                }
                else
                {
                    var instruction = instructionRef.Value.Instruction;

                    writer.Write(instruction.Name);

                    if (instruction.MicroInstructions.Any(i => i.IsIR))
                    {
                        writer.Write(" ");
                        writer.Write(instructionRef.Value.Data);
                    }
                }
            }


            if (addComments && (comment != null || pointer != null))
            {
                writer.Write(" , ");
                if (pointer != null) writer.Write(pointer.Name);

                if (comment != null)
                {
                    if (pointer != null) writer.Write(", ");
                    writer.Write(comment);
                }
            }

            writer.WriteLine();
        }
    }

    public string ToAssembly(bool addComments = false)
    {
        using var writer = new StringWriter();
        ToAssembly(writer, addComments);
        return writer.ToString();
    }

    public int[] ToArray()
    {
        var array = new int[_length];
        var i = 0;

        foreach (var value in GetBytes())
        {
            array[i++] = value;
        }

        return array;
    }

    public void ToLogisimFile(StreamWriter writer, int minSize = 0)
    {
        writer.WriteLine();
        writer.Write("v3.0 hex words addressed");

        const int perLine = 8;
        var i = 0;

        foreach (var value in GetBytes())
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

    public void ToHex(StreamWriter writer)
    {
        writer.Write("ASTRO-8 AEXE Executable file");

        foreach (var value in GetBytes())
        {
            writer.WriteLine();
            writer.Write(value.ToString("x4"));
        }
    }

    public void CopyTo(int[] array)
    {
        var i = 0;

        foreach (var value in GetBytes())
        {
            array[_offset + i++] = value;
        }
    }

    private IEnumerable<int> GetBytes()
    {
        foreach (var either in _references)
        {
            if (either is { IsLeft: true })
            {
                continue;
            }

            var (instruction, pointer, _, _) = either.Right;

            if (!instruction.HasValue)
            {
                if (pointer is null)
                {
                    throw new InvalidOperationException("Invalid instruction");
                }

                yield return pointer.Get(_pointerOffsets);
            }
            else if (pointer is null)
            {
                yield return instruction.Value;
            }
            else
            {
                yield return instruction.Value.WithData(pointer.Get(_pointerOffsets));
            }
        }
    }

    private static (Dictionary<InstructionPointer, int>, int) GetPointers(ReferenceList list, int i)
    {
        var labels = new Dictionary<InstructionPointer, int>();
        var length = 0;

        foreach (var either in list)
        {
            if (either is { IsLeft: true })
            {
                var label = either.Left;
                labels[label] = i;
                label.Set(i);
            }
            else
            {
                i++;
                length++;
            }
        }

        return (labels, length);
    }
}
