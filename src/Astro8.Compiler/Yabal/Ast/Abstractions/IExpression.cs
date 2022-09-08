using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IExpression : INode
{
    LanguageType Type { get; }

    bool OverwritesB { get; }

    void BuildExpression(YabalBuilder builder, bool isVoid);
}