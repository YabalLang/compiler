using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BlockStatement(SourceRange Range, List<Statement> Statements) : Statement(Range)
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
        foreach (var statement in Statements)
        {
            statement.Build(builder);
        }
    }

    public IEnumerable<Statement> EnumerableAll()
    {
        foreach (var statement in Statements)
        {
            if (statement is BlockStatement blockStatement)
            {
                foreach (var inner in blockStatement.EnumerableAll())
                {
                    yield return inner;
                }
            }
            else
            {
                yield return statement;
            }
        }
    }
}
