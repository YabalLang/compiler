using Yabal.Exceptions;

namespace Yabal.Ast;

public record CastExpression(SourceRange Range, LanguageType CastType, Expression Expression) : AddressExpression(Range)
{
    private CallExpression? _callExpression;

    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);

        if (builder.CastOperators.TryGetValue((Expression.Type, CastType), out var castOperator))
        {
            _callExpression = new CallExpression(Range, castOperator, new List<Expression> { Expression });
        }
        else if (CastType.Size != Expression.Type.Size)
        {
            throw new InvalidCodeException("Cannot cast between types of different sizes", Range);
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        if (_callExpression is { } callExpression)
        {
            callExpression.BuildExpressionToPointer(builder, suggestedType, pointer);
        }
        else
        {
            Expression.BuildExpressionToPointer(builder, suggestedType, pointer);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        if (_callExpression is { } callExpression)
        {
            callExpression.BuildExpression(builder, isVoid, suggestedType);
        }
        else
        {
            Expression.BuildExpression(builder, false, suggestedType);
        }
    }

    public override bool OverwritesB => Expression.OverwritesB;

    public override LanguageType Type => CastType;

    public override Pointer? Pointer => _callExpression is null ? (Expression as AddressExpression)?.Pointer : null;

    public override bool DirectCopy => _callExpression is null && ((Expression as AddressExpression)?.DirectCopy ?? false);

    public override void StoreAddressInA(YabalBuilder builder, int offset)
    {
        if (_callExpression is null && Expression is AddressExpression addressExpression)
        {
            addressExpression.StoreAddressInA(builder, offset);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return _callExpression?.Optimize(suggestedType) ?? new CastExpression(Range, CastType, Expression.Optimize(suggestedType)) { _callExpression = _callExpression };
    }

    public override AddressExpression CloneExpression()
    {
        return new CastExpression(Range, Type, Expression.CloneExpression()) { _callExpression = _callExpression };
    }
}
