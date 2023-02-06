namespace Yabal.Ast;

public interface IExpressionToB
{
    void BuildExpressionToB(YabalBuilder builder);

    bool OverwritesA { get; }
}
