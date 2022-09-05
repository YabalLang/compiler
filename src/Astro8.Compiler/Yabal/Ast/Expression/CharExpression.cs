using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CharExpression(SourceRange Range, char Value) : Expression(Range), IConstantValue, IExpressionToB
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (Character.CharToInt.TryGetValue(Value, out var intValue))
        {
            builder.SetA(intValue);
        }
        else
        {
            builder.SetA(0);
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.InvalidCharacter(Value));
        }

        builder.SetComment($"load character '{Value}'");
        return LanguageType.Integer;
    }

    public LanguageType BuildExpressionToB(YabalBuilder builder)
    {
        if (Character.CharToInt.TryGetValue(Value, out var intValue))
        {
            builder.SetB(intValue);
        }
        else
        {
            builder.SetB(0);
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.InvalidCharacter(Value));
        }

        builder.SetComment($"load character '{Value}'");
        return LanguageType.Integer;
    }

    object? IConstantValue.Value => Character.CharToInt.TryGetValue(Value, out var intValue) ? intValue : 0;

    public override bool OverwritesB => false;

    bool IExpressionToB.OverwritesA => false;
}
