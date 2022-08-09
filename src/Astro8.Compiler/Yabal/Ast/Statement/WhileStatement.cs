using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record WhileStatement(SourceRange Range, Expression Expression, BlockStatement Block) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        var next = builder.CreateLabel();
        var body = builder.CreateLabel();
        var end = builder.CreateLabel();

        builder.Mark(next);

        switch (Expression)
        {
            case BinaryExpression binaryExpression:
                binaryExpression.CreateComparison(builder, body, end);
                break;
            case BooleanExpression { Value: true }:
                builder.Jump(body);
                break;
            case BooleanExpression { Value: false }:
                builder.Jump(next);
                break;
            default:
            {
                var type = Expression.BuildExpression(builder);

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
        builder.Mark(end);
    }
}
