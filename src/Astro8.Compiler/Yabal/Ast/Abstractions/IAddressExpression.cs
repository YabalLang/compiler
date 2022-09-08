using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IAddressExpression : IAssignExpression
{
    Pointer? Pointer { get; }

    int? Bank { get; }

    void StoreAddressInA(YabalBuilder builder);

    new IAddressExpression Clone();

    IAssignExpression IAssignExpression.Clone() => Clone();

    void IAssignExpression.AssignRegisterA(YabalBuilder builder)
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

    void IAssignExpression.Assign(YabalBuilder builder, Expression expression)
    {
        if (Pointer is {} pointer)
        {
            builder.SetValue(Type, pointer, expression);
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
            else if (expression is IAddressExpression { Pointer: {} valuePointer })
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
}
