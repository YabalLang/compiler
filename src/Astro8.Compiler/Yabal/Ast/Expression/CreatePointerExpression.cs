using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CreatePointerExpression(SourceRange Range, Expression Value, int Bank, LanguageType ElementType) : Expression(Range), IConstantValue
{
    public override void Initialize(YabalBuilder builder)
    {
        Value.Initialize(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        Value.BuildExpression(builder, isVoid);

        if (Value.Type != LanguageType.Integer)
        {
            builder.AddError(ErrorLevel.Error, Value.Range, ErrorMessages.ArgumentMustBeInteger);
            builder.SetA(0);
        }
    }

    object? IConstantValue.Value { get; } = Value is IConstantValue { Value: int value }
        ? RawAddress.From(ElementType, new AbsolutePointer(value, Bank))
        : null;

    public override bool OverwritesB => Value.OverwritesB;

    public override LanguageType Type => LanguageType.Array(ElementType);

    public override Expression CloneExpression()
    {
        return new CreatePointerExpression(Range, Value.CloneExpression(), Bank, ElementType);
    }
}
