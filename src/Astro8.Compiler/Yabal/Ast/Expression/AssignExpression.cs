using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AssignExpression(SourceRange Range, Expression Object, Expression Value) : Expression(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Value.BeforeBuild(builder);
    }

    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        return SetValue(builder, Object, Value, isVoid);
    }

    public override bool OverwritesB => true;

    public static LanguageType SetValue(
        YabalBuilder builder,
        Expression @object,
        Either<Func<LanguageType>, Expression> value,
        bool isVoid = false)
    {
        switch (@object)
        {
            case IdentifierExpression expression:
                return BuildIdentifier(builder, expression, value);
            case ArrayAccessExpression arrayAccess:
                return BuildArrayAccess(builder, arrayAccess, value, isVoid);
            default:
                builder.AddError(ErrorLevel.Error, @object.Range, ErrorMessages.InvalidAssignmentTarget);
                return VisitValue(builder, value);
        }
    }

    private static LanguageType BuildArrayAccess(
        YabalBuilder builder,
        ArrayAccessExpression arrayAccess,
        Either<Func<LanguageType>, Expression> value,
        bool isVoid)
    {
        if (arrayAccess is { Array: IConstantValue { Value: IAddress constantAddress }, Key: IConstantValue { Value: int constantKey } } &&
            constantAddress.Get(builder) is {} pointer)
        {
            VisitValue(builder, value, LanguageType.Integer);

            switch (pointer)
            {
                case { IsLeft: true, Left: var left }:
                    builder.StoreA_Large(left + constantKey);
                    break;
                case { IsRight: true, Right: var right }:
                    builder.StoreA_Large(right);
                    builder.SetPointerOffset(constantKey);
                    break;
            }

            builder.SetComment("store value in pointer");
            return LanguageType.Integer;
        }

        var arrayType = ArrayAccessExpression.StoreAddressInA(builder, arrayAccess.Array, arrayAccess.Key);
        var valueRange = value.IsRight ? value.Right.Range : arrayAccess.Range;

        if (arrayType.ElementType == null)
        {
            builder.AddError(ErrorLevel.Error, arrayAccess.Array.Range, ErrorMessages.ValueIsNotAnArray);
            builder.SetA(0);
            return LanguageType.Integer;
        }

        if (value is { Right: IExpressionToB { OverwritesA: false } expression })
        {
            var type = expression.BuildExpressionToB(builder);

            if (type != arrayType.ElementType)
            {
                builder.AddError(ErrorLevel.Error, valueRange, ErrorMessages.InvalidType(type, arrayType));
            }
        }
        else if (value is { Right.OverwritesB: false })
        {
            builder.SwapA_B();
            VisitValue(builder, value, arrayType.ElementType);
            builder.SwapA_B();
            builder.StoreB_ToAddressInA();
        }
        else
        {
            using var address = builder.GetTemporaryVariable();
            builder.StoreA(address);
            VisitValue(builder, value, arrayType.ElementType);
            builder.LoadB(address);
            builder.SwapA_B();
        }

        builder.StoreB_ToAddressInA();
        builder.SetComment("store value in array");

        if (!isVoid)
        {
            builder.SwapA_B();
        }

        return arrayType.ElementType!;
    }

    private static LanguageType VisitValue(YabalBuilder builder, Either<Func<LanguageType>, Expression> value, LanguageType? type)
    {
        if (type == null)
        {
            throw new InvalidOperationException();
        }

        var valueType = value switch
        {
            { IsLeft: true, Left: var left } => left(),
            { IsRight: true, Right: var right } => right.BuildExpression(builder, false),
            _ => throw new InvalidOperationException("Invalid value")
        };

        if (type != valueType)
        {
            builder.AddError(ErrorLevel.Error, value.Right?.Range ?? SourceRange.Zero, ErrorMessages.InvalidType(valueType, type));
        }

        return type;
    }

    private static LanguageType BuildIdentifier(YabalBuilder builder, IdentifierExpression expression, Either<Func<LanguageType>, Expression> value)
    {
        var type = VisitValue(builder, value);

        if (!builder.TryGetVariable(expression.Name, out var variable))
        {
            builder.AddError(ErrorLevel.Error, expression.Range, ErrorMessages.UndefinedVariable(expression.Name));
            return type;
        }

        if (variable.IsConstant)
        {
            builder.AddError(ErrorLevel.Error, expression.Range, ErrorMessages.ConstantVariable(expression.Name));
            return type;
        }

        if (type != variable.Type)
        {
            builder.AddError(ErrorLevel.Error, expression.Range, ErrorMessages.InvalidType(type, variable.Type));
        }

        builder.StoreA(variable.Pointer);
        builder.SetComment($"store value in variable '{variable.Name}'");
        return type;
    }

    private static LanguageType VisitValue(YabalBuilder builder, Either<Func<LanguageType>, Expression> value)
    {
        LanguageType type;
        if (value is {IsLeft: true, Left: var left})
        {
            type = left();
        }
        else if (value is {IsRight: true, Right: var right})
        {
            type = right.BuildExpression(builder, false);
        }
        else
        {
            throw new InvalidOperationException("Invalid value");
        }

        return type;
    }
}
