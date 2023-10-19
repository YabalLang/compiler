using Yabal.Instructions;

namespace Yabal.Ast;

public record UnaryExpression(SourceRange Range, Expression Value, UnaryOperator Operator) : Expression(Range), IComparisonExpression
{
    public override void Initialize(YabalBuilder builder)
    {
        Value.Initialize(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        switch (Operator)
        {
            case UnaryOperator.Not:
                Value.BuildExpression(builder, isVoid, suggestedType);
                builder.Not();
                break;
            case UnaryOperator.Minus:
                Value.BuildExpression(builder, isVoid, suggestedType);
                builder.SetB(-1);
                builder.Mult();
                break;
            case UnaryOperator.Negate:
            {
                Value.BuildExpression(builder, isVoid, suggestedType);
                var skip = builder.CreateLabel();

                builder.SetB(1);
                builder.Sub();
                builder.JumpIfZero(skip);
                builder.SetA(1);
                builder.Mark(skip);
                break;
            }
            default:
                throw new InvalidOperationException();
        }
    }

    public void CreateComparison(YabalBuilder builder, InstructionLabel falseLabel, InstructionLabel trueLabel)
    {
        if (Operator != UnaryOperator.Negate)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.InvalidComparison);
            return;
        }

        if (Value is IComparisonExpression comparisonExpression)
        {
            comparisonExpression.CreateComparison(builder, trueLabel, falseLabel);
            return;
        }

        Value.BuildExpression(builder, false, null);
        builder.SetB(0);
        builder.Sub();
        builder.JumpIfZero(falseLabel);
        builder.Jump(trueLabel);
    }

    public override Expression Optimize()
    {
        var value = Value.Optimize();

        return Operator switch
        {
            UnaryOperator.Negate when value is IConstantValue { Value: bool b } => new BooleanExpression(Range, !b),
            UnaryOperator.Not when value is IConstantValue { Value: int i } => new IntegerExpression(Range, ~i & ushort.MaxValue),
            UnaryOperator.Minus when value is IConstantValue { Value: int i } => new IntegerExpression(Range, -i),
            _ => this
        };
    }

    public override bool OverwritesB => true;

    public override LanguageType Type =>
        Operator switch
        {
            UnaryOperator.Not => LanguageType.Integer,
            UnaryOperator.Negate => LanguageType.Boolean,
            UnaryOperator.Minus => LanguageType.Integer,
            _ => throw new InvalidOperationException()
        };

    public override Expression CloneExpression()
    {
        return new UnaryExpression(Range, Value.CloneExpression(), Operator);
    }
}
