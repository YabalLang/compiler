namespace Yabal.Ast;

public record UsingStatement(SourceRange Range, Namespace Namespace) : Statement(Range)
{
    public override void Declare(YabalBuilder builder)
    {
        builder.Block.AddUsing(Namespace);
    }

    public override void Build(YabalBuilder builder)
    {
    }

    public override Statement CloneStatement()
    {
        return new UsingStatement(Range, Namespace);
    }

    public override Statement Optimize()
    {
        return this;
    }
}
