using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ArrayAccessExpression(SourceRange Range, AddressExpression Array, Expression Key) : AddressExpression(Range)
{
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
            builder.AddError(ErrorLevel.Error, Key.Range, ErrorMessages.ArrayOnlyIntegerKey);
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
        var key = Key.Optimize();

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

    public override void StoreAddressInA(YabalBuilder builder)
    {
        var elementSize = Array.Type.ElementType?.Size ?? 1;

        if (Array is { Pointer: {} pointer } &&
            Key is IConstantValue { Value: int constantKey })
        {
            builder.SetA_Large(pointer.Add(constantKey * elementSize));
            return;
        }

        Array.BuildExpression(builder, false);

        if (Key is IConstantValue { Value: int intValue })
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

    public override ArrayAccessExpression CloneExpression()
    {
        return new ArrayAccessExpression(
            Range,
            Array.CloneExpression(),
            Key.CloneExpression());
    }
}
