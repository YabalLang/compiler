namespace Yabal.Ast;

public record UpdateExpression(SourceRange Range, AssignableExpression Value, bool Prefix, BinaryOperator Operator) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Value.Initialize(builder);

        Value.MarkModified();
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpression(builder, false, suggestedType);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        var isPrefix = !isVoid && !Prefix;
        var variable = isPrefix ? builder.GetTemporaryVariable() : null;

        Value.BuildExpression(builder, false, suggestedType);

        if (variable != null)
        {
            builder.StoreA(variable);
        }

        builder.SetB(1);

        switch (Operator)
        {
            case BinaryOperator.Add:
                builder.Add();
                builder.SetComment("increment value");
                break;
            case BinaryOperator.Subtract:
                builder.Sub();
                builder.SetComment("decrement value");
                break;
            default:
                throw new InvalidOperationException("Unknown operator");
        }

        Value.StoreFromA(builder, 0);

        if (variable != null)
        {
            builder.LoadA(variable);
            variable.Dispose();
        }
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => LanguageType.Integer;

    public override Expression CloneExpression()
    {
        return new UpdateExpression(Range, (AssignableExpression) Value.CloneExpression(), Prefix, Operator);
    }
}
