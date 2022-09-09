using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IExpressionToB
{
    void BuildExpressionToB(YabalBuilder builder);

    bool OverwritesA { get; }
}
