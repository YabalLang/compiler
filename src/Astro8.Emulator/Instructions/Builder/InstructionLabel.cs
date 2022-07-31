namespace Astro8.Instructions;

public class InstructionLabel : InstructionPointer
{
    private readonly InstructionBuilder _builder;

    public InstructionLabel(InstructionBuilder builder, string? name)
        : base(builder, name)
    {
        _builder = builder;
    }

    public void Mark()
    {
        _builder.Mark(this);
    }
}
