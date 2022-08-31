using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BlockStatement(SourceRange Range, List<Statement> Statements, bool NewScope = true) : Statement(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        foreach (var statement in Statements)
        {
            statement.BeforeBuild(builder);
        }
    }

    public override void Build(YabalBuilder builder)
    {
        if (NewScope)
        {
            builder.PushBlock();
        }

        foreach (var statement in Statements)
        {
            statement.Build(builder);
        }

        if (NewScope)
        {
            builder.PopBlock();
        }
    }
}
