using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IAssignExpression : IExpression
{
    void Assign(YabalBuilder builder, Expression expression);

    void AssignRegisterA(YabalBuilder builder);

    IAssignExpression Clone();

    void MarkModified()
    {
    }
}
