using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public abstract record AddressExpression(SourceRange Range) : AssignableExpression(Range)
{
    public abstract Pointer? Pointer { get; }

    public abstract int? Bank { get; }

    public abstract void StoreAddressInA(YabalBuilder builder);

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
        }
        else if (expression is IExpressionToB { OverwritesA: false } expressionToB)
        {
            var size = expression.Type.Size;

            if (size == 1)
            {
                StoreAddressInA(builder);
                expressionToB.BuildExpressionToB(builder);

                if (Bank > 0) builder.SetBank(Bank.Value);

                builder.StoreB_ToAddressInA();

                if (Bank > 0) builder.SetBank(0);
            }
            else if (expression is AddressExpression { Pointer: {} valuePointer })
            {
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
            }
            else
            {
                expression.BuildExpression(builder, false);
                AssignRegisterA(builder);
            }
        }
        else
        {
            expression.BuildExpression(builder, false);
            AssignRegisterA(builder);
        }
    }

    public abstract override AddressExpression CloneExpression();
}
