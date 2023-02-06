namespace Yabal.Ast;

public abstract record AssignableExpression(SourceRange Range) : Expression(Range)
{
    public abstract void Assign(YabalBuilder builder, Expression expression);

    public abstract void AssignRegisterA(YabalBuilder builder);

    public abstract override AssignableExpression CloneExpression();

    public virtual void MarkModified()
    {
    }
}
