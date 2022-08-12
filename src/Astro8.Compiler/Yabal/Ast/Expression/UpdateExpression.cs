using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record UpdateExpression(SourceRange Range, Expression Value, bool Prefix, BinaryOperator Operator) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        var type = Value.BuildExpression(builder, false);

        if (!isVoid && !Prefix)
        {
            builder.StoreA(builder.UpdatePointer);
        }

        if (type != LanguageType.Integer)
        {
            throw new InvalidOperationException("Cannot update a non-integer value");
        }

        AssignExpression.SetValue(builder, Value, () =>
        {
            builder.SetB(1);

            switch (Operator)
            {
                case BinaryOperator.Add:
                    builder.Add();
                    break;
                case BinaryOperator.Subtract:
                    builder.Sub();
                    break;
                default:
                    throw new InvalidOperationException("Unknown operator");
            }

            return LanguageType.Integer;
        });

        if (!isVoid && !Prefix)
        {
            builder.LoadA(builder.UpdatePointer);
        }

        return LanguageType.Integer;
    }
}
