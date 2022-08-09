using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ArrayAccessExpression(SourceRange Range, Expression Array, Expression Key) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        var type = StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();
        return type.ElementType!;
    }

    public LanguageType StoreAddressInA(YabalBuilder builder)
    {
        var type = Array.BuildExpression(builder);

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
            var keyType = Key.BuildExpression(builder);
            var valueOffset = builder.Count;

            if (keyType.StaticType != StaticType.Integer)
            {
                throw new InvalidOperationException("Array access expression can only be used on arrays");
            }

            if (watcher.B)
            {
                builder.StoreA(builder.Temp, valueOffset);
                builder.LoadB(builder.Temp);
            }

            builder.Add();
        }

        return type;
    }
}
