using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BlockStatement(SourceRange Range, List<Statement> Statements, bool NewScope = true) : Statement(Range)
{
    public override void Declare(YabalBuilder builder)
    {
        foreach (var statement in Statements)
        {
            statement.Declare(builder);
        }
    }

    public override BlockStatement CloneStatement()
    {
        return new BlockStatement(
            Range,
            Statements.Select(s => s.CloneStatement()).ToList(),
            NewScope
        );
    }

    public override BlockStatement Optimize()
    {
        return new BlockStatement(
            Range,
            Statements.Select(s => s.Optimize()).ToList(),
            NewScope
        );
    }

    public override void Initialize(YabalBuilder builder)
    {
        foreach (var statement in Statements)
        {
            statement.Initialize(builder);
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
