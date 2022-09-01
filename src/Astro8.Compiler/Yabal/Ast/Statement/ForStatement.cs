using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ForStatement(SourceRange Range, Statement? Init, Statement? Update, Expression Test, BlockStatement Block) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        var block = builder.PushBlock();

        var next = builder.CreateLabel();
        var body = builder.CreateLabel();
        var end = builder.CreateLabel();
        var test = builder.CreateLabel();

        block.Continue = next;
        block.Break = end;

        Init?.Build(builder);
        builder.Jump(test);

        builder.Mark(next);
        Update?.Build(builder);

        builder.Mark(test);

        switch (Test)
        {
            case IComparisonExpression binaryExpression:
                binaryExpression.CreateComparison(builder, end, body);
                break;
            case BooleanExpression { Value: true }:
                builder.Jump(body);
                break;
            case BooleanExpression { Value: false }:
                builder.Jump(next);
                break;
            default:
            {
                var type = Test.BuildExpression(builder, false);

                if (type != LanguageType.Boolean)
                {
                    throw new InvalidOperationException($"Expression must be of type boolean, but is {type}");
                }

                builder.SetB(0);
                builder.Sub();
                builder.JumpIfZero(end);
                builder.Jump(body);
                break;
            }
        }

        builder.Mark(body);
        Block.Build(builder);
        builder.Jump(next);
        builder.SetComment("jump to next iteration");
        builder.Mark(end);

        builder.PopBlock();
    }
}
