using Astro8.Yabal.Ast;
using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class InstructionPointer : Pointer
{
    private int? _address;

    public InstructionPointer(string? name, int size = 1, bool isSmall = false)
    {
        Name = name;
        Size = size;
        IsSmall = isSmall;
    }

    public override string? Name { get; }

    public override bool IsSmall { get; }

    public override int Bank => 0;

    public int Size { get; }

    public List<Variable> AssignedVariables { get; } = new();

    public string AssignedVariableNames => string.Join(", ", AssignedVariables.Select(i => i.Name));

    public int Address
    {
        get => _address ?? throw new InvalidOperationException($"No address has been set, make sure {nameof(InstructionBuilder)}.{nameof(InstructionBuilder.CopyTo)} is called before accessing the value");
        set => _address = value;
    }

    public override string? ToString()
    {
        return $"{Name}";
    }

    public override int Get(IReadOnlyDictionary<InstructionPointer, int> mappings)
    {
        if (!mappings.TryGetValue(this, out var value))
        {
            throw new InvalidOperationException($"No address has been set for {this}");
        }

        return value;
    }
}
