namespace Yabal.Ast;

public abstract record AddressExpression(SourceRange Range) : AssignableExpression(Range)
{
    public abstract Pointer? Pointer { get; }

    public virtual int? Bank => Pointer?.Bank;

    public virtual bool DirectCopy => true;

    public abstract void StoreAddressInA(YabalBuilder builder);

    public virtual void StoreBankInC(YabalBuilder builder)
    {
        throw new NotImplementedException();
    }

    public override void AssignRegisterA(YabalBuilder builder)
    {
        if (Type.Size > 1)
        {
            throw new NotSupportedException();
        }

        if (Pointer is { } pointer)
        {
            builder.StoreA(pointer);
        }
        else if (!OverwritesB)
        {
            builder.SwapA_B();
            StoreAddressInA(builder);
            builder.SwapA_B();

            if (Bank > 0) builder.SetBank(Bank.Value);
            builder.StoreB_ToAddressInA();
            if (Bank > 0) builder.SetBank(0);
        }
        else
        {
            using var temp = builder.GetTemporaryVariable();
            builder.StoreA(temp);
            StoreAddressInA(builder);
            builder.LoadB(temp);

            if (Bank > 0) builder.SetBank(Bank.Value);
            builder.StoreB_ToAddressInA();
            if (Bank > 0) builder.SetBank(0);
        }
    }

    public override void Assign(YabalBuilder builder, Expression expression)
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

                return;
            }

            if (expression is AddressExpression { Pointer: {} valuePointer })
            {
                if (expression.Type.StaticType == StaticType.Pointer)
                {
                    StoreAddressInA(builder);

                    return;
                }

                for (var i = 0; i < size; i++)
                {
                    StoreAddressInA(builder);

                    if (i > 0)
                    {
                        builder.SetB(i);
                        builder.Add();
                    }

                    builder.SwapA_B();
                    valuePointer.LoadToA(builder, i);

                    builder.SwapA_B();
                    builder.StoreB_ToAddressInA();
                }

                return;
            }

            expression.BuildExpression(builder, false);
            AssignRegisterA(builder);
        }
    }

    public abstract override AddressExpression CloneExpression();
}
