using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AssignExpression(SourceRange Range, string Name, Expression Value) : Expression(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Value.BeforeBuild(builder);
    }

    public override LanguageType BuildExpression(YabalBuilder builder)
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

        builder.StoreA(variable.Pointer);
        return type;
    }
}
