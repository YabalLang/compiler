namespace Yabal.Ast;

public record TernaryExpression(SourceRange Range, Expression Expression, Expression Consequent, Expression Alternate) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
        Consequent.Initialize(builder);
        Alternate.Initialize(builder);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildTernary(builder, suggestedType, e => e.BuildExpressionToPointer(builder, suggestedType, pointer));
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        BuildTernary(builder, suggestedType, e => e.BuildExpression(builder, false, suggestedType));
    }

    private void BuildTernary(YabalBuilder builder, LanguageType? suggestedType, Action<Expression> callback)
    {
        var consequentLabel = builder.CreateLabel();
        var alternateLabel = builder.CreateLabel();
        var end = builder.CreateLabel();
        var expression = Expression.Optimize(LanguageType.Boolean);

        expression.CreateComparison(builder, alternateLabel, consequentLabel);

        builder.Mark(consequentLabel);
        callback(Consequent);

        builder.Jump(end);
        builder.Mark(alternateLabel);

        callback(Alternate);
        builder.Mark(end);
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        var expression = Expression.Optimize(suggestedType);
        var consequent = Consequent.Optimize(suggestedType);
        var alternate = Alternate.Optimize(suggestedType);

        if (expression is IConstantValue { Value: bool value })
        {
            return value ? consequent : alternate;
        }

        return this with
        {
            Expression = expression,
            Consequent = consequent,
            Alternate = alternate
        };
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Consequent.Type;

    public override Expression CloneExpression()
    {
        return new TernaryExpression(
            Range,
            Expression.CloneExpression(),
            Consequent.CloneExpression(),
            Alternate.CloneExpression()
        );
    }
}
