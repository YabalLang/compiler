using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public sealed class TemporaryVariable : IDisposable
{
    public TemporaryVariable(InstructionPointer pointer, BlockStack block)
    {
        Pointer = pointer;
        Block = block;
    }

    public InstructionPointer Pointer { get; }

    public BlockStack Block { get; }

    public void Dispose()
    {
        Block.TemporaryVariablesStack.Push(this);
    }

    public static implicit operator InstructionPointer(TemporaryVariable variable)
    {
        return variable.Pointer;
    }

    public static implicit operator PointerOrData(TemporaryVariable variable)
    {
        return variable.Pointer;
    }
}