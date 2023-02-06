﻿using Yabal.Exceptions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record MemberExpression(SourceRange Range, AddressExpression Expression, string Name) : AddressExpression(Range), IVariableSource
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

        var field = type.StructReference?.Fields.FirstOrDefault(f => f.Name == Name);
        _field = field ?? throw new InvalidCodeException($"Struct {Expression.Type} does not contain a field named {Name}", Range);
    }

    public override void AssignRegisterA(YabalBuilder builder)
    {
        if (_field.Bit is {} bit)
        {
            builder.StoreBitInA(Expression, bit);
            return;
        }

        base.AssignRegisterA(builder);
    }

    public override void Assign(YabalBuilder builder, Expression expression)
    {
        if (_field.Bit is {} bit)
        {
            builder.StoreBit(Expression, expression, bit);
            return;
        }

        base.Assign(builder, expression);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();

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

    public (Variable, int? Offset) GetVariable(YabalBuilder builder)
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
        Expression.Type.StructReference?.Fields.FirstOrDefault(i => i.Name == Name)?.Type ??
        LanguageType.Unknown;

    public override Pointer? Pointer => Expression is { Pointer: {} pointer } && !_field.Bit.HasValue
        ? pointer.Add(_field.Offset)
        : null;

    public override int? Bank => Expression.Bank;

    public override void StoreAddressInA(YabalBuilder builder)
    {
        Expression.StoreAddressInA(builder);

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
        return new MemberExpression(Range, Expression.CloneExpression(), Name);
    }
}