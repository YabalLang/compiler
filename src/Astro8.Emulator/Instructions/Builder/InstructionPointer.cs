namespace Astro8.Instructions;

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
