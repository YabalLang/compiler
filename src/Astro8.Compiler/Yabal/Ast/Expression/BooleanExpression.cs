using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BooleanExpression(SourceRange Range, bool Value) : Expression(Range), IConstantValue
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(Value ? 1 : 0);
        return LanguageType.Boolean;
    }

    object IConstantValue.Value => Value;
}
