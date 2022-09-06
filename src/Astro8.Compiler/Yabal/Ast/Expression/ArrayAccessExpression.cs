using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface INode
{
    void Initialize(YabalBuilder builder);
}

public interface IExpression : INode
{
    LanguageType Type { get; }

    bool OverwritesB { get; }

    void BuildExpression(YabalBuilder builder, bool isVoid);
}

public interface IAssignExpression : IExpression
{
    void Assign(YabalBuilder builder, Expression expression);

    void StoreA(YabalBuilder builder);

    void MarkModified()
    {
    }
}

public interface IAddressExpression : IAssignExpression
{
    Pointer? Pointer { get; }

    void StoreAddressInA(YabalBuilder builder);

    void IAssignExpression.StoreA(YabalBuilder builder)
    {
        if (Type.Size > 1)
        {
            throw new NotImplementedException();
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
            builder.StoreB_ToAddressInA();
        }
        else
        {
            using var temp = builder.GetTemporaryVariable();
            builder.StoreA(temp);
            StoreAddressInA(builder);
            builder.LoadB(temp);
            builder.StoreB_ToAddressInA();
        }
    }

    void IAssignExpression.Assign(YabalBuilder builder, Expression expression)
    {
        if (Pointer is {} pointer)
        {
            builder.SetValue(pointer, expression);
        }
        else if (expression is IExpressionToB { OverwritesA: false } expressionToB)
        {
            var size = expression.Type.Size;

            if (size == 1)
            {
                StoreAddressInA(builder);

                if (expression is not IConstantValue { Value: 0 })
                {
                    expressionToB.BuildExpressionToB(builder);
                }
                else
                {
                    throw new NotSupportedException();
                }

                builder.StoreB_ToAddressInA();
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
                StoreA(builder);
            }
        }
        else
        {
            expression.BuildExpression(builder, false);
            StoreA(builder);
        }
    }
}

public record ArrayAccessExpression(SourceRange Range, Expression Array, Expression Key) : Expression(Range), IAddressExpression
{
    public override void Initialize(YabalBuilder builder)
    {
        Array.Initialize(builder);
        Key.Initialize(builder);

        if (Array.Type.StaticType != StaticType.Array)
        {
            builder.AddError(ErrorLevel.Error, Array.Range, ErrorMessages.ValueIsNotAnArray);
        }

        if (Key.Type.StaticType != StaticType.Integer)
        {
            builder.AddError(ErrorLevel.Error, Array.Range, ErrorMessages.ArrayOnlyIntegerKey);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();
    }

    public override bool OverwritesB => Array.OverwritesB || Key is not IntegerExpressionBase { Value: 0 };

    public override LanguageType Type => Array.Type.ElementType ?? LanguageType.Integer;

    public override Expression Optimize()
    {
        if (Array is not IConstantValue {Value: IAddress address} ||
            Key is not IConstantValue {Value: int index})
        {
            return this;
        }

        var value = address.GetValue(index);

        if (value.HasValue)
        {
            return new IntegerExpression(Range, value.Value);
        }

        return this;
    }

    public Pointer? Pointer
    {
        get
        {
            if (Array is not IAddressExpression {Pointer: { } pointer} ||
                Key is not IConstantValue {Value: int index})
            {
                return null;
            }

            var elementSize = Array.Type.ElementType?.Size ?? 1;
            return pointer.Add(index * elementSize);
        }
    }

    public void StoreAddressInA(YabalBuilder builder)
    {
        var elementSize = Array.Type.ElementType?.Size ?? 1;

        if (Array is IAddressExpression { Pointer: {} pointer } &&
            Key is IConstantValue { Value: int constantKey })
        {
            builder.SetA_Large(pointer.Add(constantKey * elementSize));
            return;
        }

        Array.BuildExpression(builder, false);

        if (Key is IntegerExpressionBase { Value: var intValue })
        {
            var offset = intValue * elementSize;

            if (offset == 0)
            {
                return;
            }

            builder.SetB(offset);
            builder.Add();

            builder.SetComment($"add {intValue} to pointer address");
            return;
        }

        if (!Key.OverwritesB && elementSize == 1)
        {
            builder.SwapA_B();
            Key.BuildExpression(builder, false);
            builder.Add();
            builder.SetComment("add to pointer address");
            return;
        }

        using var variable = builder.GetTemporaryVariable();
        builder.StoreA(variable);

        Key.BuildExpression(builder, false);

        if (elementSize > 1)
        {
            builder.SetB(elementSize);
            builder.Mult();
        }

        builder.LoadB(variable);
        builder.Add();

        builder.SetComment("add to pointer address");
    }

    public override string ToString()
    {
        return $"{Array}[{Key}]";
    }
}
