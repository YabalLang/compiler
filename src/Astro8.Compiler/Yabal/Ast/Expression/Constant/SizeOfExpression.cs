using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record SizeOfExpression(SourceRange Range, Expression Expression) : IntegerExpressionBase(Range)
{
    public override bool OverwritesB => false;

    public override int Value
    {
        get => Expression.Type.Size;
        init => throw new NotSupportedException();
    }

    public override string ToString()
    {
        return $"sizeof({Expression})";
    }
}
