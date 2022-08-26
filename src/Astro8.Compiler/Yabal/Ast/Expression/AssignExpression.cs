using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AssignExpression(SourceRange Range, Expression Object, Expression Value) : Expression(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Value.BeforeBuild(builder);
    }

    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        return SetValue(builder, Object, () => Value.BuildExpression(builder, false));
    }

    public static LanguageType SetValue(YabalBuilder builder, Expression @object, Func<LanguageType> value)
    {
        return @object switch
        {
            IdentifierExpression expression => BuildIdentifier(builder, expression, value),
            ArrayAccessExpression arrayAccess => BuildArrayAccess(builder, arrayAccess, value),
            _ => throw new NotSupportedException()
        };
    }

    private static LanguageType BuildArrayAccess(YabalBuilder builder, ArrayAccessExpression arrayAccess, Func<LanguageType> value)
    {
        using var address = builder.GetTemporaryVariable();

        var arrayType = ArrayAccessExpression.StoreAddressInA(builder, arrayAccess.Array, arrayAccess.Key);
        builder.StoreA(address);

        var type = value();

        if (type != arrayType.ElementType)
        {
            throw new InvalidOperationException("Type mismatch");
        }

        builder.LoadB(address);

        builder.SwapA_B();
        builder.StoreB_ToAddressInA();

        return type;
    }

    private static LanguageType BuildIdentifier(YabalBuilder builder, IdentifierExpression expression, Func<LanguageType> value)
    {
        if (!builder.TryGetVariable(expression.Name, out var variable))
        {
            throw new InvalidOperationException($"Variable {expression.Name} not found");
        }

        var type = value();

        if (type != variable.Type)
        {
            throw new InvalidOperationException("Type mismatch");
        }

        builder.StoreA(variable.Pointer);
        return type;
    }
}
