namespace Astro8.Instructions;

public class InstructionLabel : InstructionPointer
{
    public InstructionLabel(string name)
        : base(name)
    {
    }

    public override string? ToString()
    {
        return Name;
    }
}
