namespace Yabal.Ast;

public record ExpressionStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        Expression.BuildExpression(builder, true, null);

        if (Expression is AddressExpression address and (IdentifierExpression or MemberExpression))
        {
            address.ShowDebug(builder);
        }
    }

    public override Statement CloneStatement()
    {
        return new ExpressionStatement(Range, Expression.CloneExpression());
    }

    public override Statement Optimize()
    {
        return new ExpressionStatement(Range, Expression switch
        {
            IdentifierExpression or MemberExpression => Expression,
            _ => Expression.Optimize(null)
        });
    }
}
