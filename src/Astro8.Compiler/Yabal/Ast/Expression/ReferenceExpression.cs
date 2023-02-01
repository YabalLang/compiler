using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record ReferenceExpression(SourceRange Range, Expression Expression) : Expression(Range), IVariableSource
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);

        if (Expression is IdentifierExpression identifierExpression)
        {
            identifierExpression.MarkModified();
        }

        if (Expression.Type.StaticType == StaticType.Reference)
        {
            builder.AddError(ErrorLevel.Warning, Range, "Taking a reference of a reference is redundant");
        }
    }

    public override bool OverwritesB => Expression.OverwritesB;

    public (Variable, int? Offset) GetVariable(YabalBuilder builder)
    {
        if (Expression is not IVariableSource variableSource)
        {
            builder.AddError(ErrorLevel.Error, Range, "Cannot take reference of non-variable expression");
            return default;
        }

        return variableSource.GetVariable(builder);
    }

    public override LanguageType Type => LanguageType.Reference(Expression.Type);

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        if (Expression.Type.StaticType == StaticType.Reference)
        {
            Expression.BuildExpression(builder, isVoid);
            return;
        }

        if (Expression is not AddressExpression addressExpression)
        {
            builder.AddError(ErrorLevel.Error, Range, "Cannot take reference of non-address expression");
            return;
        }

        if (addressExpression.Bank != 0)
        {
            builder.AddError(ErrorLevel.Error, Range, "Cannot take reference of expression with non-zero bank");
            return;
        }

        addressExpression.StoreAddressInA(builder);
    }

    public override Expression CloneExpression()
    {
        return new ReferenceExpression(Range, Expression.CloneExpression());
    }

    public override Expression Optimize()
    {
        return new ReferenceExpression(Range, Expression.Optimize());
    }
}
