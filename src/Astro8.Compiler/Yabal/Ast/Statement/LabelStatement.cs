using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record LabelStatement(SourceRange Range, string Name) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        var label = builder.CreateLabel();

        if (!builder.Block.Labels.TryAdd(Name, label))
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.DuplicateLabel(Name));
            return;
        }

        builder.Mark(label);
    }
}
