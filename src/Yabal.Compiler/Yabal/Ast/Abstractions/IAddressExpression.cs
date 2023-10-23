namespace Yabal.Ast;

public abstract record AddressExpression(SourceRange Range) : AssignableExpression(Range)
{
    public abstract Pointer? Pointer { get; }

    public virtual int? Bank => Pointer?.Bank;

    public virtual bool DirectCopy => true;

    public virtual void StoreAddressInA(YabalBuilder builder)
    {
        StoreAddressInA(builder, 0);
    }

    public abstract void StoreAddressInA(YabalBuilder builder, int offset);

    public virtual void StoreBankInC(YabalBuilder builder)
    {
        throw new NotImplementedException();
    }

    public override void LoadToA(YabalBuilder builder, int offset)
    {
        if (Pointer is { } pointer)
        {
            builder.LoadA(pointer.Add(offset));
        }
        else
        {
            StoreAddressInA(builder, offset);

            if (Bank > 0) builder.SetBank(Bank.Value);
            builder.LoadA_FromAddressUsingA();
            if (Bank > 0) builder.SetBank(0);
        }
    }

    public override void StoreFromA(YabalBuilder builder, int offset)
    {
        if (Pointer is { } pointer)
        {
            builder.StoreA(pointer.Add(offset));
        }
        else if (!OverwritesB)
        {
            builder.SwapA_B();
            StoreAddressInA(builder, offset);
            builder.SwapA_B();

            if (Bank > 0) builder.SetBank(Bank.Value);
            builder.StoreB_ToAddressInA();
            if (Bank > 0) builder.SetBank(0);
        }
        else
        {
            using var temp = builder.GetTemporaryVariable();
            builder.StoreA(temp);
            StoreAddressInA(builder, offset);
            builder.LoadB(temp);

            if (Bank > 0) builder.SetBank(Bank.Value);
            builder.StoreB_ToAddressInA();
            if (Bank > 0) builder.SetBank(0);
        }
    }

    public override void Assign(YabalBuilder builder, Expression expression, SourceRange range)
    {
        if (Pointer is {} pointer)
        {
            builder.SetValue(pointer, Type, expression);
            return;
        }

        if (expression.Type.StaticType is StaticType.Pointer)
        {
            if (expression is IdentifierExpression {Variable: var variable})
            {
                StoreAddressInA(builder);
                builder.LoadB(variable.Pointer);
                builder.StoreB_ToAddressInA();

                StoreAddressInA(builder);
                builder.SetB(1);
                builder.Add();

                builder.LoadB(variable.Pointer.Add(1));
                builder.StoreB_ToAddressInA();
            }
            else if (expression is AddressExpression { Pointer: { } valuePointer })
            {
                StoreAddressInA(builder);
                builder.SetB_Large(valuePointer);
                builder.StoreB_ToAddressInA();

                builder.SetB(1);
                builder.Add();

                builder.SetB(valuePointer.Bank);
                builder.StoreB_ToAddressInA();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            var size = expression.Type.Size;

            if (size == 1 && expression is IExpressionToB { OverwritesA: false } expressionToB)
            {
                if (!Bank.HasValue)
                {
                    StoreBankInC(builder);
                    builder.DisallowC++;
                }

                StoreAddressInA(builder);
                builder.SetComment("load address");

                expressionToB.BuildExpressionToB(builder);
                builder.SetComment("set value in B");

                if (Bank is not {} bank)
                {
                    builder.DisallowC--;
                    builder.SetBank_FromC();
                    builder.StoreB_ToAddressInA();
                    builder.SetBank(0);
                }
                else if (bank == 0)
                {
                    builder.StoreB_ToAddressInA();
                }
                else
                {
                    builder.SetBank(bank);
                    builder.StoreB_ToAddressInA();
                    builder.SetBank(0);
                }

                ZeroRemainingBytes(builder, expression.Type, range);

                return;
            }

            if (expression is AddressExpression { Pointer: {} valuePointer })
            {
                for (var i = 0; i < size; i++)
                {
                    valuePointer.LoadToA(builder, i);
                    StoreFromA(builder, i);
                }

                ZeroRemainingBytes(builder, expression.Type, range);

                return;
            }

            expression.BuildExpression(builder, false, Type);
            StoreFromA(builder, 0);
            ZeroRemainingBytes(builder, expression.Type, range);
        }
    }

    private void ZeroRemainingBytes(YabalBuilder builder, LanguageType expressionType, SourceRange range)
    {
        var missingBytes = Type.Size - expressionType.Size;

        if (missingBytes == 0)
        {
            return;
        }

        builder.AddError(ErrorLevel.Warning, range, $"Assigning a value of type {expressionType} to a variable of type {Type} will only copy the first byte(s) of the value, the remaining byte(s) will be set to 0.");

        for (var i = expressionType.Size; i < Type.Size; i++)
        {
            builder.SetA(0);
            StoreFromA(builder, i);
        }
    }

    public abstract override AddressExpression CloneExpression();
}
