using Yabal.Visitor;

namespace Yabal.Ast;

public interface IConstantValue
{
    bool HasConstantValue => true;

    object? Value { get; }

    void StoreConstantValue(Span<int> buffer);
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
