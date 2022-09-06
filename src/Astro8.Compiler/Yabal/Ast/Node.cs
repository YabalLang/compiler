using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public abstract record Node(SourceRange Range) : INode
{
    public virtual void Initialize(YabalBuilder builder)
    {
    }

    public abstract void Build(YabalBuilder builder);
}
