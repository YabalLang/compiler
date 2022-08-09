using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BinaryExpression(SourceRange Range, BinaryOperator Operator, Expression Left, Expression Right) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        SetRegisters(builder);

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
            case BinaryOperator.LessThan:
            case BinaryOperator.Equal:
            case BinaryOperator.NotEqual:
            case BinaryOperator.GreaterThan:
            case BinaryOperator.GreaterThanOrEqual:
            case BinaryOperator.LessThanOrEqual:
            {
                var trueLabel = builder.CreateLabel();
                var falseLabel = builder.CreateLabel();
                var skipLabel = builder.CreateLabel();
                CreateComparison(builder, trueLabel, falseLabel);

                builder.Mark(falseLabel);
                builder.SetA(0);
                builder.Jump(skipLabel);

                builder.Mark(trueLabel);
                builder.SetA(1);

                builder.Mark(skipLabel);

                return LanguageType.Boolean;
            }
            default:
                throw new NotSupportedException();
        }

        return LanguageType.Integer;
    }

    public void CreateComparison(YabalBuilder builder, InstructionLabel trueLabel, InstructionLabel falseLabel)
    {
        SetRegisters(builder);

        switch (Operator)
        {
            case BinaryOperator.Add:
            case BinaryOperator.Subtract:
            case BinaryOperator.Multiply:
            case BinaryOperator.Divide:
                throw new NotSupportedException();
            case BinaryOperator.GreaterThan:
                builder.Sub();
                builder.JumpIfCarryBit(trueLabel);
                builder.Jump(falseLabel);
                break;
            case BinaryOperator.GreaterThanOrEqual:
                builder.Sub();
                builder.JumpIfZero(trueLabel);
                builder.JumpIfCarryBit(trueLabel);
                builder.Jump(falseLabel);
                break;
            case BinaryOperator.LessThan:
                builder.Sub();
                builder.JumpIfCarryBit(falseLabel);
                builder.Jump(trueLabel);
                break;
            case BinaryOperator.LessThanOrEqual:
                builder.Sub();
                builder.JumpIfZero(trueLabel);
                builder.JumpIfCarryBit(falseLabel);
                builder.Jump(trueLabel);
                break;
            case BinaryOperator.Equal:
                builder.Sub();
                builder.JumpIfZero(trueLabel);
                builder.Jump(falseLabel);
                break;
            case BinaryOperator.NotEqual:
                builder.Sub();
                builder.JumpIfZero(falseLabel);
                builder.Jump(trueLabel);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private void SetRegisters(YabalBuilder builder)
    {
        if (Right is IntegerExpression { IsSmall: true } intExpression)
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
