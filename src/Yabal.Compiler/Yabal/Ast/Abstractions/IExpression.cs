namespace Yabal.Ast;

public interface IExpression : INode
{
    LanguageType Type { get; }

    bool OverwritesB { get; }

    void BuildExpression(YabalBuilder builder, bool isVoid, LanguageType? suggestedType);
}
