using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class InstructionPointer
{
    private int? _address;

    public InstructionPointer(string? name)
    {
        Name = name;
    }

    public string? Name { get; }

    public List<Variable> AssignedVariables { get; } = new();

    public string AssignedVariableNames => string.Join(", ", AssignedVariables.Select(i => i.Name));

    public int Address
    {
        get => _address ?? throw new InvalidOperationException($"No address has been set, make sure {nameof(InstructionBuilder)}.{nameof(InstructionBuilder.CopyTo)} is called before accessing the value");
        set => _address = value;
    }

    public override string? ToString()
    {
        return $"@{Name}";
    }
}
