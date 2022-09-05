using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record TernaryExpression(SourceRange Range, Expression Expression, Expression Consequent, Expression Alternate) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        var consequentLabel = builder.CreateLabel();
        var alternateLabel = builder.CreateLabel();
        var end = builder.CreateLabel();

        switch (Expression)
        {
            case IComparisonExpression binaryExpression:
            {
                binaryExpression.CreateComparison(builder, alternateLabel, consequentLabel);
                break;
            }
            default:
            {
                var type = Expression.BuildExpression(builder, false);

                if (type != LanguageType.Boolean)
                {
                    builder.AddError(ErrorLevel.Error, Expression.Range, ErrorMessages.ExpectedBoolean(type));
                }

                builder.SetB(0);
                builder.Sub();
                builder.JumpIfZero(alternateLabel);
                builder.Jump(end);
                break;
            }
        }

        builder.Mark(consequentLabel);
        var leftType = Consequent.BuildExpression(builder, isVoid);

        builder.Jump(end);
        builder.Mark(alternateLabel);

        var alternateType = Alternate.BuildExpression(builder, isVoid);
        if (alternateType != leftType)
        {
            builder.AddError(ErrorLevel.Error, Alternate.Range, ErrorMessages.InvalidType(leftType, alternateType));
        }

        builder.Mark(end);
        return leftType;
    }

    public override bool OverwritesB => true;
}
