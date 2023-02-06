namespace Yabal.Ast;

public record CharExpression(SourceRange Range, char Value) : Expression(Range), IConstantValue, IExpressionToB
{
    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
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
    }

    void IExpressionToB.BuildExpressionToB(YabalBuilder builder)
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
    }

    object? IConstantValue.Value => Character.CharToInt.TryGetValue(Value, out var intValue) ? intValue : 0;

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Integer;

    bool IExpressionToB.OverwritesA => false;

    public override string ToString()
    {
        return $"'{Value}'";
    }

    public override Expression CloneExpression()
    {
        return this;
    }
}
