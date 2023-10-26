namespace Yabal.Ast;

public abstract record AssignableExpression(SourceRange Range) : Expression(Range)
{
    public abstract void Assign(YabalBuilder builder, Expression expression, SourceRange range);

    public abstract void LoadToA(YabalBuilder builder, int offset);

    public abstract void StoreFromA(YabalBuilder builder, int offset);

    public virtual void MarkModified()
    {
    }
}
