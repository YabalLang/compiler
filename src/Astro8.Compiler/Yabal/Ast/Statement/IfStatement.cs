using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IfStatement(SourceRange Range, Expression Expression, Statement Consequent, Statement? Alternate) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        if (Expression is BooleanExpression { Value: var value })
        {
            if (value)
            {
                Consequent.Build(builder);
            }
            else
            {
                Alternate?.Build(builder);
            }

            return;
        }

        var consequentLabel = builder.CreateLabel();
        var alternateLabel = Alternate != null ? builder.CreateLabel() : null;
        var end = builder.CreateLabel();

        switch (Expression)
        {
            case BinaryExpression binaryExpression:
            {
                binaryExpression.CreateComparison(builder, alternateLabel ?? end, consequentLabel);
                break;
            }
            default:
            {
                var type = Expression.BuildExpression(builder, false);

                if (type != LanguageType.Boolean)
                {
                    throw new InvalidOperationException($"Expression must be of type boolean, but is {type}");
                }

                builder.SetB(0);
                builder.Sub();
                builder.JumpIfZero(alternateLabel ?? end);
                builder.Jump(end);
                break;
            }
        }

        builder.Mark(consequentLabel);
        Consequent.Build(builder);

        if (alternateLabel != null)
        {
            builder.Jump(end);
            builder.Mark(alternateLabel);
            Alternate!.Build(builder);
        }

        builder.Mark(end);
    }
}
