using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record UnaryExpression(SourceRange Range, Expression Value, UnaryOperator Operator) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (Operator != UnaryOperator.Not)
        {
            throw new NotSupportedException();
        }
        var valueType = Value.BuildExpression(builder, isVoid);

        if (valueType != LanguageType.Integer)
        {
            throw new InvalidOperationException($"Cannot use '{Operator}' operator on type '{valueType}'");
        }

        builder.Not();

        return LanguageType.Integer;
    }
}
