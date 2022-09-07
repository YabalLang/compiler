using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record WhileStatement(SourceRange Range, Expression Expression, BlockStatement Body) : ScopeStatement(Range)
{
    public override void OnDeclare(YabalBuilder builder)
    {
        Body.Declare(builder);
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
        Body.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        var expression = Expression.Optimize();

        var next = builder.CreateLabel();
        var body = builder.CreateLabel();
        var end = builder.CreateLabel();

        Block.Continue = next;
        Block.Break = end;

        builder.Mark(next);

        if (expression is not IConstantValue {Value: true})
        {
            expression.CreateComparison(builder, end, body);
            builder.Mark(body);
        }

        Body.Build(builder);
        builder.Jump(next);
        builder.Mark(end);
    }
}
