using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ContinueStatement(SourceRange Range) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        if (builder.Block.Continue is null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.ContinueOutsideLoop);
            return;
        }

        builder.Jump(builder.Block.Continue);
    }
}
