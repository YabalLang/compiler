using System.Diagnostics.CodeAnalysis;
using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public class BlockCompileStack
{
    public BlockCompileStack(BlockCompileStack? parent = null)
    {
        Parent = parent;
    }

    public BlockCompileStack? Parent { get; }

    public Dictionary<string, Variable> Variables { get; } = new();

    public int Offset { get; set; }

    public bool TryGetVariable(string name, [NotNullWhen(true)] out Variable? variable)
    {
        if (Variables.TryGetValue(name, out variable))
        {
            return true;
        }

        if (Parent != null)
        {
            return Parent.TryGetVariable(name, out variable);
        }

        return false;
    }
}
