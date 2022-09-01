using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BreakStatement(SourceRange Range) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        builder.Jump(builder.Block.Break ?? throw new InvalidOperationException("Cannot continue outside of a loop"));
    }
}
