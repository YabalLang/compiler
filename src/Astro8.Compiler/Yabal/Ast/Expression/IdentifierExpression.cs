using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IdentifierExpression(SourceRange Range, string Name) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        return LoadValue(builder, Name);
    }

    public static LanguageType LoadValue(YabalBuilder builder, string name)
    {
        if (!builder.TryGetVariable(name, out var variable))
        {
            throw new InvalidOperationException($"Variable {name} not found");
        }

        builder.LoadA(variable.Pointer);

        return variable.Type;
    }
}
