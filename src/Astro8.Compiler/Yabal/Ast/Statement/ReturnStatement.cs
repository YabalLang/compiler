using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ReturnStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        if (builder.Block.Function is not {} function)
        {
            throw new InvalidOperationException("Return statement outside of function");
        }

        var type = Expression.BuildExpression(builder, false);

        if (type != LanguageType.Assembly && function.ReturnType != type)
        {
            throw new InvalidOperationException($"Return type mismatch: {function.ReturnType} != {type}");
        }
    }
}
