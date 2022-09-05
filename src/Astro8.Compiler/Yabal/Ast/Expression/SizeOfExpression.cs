using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record SizeOfExpression(SourceRange Range, Expression Expression) : IntegerExpressionBase(Range)
{
    public override bool OverwritesB => false;

    protected override int GetValue(YabalBuilder builder)
    {
        if (Expression is IdentifierExpression identifier && !builder.TryGetVariable(identifier.Name, out _))
        {
            builder.AddError(ErrorLevel.Error, identifier.Range, ErrorMessages.UndefinedVariable(identifier.Name));
            return 0;
        }

        if (Expression is not IConstantValue { Value: IAddress })
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.SizeOfExpressionMustBeConstant);
        }

        return base.GetValue(builder);
    }

    public override int Value
    {
        get => Expression is IConstantValue { Value: IAddress { Length: {} length } }
            ? length
            : 0;
        init => throw new NotSupportedException();
    }
}
