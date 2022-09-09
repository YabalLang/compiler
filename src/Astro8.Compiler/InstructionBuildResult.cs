using Astro8.Instructions;

namespace Astro8;

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

    public void ToAssembly(TextWriter writer, bool addComments = false, bool htmlHighlight = false)
    {
        var i = 0;

        foreach (var either in _references)
        {
            if (either is { IsLeft: true })
            {
                if (addComments)
                {
                    if (htmlHighlight)
                    {
                        writer.Write(@"<div class=""asm-line asm-pointer-line"">");
                        writer.Write(@"<span class=""asm-line-number""></span>");
                        writer.Write(@"<span class=""asm-comment"">");
                    }

                    writer.Write(", ");
                    writer.Write(either.Left is InstructionLabel ? "label " : "reference ");
                    writer.WriteLine(either.Left.ToString());

                    if (htmlHighlight)
                    {
                        writer.Write(@"</span>");
                        writer.Write(@"</div>");
                    }
                }

                continue;
            }

            i++;

            if (htmlHighlight)
            {
                writer.Write(@"<div class=""asm-line asm-instruction-line"">");

                writer.Write(@"<span class=""asm-line-number"">");
                writer.Write(i.ToString());
                writer.Write(@"</span>");
            }

            var (instructionRef, pointer, raw, comment) = either.Right;

            if (!instructionRef.HasValue)
            {
                if (pointer is null)
                {
                    throw new InvalidOperationException("Invalid instruction");
                }

                if (htmlHighlight) writer.Write(@"<span class=""asm-instruction"">");

                writer.Write("HERE ");

                if (htmlHighlight) writer.Write(@"</span>");
                if (htmlHighlight) writer.Write(@"<span class=""asm-instruction-data"">");

                writer.Write(pointer.Get(_pointerOffsets));

                if (htmlHighlight) writer.Write(@"</span>");
            }
            else
            {
                if (pointer is not null)
                {
                    instructionRef = instructionRef.Value.WithData(pointer.Get(_pointerOffsets));
                }

                if (raw)
                {
                    if (htmlHighlight) writer.Write(@"<span class=""asm-instruction"">");

                    writer.Write("HERE ");

                    if (htmlHighlight) writer.Write(@"</span>");
                    if (htmlHighlight) writer.Write(@"<span class=""asm-instruction-data"">");

                    writer.Write(instructionRef.Value.Raw);

                    if (htmlHighlight) writer.Write(@"</span>");
                }
                else
                {
                    var instruction = instructionRef.Value.Instruction;

                    if (htmlHighlight) writer.Write(@"<span class=""asm-instruction"">");

                    writer.Write(instruction.Name);

                    if (htmlHighlight) writer.Write(@"</span>");

                    if (instruction.MicroInstructions.Any(i => i.IsIR))
                    {
                        if (htmlHighlight) writer.Write(@"<span class=""asm-instruction-data"">");

                        writer.Write(" ");
                        writer.Write(instructionRef.Value.Data);

                        if (htmlHighlight) writer.Write(@"</span>");
                    }
                }
            }


            if (addComments && (comment != null || pointer != null))
            {
                if (htmlHighlight) writer.Write(@"<span class=""asm-comment"">");

                writer.Write(" , ");
                if (pointer != null) writer.Write(pointer.Name);

                if (comment != null)
                {
                    if (pointer != null) writer.Write(", ");
                    writer.Write(comment);
                }

                if (htmlHighlight) writer.Write(@"</span>");
            }

            if (htmlHighlight) writer.Write(@"</div>");
            writer.WriteLine();
        }
    }

    public string ToAssembly(bool addComments = false, bool htmlHighlight = false)
    {
        using var writer = new StringWriter();
        ToAssembly(writer, addComments, htmlHighlight);
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
                label.Address = i;
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
