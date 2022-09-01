using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record SizeOfExpression(SourceRange Range, Expression Expression) : IntegerExpressionBase(Range)
{
    public override bool OverwritesB => false;

    public override int Value
    {
        get => Expression is IConstantValue { Value: IAddress { Length: {} length } }
            ? length
            : throw new InvalidOperationException();
        init => throw new NotSupportedException();
    }
}
