using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BooleanExpression(SourceRange Range, bool Value) : Expression(Range), IConstantValue, IExpressionToB
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(Value ? 1 : 0);
        builder.SetComment($"load boolean {(Value ? "true" : "false")}");
        return LanguageType.Boolean;
    }

    public LanguageType BuildExpressionToB(YabalBuilder builder)
    {
        builder.SetB(Value ? 1 : 0);
        builder.SetComment($"load boolean {(Value ? "true" : "false")}");
        return LanguageType.Boolean;
    }

    object? IConstantValue.Value => Value;

    public override bool OverwritesB => false;

    bool IExpressionToB.OverwritesA => false;
}
