namespace Yabal.Ast;

public interface INode
{
    SourceRange Range { get; }

    void Initialize(YabalBuilder builder);
}
