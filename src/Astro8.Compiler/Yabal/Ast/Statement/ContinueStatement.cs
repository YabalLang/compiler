using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ContinueStatement(SourceRange Range) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        if (builder.Block.Continue is null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.ContinueOutsideLoop);
        }
    }

    public override void Build(YabalBuilder builder)
    {
        builder.Jump(builder.Block.Continue);
    }

    public override Statement CloneStatement() => this;

    public override Statement Optimize() => this;
}
