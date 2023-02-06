namespace Yabal.Ast;

public record BreakStatement(SourceRange Range) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        if (builder.Block.Break is null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.BreakOutsideLoop);
        }
    }

    public override void Build(YabalBuilder builder)
    {
        builder.Jump(builder.Block.Break);
    }

    public override Statement CloneStatement() => this;

    public override Statement Optimize() => this;
}
