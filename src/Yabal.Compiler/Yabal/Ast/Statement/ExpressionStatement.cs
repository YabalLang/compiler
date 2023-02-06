namespace Yabal.Ast;

public record ExpressionStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        Expression.BuildExpression(builder, true);
    }

    public override Statement CloneStatement()
    {
        return new ExpressionStatement(Range, Expression.CloneExpression());
    }

    public override Statement Optimize() => new ExpressionStatement(Range, Expression.Optimize());
}
