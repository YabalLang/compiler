namespace Yabal.Ast;

public record GlobalNamespaceStatement(SourceRange Range, Namespace Namespace) : Statement(Range)
{
    public override void Declare(YabalBuilder builder)
    {
        builder.Block.Namespace = Namespace;
    }

    public override void Build(YabalBuilder builder)
    {
    }

    public override Statement CloneStatement()
    {
        return new GlobalNamespaceStatement(Range, Namespace);
    }

    public override Statement Optimize()
    {
        return this;
    }
}
