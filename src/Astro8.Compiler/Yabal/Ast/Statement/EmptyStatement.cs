using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record EmptyStatement(SourceRange Range) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
    }

    public override Statement CloneStatement() => this;

    public override Statement Optimize() => this;
}
