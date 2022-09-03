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
        return @object switch
        {
            IdentifierExpression expression => BuildIdentifier(builder, expression, value),
            ArrayAccessExpression arrayAccess => BuildArrayAccess(builder, arrayAccess, value, isVoid),
            _ => throw new NotSupportedException()
        };
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

        if (value is { Right: IExpressionToB { OverwritesA: false } expression })
        {
            var type = expression.BuildExpressionToB(builder);

            if (type != arrayType.ElementType)
            {
                throw new InvalidOperationException("Type mismatch");
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
            throw new InvalidOperationException("Type mismatch");
        }

        return type;
    }

    private static LanguageType BuildIdentifier(YabalBuilder builder, IdentifierExpression expression, Either<Func<LanguageType>, Expression> value)
    {
        if (!builder.TryGetVariable(expression.Name, out var variable))
        {
            throw new InvalidOperationException($"Variable {expression.Name} not found");
        }

        if (variable.IsConstant)
        {
            throw new InvalidOperationException($"Variable {expression.Name} is constant and cannot be assigned");
        }

        LanguageType type;

        if (value is { IsLeft: true, Left: var left })
        {
            type = left();
        }
        else if (value is { IsRight: true, Right: var right })
        {
            type = right.BuildExpression(builder, false);
        }
        else
        {
            throw new InvalidOperationException("Invalid value");
        }

        if (type != variable.Type)
        {
            throw new InvalidOperationException("Type mismatch");
        }


        builder.StoreA(variable.Pointer);
        builder.SetComment($"store value in variable '{variable.Name}'");
        return type;
    }
}
