namespace Yabal.Ast;

public record AssignExpression(SourceRange Range, AssignableExpression Object, Expression Value) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Object.Initialize(builder);
        Value.Initialize(builder);

        if (Value is ITypeExpression typeExpression)
        {
            typeExpression.Initialize(builder, Object.Type);
        }

        Object.MarkModified();

        if (Value is ArrowFunctionExpression arrowFunction)
        {
            arrowFunction.Function.MarkUsed();
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        Object.Assign(builder, Value, Range);
        Object.ShowDebug(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        Object.Assign(builder, Value, Range);
        Object.ShowDebug(builder);
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Value.Type;

    public override Expression CloneExpression()
    {
        return new AssignExpression(Range, (AssignableExpression) Object.CloneExpression(), Value.CloneExpression());
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new AssignExpression(Range, Object, Value.Optimize(suggestedType ?? Object.Type));
    }
}
