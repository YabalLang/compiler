using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AssignStatement(SourceRange Range, string Name, Expression Value) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        if (!builder.TryGetVariable(Name, out var variable))
        {
            throw new InvalidOperationException($"Variable {Name} not found");
        }

        var type = Value.BuildExpression(builder);

        if (type != variable.Type)
        {
            throw new InvalidOperationException("Type mismatch");
        }

        builder.Instruction.StoreA(variable.Pointer);
    }
}