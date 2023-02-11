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

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        if (_callExpression is { } callExpression)
        {
            callExpression.BuildExpression(builder, isVoid);
        }
        else
        {
            Expression.BuildExpression(builder, false);
        }
    }

    public override bool OverwritesB => Expression.OverwritesB;

    public override LanguageType Type => CastType;

    public override Pointer? Pointer => _callExpression is null ? (Expression as AddressExpression)?.Pointer : null;

    public override bool DirectCopy => _callExpression is null && ((Expression as AddressExpression)?.DirectCopy ?? false);

    public override void StoreAddressInA(YabalBuilder builder)
    {
        if (_callExpression is null && Expression is AddressExpression addressExpression)
        {
            addressExpression.StoreAddressInA(builder);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public override Expression Optimize()
    {
        return _callExpression?.Optimize() ?? new CastExpression(Range, CastType, Expression.Optimize()) { _callExpression = _callExpression };
    }

    public override AddressExpression CloneExpression()
    {
        return new CastExpression(Range, Type, Expression.CloneExpression()) { _callExpression = _callExpression };
    }
}
