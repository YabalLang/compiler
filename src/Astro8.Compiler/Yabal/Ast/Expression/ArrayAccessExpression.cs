using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ArrayAccessExpression(SourceRange Range, Expression Array, Expression Key) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (Array is IConstantValue { Value: IAddress constantAddress } &&
            Key is IConstantValue { Value: int constantKey })
        {
            switch (constantAddress.Get(builder))
            {
                case { IsLeft: true, Left: var left }:
                    builder.LoadA_Large(left + constantKey);
                    break;
                case { IsRight: true, Right: var right }:
                    builder.LoadA_Large(right);
                    builder.SetPointerOffset(constantKey);
                    break;
            }

            return LanguageType.Integer;
        }

        var type = StoreAddressInA(builder, Array, Key);
        builder.LoadA_FromAddressUsingA();
        return type.ElementType!;
    }

    public override bool OverwritesB => Array.OverwritesB || Key is not IntegerExpressionBase { Value: 0 };

    public static LanguageType StoreAddressInA(YabalBuilder builder, Expression array, Expression key)
    {
        var type = array.BuildExpression(builder, false);

        if (type != LanguageType.Assembly && (type.StaticType != StaticType.Pointer || type.ElementType == null))
        {
            throw new InvalidOperationException("Array access expression can only be used on arrays");
        }

        if (key is IntegerExpressionBase { Value: var intValue })
        {
            if (intValue == 0)
            {
                return type;
            }

            builder.SetB(intValue);
            builder.Add();

            builder.SetComment($"add {intValue} to pointer address");
        }
        else if (!key.OverwritesB)
        {
            builder.SwapA_B();
            var keyType = key.BuildExpression(builder, false);

            if (keyType.StaticType != StaticType.Integer)
            {
                throw new InvalidOperationException("Array access expression can only be used on arrays");
            }
            builder.Add();

            builder.SetComment("add to pointer address");
        }
        else
        {
            using var variable = builder.GetTemporaryVariable();
            builder.StoreA(variable);

            var keyType = key.BuildExpression(builder, false);

            if (keyType.StaticType != StaticType.Integer)
            {
                throw new InvalidOperationException("Array access expression can only be used on arrays");
            }

            builder.LoadB(variable);
            builder.Add();

            builder.SetComment("add to pointer address");
        }

        return type;
    }
}
