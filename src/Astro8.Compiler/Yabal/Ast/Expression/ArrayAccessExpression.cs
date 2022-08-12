using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ArrayAccessExpression(SourceRange Range, Expression Array, Expression Key) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid = false)
    {
        return LoadValue(builder, Array, Key);
    }

    public static LanguageType LoadValue(YabalBuilder builder, Expression array, Expression Key)
    {
        var type = StoreAddressInA(builder, array, Key);
        builder.LoadA_FromAddressUsingA();
        return type.ElementType!;
    }

    public static LanguageType StoreAddressInA(YabalBuilder builder, Expression array, Expression Key)
    {
        var type = array.BuildExpression(builder, false);
        var arrayOffset = builder.Count;

        if (type.StaticType != StaticType.Array || type.ElementType == null)
        {
            throw new InvalidOperationException("Array access expression can only be used on arrays");
        }

        if (Key is IntegerExpression { Value: var intValue })
        {
            if (intValue != 0)
            {
                builder.SetB(intValue);
                builder.Add();
            }
        }
        else
        {
            builder.SwapA_B();

            using var watcher = builder.WatchRegister();
            var keyType = Key.BuildExpression(builder, false);

            if (keyType.StaticType != StaticType.Integer)
            {
                throw new InvalidOperationException("Array access expression can only be used on arrays");
            }

            if (watcher.B)
            {
                builder.StoreA(builder.TempPointer, arrayOffset);
                builder.LoadB(builder.TempPointer);
            }

            builder.Add();
        }

        return type;
    }
}
