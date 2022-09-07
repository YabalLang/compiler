using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record ForStatement(SourceRange Range, Statement? Init, Statement? Update, Expression Test, BlockStatement Body) : ScopeStatement(Range)
{
    public override void OnDeclare(YabalBuilder builder)
    {
        Init?.Declare(builder);
        Update?.Declare(builder);
        Body.Declare(builder);
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        Init?.Initialize(builder);
        Update?.Initialize(builder);
        Test.Initialize(builder);
        Body.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        var next = builder.CreateLabel();
        var body = builder.CreateLabel();
        var end = builder.CreateLabel();
        var test = builder.CreateLabel();

        Block.Continue = next;
        Block.Break = end;

        Init?.Build(builder);
        builder.Jump(test);

        builder.Mark(next);
        Update?.Build(builder);

        builder.Mark(test);
        Test.CreateComparison(builder, end, body);

        builder.Mark(body);
        Body.Build(builder);
        builder.Jump(next);
        builder.SetComment("jump to next iteration");

        builder.Mark(end);
    }

    public override Statement CloneStatement()
    {
        return new ForStatement(
            Range,
            Init?.CloneStatement(),
            Update?.CloneStatement(),
            Test.CloneExpression(),
            Body.CloneStatement()
        );
    }

    public override Statement Optimize()
    {
        return new ForStatement(
            Range,
            Init?.Optimize(),
            Update?.Optimize(),
            Test.Optimize(),
            Body.Optimize()
        )
        {
            Block = Block
        };
    }
}
