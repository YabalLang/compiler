using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BlockStatement(SourceRange Range, List<Statement> Statements) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        foreach (var statement in Statements)
        {
            statement.Build(builder);
        }
    }
}
