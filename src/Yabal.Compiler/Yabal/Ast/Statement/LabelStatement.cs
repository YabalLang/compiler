namespace Yabal.Ast;

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

    public override Statement CloneStatement() => this;

    public override Statement Optimize() => this;
}
