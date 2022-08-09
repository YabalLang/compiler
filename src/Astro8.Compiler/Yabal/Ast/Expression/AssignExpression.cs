using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AssignExpression(SourceRange Range, Expression Object, Expression Value) : Expression(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Value.BeforeBuild(builder);
    }

    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        return Object switch
        {
            IdentifierExpression expression => BuildIdentifier(builder, expression),
            ArrayAccessExpression arrayAccess => BuildArrayAccess(builder, arrayAccess),
            _ => throw new NotSupportedException()
        };
    }

    private LanguageType BuildArrayAccess(YabalBuilder builder, ArrayAccessExpression arrayAccess)
    {
        var arrayType = arrayAccess.StoreAddressInA(builder);
        var valueOffset = builder.Count;

        builder.SwapA_B();

        using var watcher = builder.WatchRegister();

        var type = Value.BuildExpression(builder);

        if (type != arrayType.ElementType)
        {
            throw new InvalidOperationException("Type mismatch");
        }

        if (watcher.B)
        {
            builder.StoreA(builder.Temp, valueOffset);
            builder.LoadB(builder.Temp);
        }

        builder.SwapA_B();
        builder.StoreB_ToAddressInA();

        return type;
    }

    private LanguageType BuildIdentifier(YabalBuilder builder, IdentifierExpression expression)
    {
        if (!builder.TryGetVariable(expression.Name, out var variable))
        {
            throw new InvalidOperationException($"Variable {expression.Name} not found");
        }

        var type = Value.BuildExpression(builder);

        if (type != variable.Type)
        {
            throw new InvalidOperationException("Type mismatch");
        }

        builder.StoreA(variable.Pointer);
        return type;
    }
}
