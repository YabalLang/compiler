using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IdentifierExpression(SourceRange Range, string Name) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        if (!builder.TryGetVariable(Name, out var variable))
        {
            throw new InvalidOperationException($"Variable {Name} not found");
        }

        builder.LoadA(variable.Pointer);

        return variable.Type;
    }
}
