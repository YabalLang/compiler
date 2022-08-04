using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BinaryExpression(SourceRange Range, BinaryOperator Operator, Expression Left, Expression Right) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        if (Right is IntegerExpression intExpression)
        {
            Left.Build(builder);
            builder.SetB(intExpression.Value);
        }
        else
        {
            Left.Build(builder);
            var leftOffset = builder.Count;
            builder.SwapA_B();

            using var watcher = builder.WatchRegister();
            Right.Build(builder);

            if (watcher.B)
            {
                // Right changed the value in register B, so we need to store the left-side to a temporary variable
                builder.StoreA(builder.Temp, leftOffset);
                builder.LoadB(builder.Temp);
            }
        }

        switch (Operator)
        {
            case BinaryOperator.Add:
                builder.Add();
                break;
            case BinaryOperator.Subtract:
                builder.Sub();
                break;
            case BinaryOperator.Multiply:
                builder.Mult();
                break;
            case BinaryOperator.Divide:
                builder.SwapA_B();
                builder.Div();
                break;
            default:
                throw new NotSupportedException();
        }

        return LanguageType.Integer;
    }

    public override Expression Optimize()
    {
        var left = Left.Optimize();
        var right = Right.Optimize();

        if (left is IntegerExpression { Value: var leftInt } &&
            right is IntegerExpression { Value: var rightInt })
        {
            return new IntegerExpression(
                Range,
                Operator switch
                {
                    BinaryOperator.Add => leftInt + rightInt,
                    BinaryOperator.Subtract => leftInt - rightInt,
                    BinaryOperator.Multiply => leftInt * rightInt,
                    BinaryOperator.Divide => leftInt / rightInt,
                    _ => throw new NotSupportedException()
                });
        }

        return new BinaryExpression(Range, Operator, left, right);
    }
}
