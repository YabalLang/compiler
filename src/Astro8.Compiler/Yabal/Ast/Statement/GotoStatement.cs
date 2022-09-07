using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record GotoStatement(SourceRange Range, string Name) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        if (!builder.Block.TryGetLabel(Name, out var label))
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.UndefinedLabel(Name));
            return;
        }

        builder.Jump(label);
    }

    public override Statement CloneStatement() => this;

    public override Statement Optimize() => this;
}
