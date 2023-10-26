using Yabal.Exceptions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record MemberExpression(SourceRange Range, AddressExpression Expression, Identifier Name) : AddressExpression(Range), IVariableSource
{
    private LanguageStructField _field = null!;

    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);

        var type = Expression.Type;

        if (type.StaticType == StaticType.Reference)
        {
            type = type.ElementType!;
        }

        var field = type.StructReference?.Fields.FirstOrDefault(f => f.Name == Name.Name);
        _field = field ?? throw new InvalidCodeException($"Struct {Expression.Type} does not contain a field named {Name}", Range);
    }

    public override void LoadToA(YabalBuilder builder, int offset)
    {
        base.LoadToA(builder, offset);
        AfterLoad(builder);
    }

    public override void StoreFromA(YabalBuilder builder, int offset)
    {
        if (_field.Bit is {} bit)
        {
            builder.StoreBitInA(Expression, bit);
            return;
        }

        base.StoreFromA(builder, offset);
    }

    public override void Assign(YabalBuilder builder, Expression expression, SourceRange range)
    {
        if (_field.Bit is {} bit)
        {
            builder.StoreBit(Expression, expression, bit);
            return;
        }

        base.Assign(builder, expression, range);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        if (_field.Bit is not null)
        {
            BuildExpressionCore(builder, false, suggestedType);
            builder.StoreA(pointer);
        }
        else
        {
            var offset = _field.Offset;

            for (var i = 0; i < suggestedType.Size; i++)
            {
                Expression.StoreAddressInA(builder);

                if (i > 0)
                {
                    builder.SetB(offset + i);
                    builder.Add();
                }

                builder.LoadA_FromAddressUsingA();
                builder.StoreA(pointer.Add(i));
            }
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();
        AfterLoad(builder);
    }

    private void AfterLoad(YabalBuilder builder)
    {
        if (_field.Bit is {} bit)
        {
            if (bit.Offset > 0)
            {
                builder.SetB_Large(bit.Offset);
                builder.BitShiftRight();
            }

            builder.SetB_Large((1 << bit.Size) - 1);
            builder.And();
        }
    }

    public override bool OverwritesB => true;

    public (IVariable, int? Offset) GetVariable(YabalBuilder builder)
    {
        if (_field.Bit.HasValue)
        {
            throw new InvalidOperationException("Cannot get variable from bit field");
        }

        if (Expression is IdentifierExpression identifierExpression)
        {
            return (identifierExpression.Variable, _field.Offset);
        }

        throw new InvalidOperationException("Cannot get variable from expression");
    }

    public bool CanGetVariable => Expression is IdentifierExpression && !_field.Bit.HasValue;

    public override bool DirectCopy => !_field.Bit.HasValue;

    public override LanguageType Type =>
        Expression.Type.StructReference?.Fields.FirstOrDefault(i => i.Name == Name.Name)?.Type ??
        LanguageType.Unknown;

    public override Pointer? Pointer => Expression is { Pointer: {} pointer } && !_field.Bit.HasValue
        ? pointer.Add(_field.Offset)
        : null;

    public override int? Bank => Expression.Bank;

    public override void StoreAddressInA(YabalBuilder builder, int offset)
    {
        Expression.StoreAddressInA(builder, offset);

        if (_field.Offset > 0)
        {
            builder.SetB(_field.Offset);
            builder.Add();
        }
    }

    public override string ToString()
    {
        return $"{Expression}.{Name}";
    }

    public override MemberExpression CloneExpression()
    {
        return new MemberExpression(Range, (AddressExpression)Expression.CloneExpression(), Name);
    }
}
