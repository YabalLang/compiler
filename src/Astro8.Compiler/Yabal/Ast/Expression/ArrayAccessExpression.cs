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

        if (type.StaticType != StaticType.Array || type.ElementType == null)
        {
            throw new InvalidOperationException("Array access expression can only be used on arrays");
        }

        if (Key is IntegerExpression { Value: var intValue })
        {
            if (intValue == 0)
            {
                return type;
            }

            builder.SetB(intValue);
            builder.Add();
        }
        else
        {
            using var variable = builder.GetTemporaryVariable();
            builder.StoreA(variable);

            var keyType = Key.BuildExpression(builder, false);

            if (keyType.StaticType != StaticType.Integer)
            {
                throw new InvalidOperationException("Array access expression can only be used on arrays");
            }

            builder.LoadB(variable);
            builder.Add();
        }

        return type;
    }
}
