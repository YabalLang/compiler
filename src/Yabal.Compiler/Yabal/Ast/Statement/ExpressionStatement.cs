namespace Yabal.Ast;

public record ExpressionStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        if (builder.Debug && Expression is CallExpression { Type.StaticType: not (StaticType.Void or StaticType.Unknown) } callExpression)
        {
            var temp = builder.GetTemporaryVariable(global: true, size: callExpression.Type.Size);

            callExpression.BuildExpressionToPointer(builder, callExpression.Type, temp);

            builder.AddVariableDebug(Range, callExpression.Type, temp);
        }
        else
        {
            Expression.BuildExpression(builder, true, null);

            if (Expression is AddressExpression address and (IdentifierExpression or MemberExpression or ArrayAccessExpression))
            {
                address.ShowDebug(builder);
            }
        }
    }

    public bool ShowDebug => Expression is
        (AddressExpression and (IdentifierExpression or MemberExpression or ArrayAccessExpression)) or
        (CallExpression { Type.StaticType: not StaticType.Void });

    public override Statement CloneStatement()
    {
        return new ExpressionStatement(Range, Expression.CloneExpression());
    }

    public override Statement Optimize()
    {
        return new ExpressionStatement(Range, Expression switch
        {
            IdentifierExpression or MemberExpression or ArrayAccessExpression or CallExpression => Expression,
            _ => Expression.Optimize(null)
        });
    }
}
