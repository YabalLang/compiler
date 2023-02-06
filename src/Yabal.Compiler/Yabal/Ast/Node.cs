namespace Yabal.Ast;

public abstract record Node(SourceRange Range) : INode
{
    public virtual void Initialize(YabalBuilder builder)
    {
    }

    public abstract void Build(YabalBuilder builder);
}
