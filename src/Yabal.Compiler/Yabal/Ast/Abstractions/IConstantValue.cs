using Yabal.Visitor;

namespace Yabal.Ast;

public interface IConstantValue
{
    object? Value { get; }
}

public interface IPointerSource : IExpression
{
    int Bank { get; }
}

public interface IVariableSource : IExpression
{
    (Variable, int? Offset) GetVariable(YabalBuilder builder);

    bool CanGetVariable { get; }
}
