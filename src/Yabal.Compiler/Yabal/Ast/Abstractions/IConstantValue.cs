using Yabal.Visitor;

namespace Yabal.Ast;

public interface IConstantValue
{
    bool HasConstantValue => true;

    object? Value { get; }

    void StoreConstantValue(Span<int> buffer);
}

public interface IBankSource : IExpression
{
    int Bank { get; }
}

public interface IPointerSource : IExpression
{
    Pointer? Pointer { get; }
}

public interface IVariableSource : IExpression
{
    (IVariable, int? Offset) GetVariable(YabalBuilder builder);

    bool CanGetVariable { get; }
}
