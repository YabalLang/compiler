using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record UnaryExpression(SourceRange Range, Expression Value, UnaryOperator Operator) : Expression(Range), IComparisonExpression
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        switch (Operator)
        {
            case UnaryOperator.Not:
            {
                var valueType = Value.BuildExpression(builder, isVoid);

                if (valueType != LanguageType.Integer)
                {
                    builder.AddError(ErrorLevel.Error, Value.Range, ErrorMessages.UnaryOperatorNotApplicableToType(Operator, valueType));
                }

                builder.Not();
                return LanguageType.Integer;
            }
            case UnaryOperator.Negate:
            {
                var valueType = Value.BuildExpression(builder, isVoid);

                if (valueType != LanguageType.Boolean)
                {
                    builder.AddError(ErrorLevel.Error, Value.Range, ErrorMessages.UnaryOperatorNotApplicableToType(Operator, valueType));
                }

                var skip = builder.CreateLabel();

                builder.SetB(1);
                builder.Sub();
                builder.JumpIfZero(skip);
                builder.SetA(1);
                builder.Mark(skip);

                return LanguageType.Boolean;
            }
            case UnaryOperator.Minus:
            {
                var valueType = Value.BuildExpression(builder, isVoid);

                if (valueType != LanguageType.Integer)
                {
                    builder.AddError(ErrorLevel.Error, Value.Range, ErrorMessages.UnaryOperatorNotApplicableToType(Operator, valueType));
                }

                builder.SetB(-1);
                builder.Mult();
                return LanguageType.Integer;
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

        var type = Value.BuildExpression(builder, false);

        if (type != LanguageType.Boolean)
        {
            builder.AddError(ErrorLevel.Error, Value.Range, ErrorMessages.ExpectedBoolean(type));
        }

        builder.SetB(0);
        builder.Sub();
        builder.JumpIfZero(falseLabel);
        builder.Jump(trueLabel);
    }

    public override Expression Optimize(BlockCompileStack block)
    {
        return Operator switch
        {
            UnaryOperator.Negate when Value is IConstantValue { Value: bool value } => new BooleanExpression(Range, !value),
            UnaryOperator.Not when Value is IConstantValue { Value: int value } => new IntegerExpression(Range, ~value),
            UnaryOperator.Minus when Value is IConstantValue { Value: int value } => new IntegerExpression(Range, -value),
            _ => this
        };
    }

    public override bool OverwritesB => true;
}
