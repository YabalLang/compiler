using Yabal.Visitor;

namespace Yabal.Ast;

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

    public (IVariable, int? Offset) GetVariable(YabalBuilder builder)
    {
        if (Expression is not IVariableSource variableSource)
        {
            builder.AddError(ErrorLevel.Error, Range, "Cannot take reference of non-variable expression");
            return default;
        }

        return variableSource.GetVariable(builder);
    }

    public bool CanGetVariable => Expression is IVariableSource;

    public override LanguageType Type => LanguageType.Reference(Expression.Type);

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpressionCore(builder, false, suggestedType);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        if (Expression.Type.StaticType == StaticType.Reference)
        {
            Expression.BuildExpression(builder, isVoid, suggestedType);
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

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new ReferenceExpression(Range, Expression.Optimize(suggestedType));
    }
}
