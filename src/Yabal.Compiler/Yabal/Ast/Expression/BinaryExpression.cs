using Yabal.Exceptions;
using Yabal.Instructions;

namespace Yabal.Ast;

public interface IComparisonExpression
{
    void CreateComparison(YabalBuilder builder, InstructionLabel falseLabel, InstructionLabel trueLabel);
}

public static class ExpressionExtensions
{
    public static void CreateComparison(this Expression expression, YabalBuilder builder, InstructionLabel falseLabel, InstructionLabel trueLabel)
    {
        if (expression is IComparisonExpression comparisonExpression)
        {
            comparisonExpression.CreateComparison(builder, falseLabel, trueLabel);
        }
        else
        {
            expression.BuildExpression(builder, false, LanguageType.Boolean);
            builder.SetB(0);
            builder.Sub();
            builder.JumpIfZero(falseLabel);
            builder.Jump(trueLabel);
        }
    }
}

public record BinaryExpression(SourceRange Range, BinaryOperator Operator, Expression Left, Expression Right) : Expression(Range), IComparisonExpression
{
    private LanguageType? _type;
    private CallExpression? _callExpression;

    public override void Initialize(YabalBuilder builder)
    {
        Left.Initialize(builder);
        Right.Initialize(builder);

        if (builder.BinaryOperators.TryGetValue((Operator, Left.Type, Right.Type), out var function))
        {
            _type = function.ReturnType;
            _callExpression = new CallExpression(Range, function, new List<Expression> { Left, Right });
            _callExpression.Initialize(builder);
        }
        else
        {
            _type = Operator switch
            {
                BinaryOperator.Add => LanguageType.Integer,
                BinaryOperator.Subtract => LanguageType.Integer,
                BinaryOperator.Multiply => LanguageType.Integer,
                BinaryOperator.Divide => LanguageType.Integer,
                BinaryOperator.Modulo => LanguageType.Integer,
                BinaryOperator.GreaterThan => LanguageType.Boolean,
                BinaryOperator.GreaterThanOrEqual => LanguageType.Boolean,
                BinaryOperator.LessThan => LanguageType.Boolean,
                BinaryOperator.LessThanOrEqual => LanguageType.Boolean,
                BinaryOperator.LeftShift => LanguageType.Integer,
                BinaryOperator.RightShift => LanguageType.Integer,
                BinaryOperator.And => LanguageType.Integer,
                BinaryOperator.Or => LanguageType.Integer,
                BinaryOperator.Xor => LanguageType.Integer,
                BinaryOperator.Equal => LanguageType.Boolean,
                BinaryOperator.NotEqual => LanguageType.Boolean,
                BinaryOperator.AndAlso => LanguageType.Boolean,
                BinaryOperator.OrElse => LanguageType.Boolean,
                _ => throw new NotSupportedException()
            };
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        if (_callExpression != null)
        {
            _callExpression.BuildExpressionToPointer(builder, suggestedType, pointer);
        }
        else
        {
            BuildExpressionCore(builder, false, suggestedType);
            pointer.StoreA(builder);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        if (_callExpression != null)
        {
            _callExpression.BuildExpression(builder, isVoid, suggestedType);
            return;
        }

        if (Left.Type.StaticType is not (StaticType.Integer or StaticType.Boolean) ||
            Right.Type.StaticType is not (StaticType.Integer or StaticType.Boolean))
        {
            throw new InvalidCodeException($"Binary operator {Operator} is not supported for {Left.Type} and {Right.Type}", Range);
        }

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
            case BinaryOperator.Xor:
            {
                using var value = builder.GetTemporaryVariable(global: true);
                using var left = builder.GetTemporaryVariable(global: true);
                using var right = builder.GetTemporaryVariable(global: true);

                Left.BuildExpression(builder, false, LanguageType.Integer);
                builder.StoreA(left);

                Right.BuildExpression(builder, false, LanguageType.Integer);
                builder.StoreA(right);

                builder.LoadA(left);
                builder.LoadB(right);
                builder.And();
                builder.StoreA(value);

                builder.LoadA(left);
                builder.LoadB(right);
                builder.Or();

                builder.LoadB(value);
                builder.Sub();

                break;
            }
            case BinaryOperator.Modulo:
            {
                SetRegisters(builder);
                using var temp = builder.GetTemporaryVariable(global: true);
                builder.StoreA(temp);
                builder.Div();
                builder.Mult();
                builder.LoadB(temp);
                builder.SwapA_B();
                builder.Sub();
                break;
            }
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
                break;
            }
            default:
                throw new NotSupportedException();
        }
    }

    public void CreateComparison(YabalBuilder builder, InstructionLabel falseLabel, InstructionLabel trueLabel)
    {
        if (_callExpression != null)
        {
            _callExpression.CreateComparison(builder, falseLabel, trueLabel);
            return;
        }

        if (Operator == BinaryOperator.OrElse)
        {
            Left.BuildExpression(builder, false, LanguageType.Boolean);
            builder.SetB(1);
            builder.Sub();
            builder.JumpIfZero(trueLabel);

            Right.BuildExpression(builder, false, LanguageType.Boolean);
            builder.SetB(1);
            builder.Sub();
            builder.JumpIfZero(trueLabel);

            builder.Jump(falseLabel);
            return;
        }

        if (Operator == BinaryOperator.AndAlso)
        {
            Left.BuildExpression(builder, false, LanguageType.Boolean);
            builder.SetB(0);
            builder.Sub();
            builder.JumpIfZero(falseLabel);

            Right.BuildExpression(builder, false, LanguageType.Boolean);
            builder.SetB(0);
            builder.Sub();
            builder.JumpIfZero(falseLabel);

            builder.Jump(trueLabel);
            return;
        }

        SetRegisters(builder);

        Jump(Operator, builder, falseLabel, trueLabel);
    }

    public static void Jump(BinaryOperator @operator, YabalBuilder builder, PointerOrData falseLabel, PointerOrData trueLabel)
    {
        switch (@operator)
        {
            case BinaryOperator.GreaterThan:
                builder.Sub();
                builder.SetComment("operator >");
                builder.JumpIfZero(falseLabel);
                builder.JumpIfCarryBit(trueLabel);
                builder.Jump(falseLabel);
                break;
            case BinaryOperator.GreaterThanOrEqual:
                builder.Sub();
                builder.SetComment("operator >=");
                builder.JumpIfZero(trueLabel);
                builder.JumpIfCarryBit(trueLabel);
                builder.Jump(falseLabel);
                break;
            case BinaryOperator.LessThan:
                builder.Sub();
                builder.SetComment("operator <");
                builder.JumpIfZero(falseLabel);
                builder.JumpIfCarryBit(falseLabel);
                builder.Jump(trueLabel);
                break;
            case BinaryOperator.LessThanOrEqual:
                builder.Sub();
                builder.SetComment("operator <=");
                builder.JumpIfZero(trueLabel);
                builder.JumpIfCarryBit(falseLabel);
                builder.Jump(trueLabel);
                break;
            case BinaryOperator.Equal:
                builder.Sub();
                builder.SetComment("operator ==");
                builder.JumpIfZero(trueLabel);
                builder.Jump(falseLabel);
                break;
            case BinaryOperator.NotEqual:
                builder.Sub();
                builder.SetComment("operator !=");
                builder.JumpIfZero(falseLabel);
                builder.Jump(trueLabel);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private void SetRegisters(YabalBuilder builder)
    {
        if (Right is IExpressionToB { OverwritesA: false } right)
        {
            Left.BuildExpression(builder, false, LanguageType.Integer);
            right.BuildExpressionToB(builder);
        }
        else if (!Right.OverwritesB)
        {
            Left.BuildExpression(builder, false, LanguageType.Integer);
            builder.SwapA_B();

            Right.BuildExpression(builder, false, LanguageType.Integer);
            builder.SwapA_B();
        }
        else
        {
            using var variable = builder.GetTemporaryVariable();

            Left.BuildExpression(builder, false, LanguageType.Integer);
            builder.StoreA(variable);

            Right.BuildExpression(builder, false, LanguageType.Integer);
            builder.LoadB(variable);

            builder.SwapA_B();
        }
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        if (_callExpression != null)
        {
            return _callExpression.Optimize(suggestedType);
        }

        var left = Left.Optimize(suggestedType);
        var right = Right.Optimize(suggestedType);

        if (Operator is BinaryOperator.Equal or BinaryOperator.NotEqual &&
            left is IConstantValue { Value: {} leftValue } &&
            right is IConstantValue { Value: {} rightValue })
        {
            return Operator switch
            {
                BinaryOperator.Equal => new BooleanExpression(Range, Equals(leftValue, rightValue)),
                BinaryOperator.NotEqual => new BooleanExpression(Range, !Equals(leftValue, rightValue)),
                _ => new BinaryExpression(Range, Operator, left, right) { _type = _type }
            };
        }

        if (Operator is BinaryOperator.AndAlso or BinaryOperator.OrElse &&
            left is IConstantValue { Value: bool leftBool } &&
            right is IConstantValue { Value: bool rightBool })
        {
            return Operator switch
            {
                BinaryOperator.AndAlso => new BooleanExpression(Range, leftBool && rightBool),
                BinaryOperator.OrElse => new BooleanExpression(Range, leftBool || rightBool),
                _ => new BinaryExpression(Range, Operator, left, right) { _type = _type }
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
                BinaryOperator.Modulo => new IntegerExpression(Range, leftInt % rightInt),
                BinaryOperator.GreaterThan => new BooleanExpression(Range, leftInt > rightInt),
                BinaryOperator.GreaterThanOrEqual => new BooleanExpression(Range, leftInt >= rightInt),
                BinaryOperator.LessThan => new BooleanExpression(Range, leftInt < rightInt),
                BinaryOperator.LessThanOrEqual => new BooleanExpression(Range, leftInt == rightInt),
                BinaryOperator.LeftShift => new IntegerExpression(Range, leftInt << rightInt),
                BinaryOperator.RightShift => new IntegerExpression(Range, leftInt >> rightInt),
                BinaryOperator.And => new IntegerExpression(Range, leftInt & rightInt),
                BinaryOperator.Or => new IntegerExpression(Range, leftInt | rightInt),
                BinaryOperator.Xor => new IntegerExpression(Range, leftInt ^ rightInt),
                _ => new BinaryExpression(Range, Operator, left, right) { _type = _type }
            };
        }

        return new BinaryExpression(Range, Operator, left, right) { _type = _type };
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => _type ?? throw new InvalidOperationException("Type not set");

    public override Expression CloneExpression()
    {
        return new BinaryExpression(
            Range,
            Operator,
            Left.CloneExpression(),
            Right.CloneExpression()
        )  { _type = _type };
    }
}
