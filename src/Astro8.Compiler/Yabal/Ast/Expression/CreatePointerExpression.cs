using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CreatePointerExpression(SourceRange Range, Expression Value, LanguageType Type) : Expression(Range), IConstantValue
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        var result = Value.BuildExpression(builder, isVoid);

        if (result != LanguageType.Integer)
        {
            builder.AddError(ErrorLevel.Error, Value.Range, ErrorMessages.ArgumentMustBeInteger);
            builder.SetA(0);
        }

        return LanguageType.Pointer(Type);
    }

    object? IConstantValue.Value { get; } = Value is IConstantValue { Value: int value }
        ? RawAddress.From(value, Type)
        : null;

    public override bool OverwritesB => Value.OverwritesB;
}
