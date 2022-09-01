using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CharExpression(SourceRange Range, char Value) : Expression(Range), IConstantValue, IExpressionToB
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(GetValue(Value));
        builder.SetComment($"load character '{Value}'");
        return LanguageType.Integer;
    }

    public LanguageType BuildExpressionToB(YabalBuilder builder)
    {
        builder.SetB(GetValue(Value));
        builder.SetComment($"load character '{Value}'");
        return LanguageType.Integer;
    }

    public static int GetValue(char value)
    {
        if (!Character.CharToInt.TryGetValue(value, out var intValue))
        {
            throw new KeyNotFoundException($"Unknown character '{value}'");
        }

        return intValue;
    }

    object? IConstantValue.Value => GetValue(Value);

    public override bool OverwritesB => false;

    bool IExpressionToB.OverwritesA => false;
}
