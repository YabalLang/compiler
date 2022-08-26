using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IComparisonExpression
{
    void CreateComparison(YabalBuilder builder, InstructionLabel falseLabel, InstructionLabel trueLabel);
}

public record BinaryExpression(SourceRange Range, BinaryOperator Operator, Expression Left, Expression Right) : Expression(Range), IComparisonExpression
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        switch (Operator)
        {
            case BinaryOperator.Add:
                SetRegisters(builder);
                builder.Add();
                break;
            case BinaryOperator.Subtract:
                SetRegisters(builder);
                builder.Sub();
                break;
            case BinaryOperator.Multiply:
                SetRegisters(builder);
                builder.Mult();
                break;
            case BinaryOperator.Divide:
                SetRegisters(builder);
                builder.Div();
                break;
            case BinaryOperator.And:
                SetRegisters(builder);
                builder.And();
                break;
            case BinaryOperator.Or:
                SetRegisters(builder);
                builder.Or();
                break;
            case BinaryOperator.LeftShift:
                SetRegisters(builder);
                builder.BitShiftLeft();
                break;
            case BinaryOperator.RightShift:
                SetRegisters(builder);
                builder.BitShiftRight();
                break;
            case BinaryOperator.LessThan:
            case BinaryOperator.Equal:
            case BinaryOperator.NotEqual:
            case BinaryOperator.GreaterThan:
            case BinaryOperator.GreaterThanOrEqual:
            case BinaryOperator.LessThanOrEqual:
            case BinaryOperator.AndAlso:
            case BinaryOperator.OrElse:
            {
                var trueLabel = builder.CreateLabel();
                var falseLabel = builder.CreateLabel();
                var end = builder.CreateLabel();
                CreateComparison(builder, trueLabel, falseLabel);

                builder.Mark(trueLabel);
                builder.SetA(0);
                builder.Jump(end);

                builder.Mark(falseLabel);
                builder.SetA(1);

                builder.Mark(end);

                return LanguageType.Boolean;
            }
            default:
                throw new NotSupportedException();
        }

        return LanguageType.Integer;
    }

    public void CreateComparison(YabalBuilder builder, InstructionLabel falseLabel, InstructionLabel trueLabel)
    {
        if (Operator == BinaryOperator.OrElse)
        {
            Left.Build(builder);
            builder.SetB(1);
            builder.Sub();
            builder.JumpIfZero(trueLabel);

            Right.Build(builder);
            builder.SetB(1);
            builder.Sub();
            builder.JumpIfZero(trueLabel);

            builder.Jump(falseLabel);
            return;
        }

        if (Operator == BinaryOperator.AndAlso)
        {
            Left.Build(builder);
            builder.SetB(0);
            builder.Sub();
            builder.JumpIfZero(falseLabel);

            Right.Build(builder);
            builder.SetB(0);
            builder.Sub();
            builder.JumpIfZero(falseLabel);

            builder.Jump(trueLabel);
            return;
        }

        SetRegisters(builder);

        switch (Operator)
        {
            case BinaryOperator.GreaterThan:
                builder.Sub();
                builder.JumpIfZero(falseLabel);
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
                builder.JumpIfZero(falseLabel);
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
            using var variable = builder.GetTemporaryVariable();

            Left.Build(builder);
            builder.StoreA(variable);

            Right.Build(builder);
            builder.LoadB(variable);

            builder.SwapA_B();
        }
    }

    public override Expression Optimize()
    {
        var left = Left.Optimize();
        var right = Right.Optimize();

        if (Operator is BinaryOperator.Equal or BinaryOperator.NotEqual &&
            left is IConstantValue { Value: var leftValue } &&
            right is IConstantValue { Value: var rightValue })
        {
            return Operator switch
            {
                BinaryOperator.Equal => new BooleanExpression(Range, Equals(leftValue, rightValue)),
                BinaryOperator.NotEqual => new BooleanExpression(Range, !Equals(leftValue, rightValue)),
                _ => this
            };
        }

        if (Operator is BinaryOperator.AndAlso or BinaryOperator.OrElse &&
            left is BooleanExpression { Value: var leftBool } &&
            right is BooleanExpression { Value: var rightBool })
        {
            return Operator switch
            {
                BinaryOperator.AndAlso => new BooleanExpression(Range, leftBool && rightBool),
                BinaryOperator.OrElse => new BooleanExpression(Range, leftBool || rightBool),
                _ => this
            };
        }

        if (left is IConstantValue { Value: int leftInt } &&
            right is IConstantValue { Value: int rightInt })
        {
            return Operator switch
            {
                BinaryOperator.Add => new IntegerExpression(Range, leftInt + rightInt),
                BinaryOperator.Subtract => new IntegerExpression(Range, leftInt - rightInt),
                BinaryOperator.Multiply => new IntegerExpression(Range, leftInt * rightInt),
                BinaryOperator.Divide => new IntegerExpression(Range, leftInt / rightInt),
                BinaryOperator.GreaterThan => new BooleanExpression(Range, leftInt > rightInt),
                BinaryOperator.GreaterThanOrEqual => new BooleanExpression(Range, leftInt >= rightInt),
                BinaryOperator.LessThan => new BooleanExpression(Range, leftInt < rightInt),
                BinaryOperator.LessThanOrEqual => new BooleanExpression(Range, leftInt == rightInt),
                _ => this
            };
        }

        return this;
    }
}
