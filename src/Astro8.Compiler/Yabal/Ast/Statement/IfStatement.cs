using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IfStatement(SourceRange Range, Expression Expression, Statement Consequent, Statement? Alternate) : Statement(Range)
{
    public override void Declare(YabalBuilder builder)
    {
        Consequent.Declare(builder);
        Alternate?.Declare(builder);
    }

    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
        Consequent.Initialize(builder);
        Alternate?.Initialize(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        if (Expression.Optimize() is IConstantValue { Value: bool value })
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
            case IComparisonExpression binaryExpression:
            {
                binaryExpression.CreateComparison(builder, alternateLabel ?? end, consequentLabel);
                break;
            }
            default:
            {
                Expression.BuildExpression(builder, false);
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
