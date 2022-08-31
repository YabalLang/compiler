using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record Address(int Value);

public record CreatePointerExpression(SourceRange Range, Expression Value) : Expression(Range), IConstantValue
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        var result = Value.BuildExpression(builder, isVoid);

        if (result != LanguageType.Integer)
        {
            throw new InvalidOperationException("Integer expected");
        }

        return LanguageType.Pointer(LanguageType.Integer);
    }

    object? IConstantValue.Value => Value is IConstantValue { Value: int value }
        ? new Address(value)
        : null;

    public override bool OverwritesB => Value.OverwritesB;
}
