namespace Yabal.Ast;

public record ArrayAccessExpression(SourceRange Range, AddressExpression Array, Expression Key) : AddressExpression(Range)
{
    public Expression Key { get; private set; } = Key;

    public override void Initialize(YabalBuilder builder)
    {
        Array.Initialize(builder);
        Key.Initialize(builder);

        if (Array.Type.StaticType != StaticType.Pointer)
        {
            builder.AddError(ErrorLevel.Error, Array.Range, ErrorMessages.ValueIsNotAnArray);
        }

        if (Key.Type.StaticType != StaticType.Integer)
        {
            if (builder.CastOperators.TryGetValue((Key.Type, LanguageType.Integer), out var cast))
            {
                Key = new CallExpression(
                    Key.Range,
                    cast,
                    new List<Expression> { Key }
                );

                Key.Initialize(builder);
            }
            else
            {
                builder.AddError(ErrorLevel.Error, Key.Range, ErrorMessages.ArrayOnlyIntegerKey);
            }
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        for (var i = 0; i < suggestedType.Size; i++)
        {
            LoadValue(builder, i);
            pointer.StoreA(builder, i);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        LoadValue(builder, 0);
    }

    public void LoadValue(YabalBuilder builder, int offset)
    {
        StoreAddressInA(builder, offset);

        if (Array.Bank > 0) builder.SetBank(Array.Bank.Value);
        builder.LoadA_FromAddressUsingA();
        if (Array.Bank > 0) builder.SetBank(0);
    }

    public override bool OverwritesB => Array.OverwritesB || Key is not IntegerExpressionBase { Value: 0 };

    public override LanguageType Type => Array.Type.ElementType ?? LanguageType.Integer;

    public override Expression Optimize(LanguageType? suggestedType)
    {
        var key = Key.Optimize(suggestedType);

        if (Array is not IConstantValue {Value: IAddress address} ||
            key is not IConstantValue {Value: int index})
        {
            return new ArrayAccessExpression(Range, Array, key);
        }

        var value = address.GetValue(index);

        if (value.HasValue)
        {
            return new IntegerExpression(Range, value.Value);
        }

        return new ArrayAccessExpression(Range, Array, key);
    }

    public override Pointer? Pointer
    {
        get
        {
            if (Array.Type.IsReference)
            {
                return null;
            }

            if (Array is not {Pointer: { } pointer} ||
                Key is not IConstantValue {Value: int index})
            {
                return null;
            }

            var elementSize = Array.Type.ElementType?.Size ?? 1;
            return pointer.Add(index * elementSize);
        }
    }

    public override int? Bank => Array.Bank;

    public override void StoreAddressInA(YabalBuilder builder, int pointerOffset)
    {
        var elementSize = Array.Type.ElementType?.Size ?? 1;

        if (Array is { Pointer: {} pointer } &&
            Key is IConstantValue { Value: int constantKey })
        {
            builder.SetA_Large(pointer.Add(constantKey * elementSize + pointerOffset));
            return;
        }

        if (Key is IConstantValue { Value: int intValue })
        {
            var offset = intValue * elementSize + pointerOffset;

            Array.BuildExpression(builder, false, null);

            if (offset == 0)
            {
                return;
            }

            builder.SetB(offset);
            builder.Add();

            builder.SetComment($"add {intValue} to pointer address");
            return;
        }

        if (elementSize > 1 && !Array.OverwritesB)
        {
            Key.BuildExpression(builder, false, null);
            builder.SetB(elementSize);
            builder.Mult();

            if (pointerOffset > 0)
            {
                builder.SetB(pointerOffset);
                builder.Add();
            }

            builder.SwapA_B();

            Array.BuildExpression(builder, false, null);
            builder.Add();
            return;
        }

        Array.BuildExpression(builder, false, null);

        if (elementSize == 1)
        {
            if (Key is IExpressionToB expressionToB)
            {
                expressionToB.BuildExpressionToB(builder);
                builder.Add();

                if (pointerOffset > 0)
                {
                    builder.SetB(pointerOffset);
                    builder.Add();
                }

                builder.SetComment("add to pointer address");
                return;
            }

            if (builder.DisallowC == 0 && !Key.OverwritesB)
            {
                builder.SwapA_B();
                Key.BuildExpression(builder, false, null);
                builder.Add();

                if (pointerOffset > 0)
                {
                    builder.SetB(pointerOffset);
                    builder.Add();
                }

                builder.SetComment("add to pointer address");
                return;
            }
        }

        using var variable = builder.GetTemporaryVariable();
        builder.StoreA(variable);

        Key.BuildExpression(builder, false, null);

        if (elementSize > 1)
        {
            builder.SetB(elementSize);
            builder.Mult();
        }

        builder.LoadB(variable);
        builder.Add();

        if (pointerOffset > 0)
        {
            builder.SetB(pointerOffset);
            builder.Add();
        }

        builder.SetComment("add to pointer address");
    }

    public override void StoreBankInC(YabalBuilder builder)
    {
        Array.StoreAddressInA(builder);
        builder.SetB(1);
        builder.Add();
        builder.LoadA_FromAddressUsingA();
        builder.SwapA_C();
        builder.SetComment("store bank in C");
    }

    public override string ToString()
    {
        return $"{Array}[{Key}]";
    }

    public override ArrayAccessExpression CloneExpression()
    {
        return new ArrayAccessExpression(
            Range,
            Array.CloneExpression(),
            Key.CloneExpression());
    }
}
