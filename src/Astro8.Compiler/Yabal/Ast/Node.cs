using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public abstract record Node(SourceRange Range)
{
    public abstract void Build(YabalBuilder builder);
}
