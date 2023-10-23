namespace Yabal.Ast;

public record BooleanExpression(SourceRange Range, bool Value) : Expression(Range), IConstantValue, IExpressionToB
{
    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpressionCore(builder, false, suggestedType);
        pointer.StoreA(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        builder.SetA(Value ? 1 : 0);
        builder.SetComment($"load boolean {(Value ? "true" : "false")}");
    }

    void IExpressionToB.BuildExpressionToB(YabalBuilder builder)
    {
        builder.SetB(Value ? 1 : 0);
        builder.SetComment($"load boolean {(Value ? "true" : "false")}");
    }

    object? IConstantValue.Value => Value;

    public void StoreConstantValue(Span<int> buffer)
    {
        buffer[0] = Value ? 1 : 0;
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Boolean;

    bool IExpressionToB.OverwritesA => false;

    public override Expression CloneExpression()
    {
        return this;
    }
}
