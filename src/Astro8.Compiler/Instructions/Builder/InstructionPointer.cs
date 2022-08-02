namespace Astro8.Instructions;

public class InstructionPointer
{
    private readonly InstructionBuilder _builder;
    private int? _value;

    public InstructionPointer(InstructionBuilder builder, string? name)
    {
        Name = name;
        _builder = builder;
    }

    public string? Name { get; }

    public int Value
    {
        get => _value ?? throw new InvalidOperationException($"No value has been set, make sure {nameof(InstructionBuilder)}.{nameof(InstructionBuilder.CopyTo)} is called before accessing the value");
        set => _value = value;
    }

    public override string? ToString()
    {
        return $":{Name}";
    }
}
