using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface INode
{
    SourceRange Range { get; }

    void Initialize(YabalBuilder builder);
}