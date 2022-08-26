using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record UpdateExpression(SourceRange Range, Expression Value, bool Prefix, BinaryOperator Operator) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        var isPrefix = !isVoid && !Prefix;
        var variable = isPrefix ? builder.GetTemporaryVariable() : null;

        var type = Value.BuildExpression(builder, false);

        if (variable != null)
        {
            builder.StoreA(variable);
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

        if (variable != null)
        {
            builder.LoadA(variable);
            variable.Dispose();
        }

        return LanguageType.Integer;
    }
}
