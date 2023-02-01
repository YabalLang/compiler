using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

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
}
