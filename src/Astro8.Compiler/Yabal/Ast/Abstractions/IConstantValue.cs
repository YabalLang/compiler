namespace Astro8.Yabal.Ast;

public interface IConstantValue
{
    object? Value { get; }
}

public interface IPointerSource : IExpression
{
    int Bank { get; }
}
