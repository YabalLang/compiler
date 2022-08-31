using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IConstantValue
{
    object? Value { get; }
}

public interface IExpressionToB
{
    LanguageType BuildExpressionToB(YabalBuilder builder);

    bool OverwritesA { get; }
}
