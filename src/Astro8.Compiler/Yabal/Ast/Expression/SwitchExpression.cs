using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record SwitchItem(List<Expression> Cases, Expression Value);

public record SwitchExpression(SourceRange Range, Expression Value, List<SwitchItem> Items, Expression? Default) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        LanguageType? returnType = null;

        void CheckReturnType(LanguageType type)
        {
            if (returnType == null)
            {
                returnType = type;
            }
            else if (returnType != type)
            {
                throw new InvalidOperationException("Return value type does not match with previous return value type");
            }
        }
        var valueType = Value.BuildExpression(builder, isVoid);

        using var tempValue = builder.GetTemporaryVariable();
        builder.StoreA(tempValue);

        var end = builder.CreateLabel();


        foreach (var item in Items)
        {
            var returnValue = builder.CreateLabel();
            var next = builder.CreateLabel();

            foreach (var caseValue in item.Cases)
            {
                if (caseValue.BuildExpression(builder, isVoid) != valueType)
                {
                    throw new InvalidOperationException("Case value type does not match switch value type");
                }

                builder.LoadB(tempValue);
                builder.Sub();
                builder.JumpIfZero(returnValue);
                builder.Jump(next);
            }

            builder.Mark(returnValue);
            CheckReturnType(item.Value.BuildExpression(builder, isVoid));
            builder.Jump(end);
            builder.Mark(next);
        }

        if (Default != null)
        {
            CheckReturnType(Default.BuildExpression(builder, isVoid));
        }

        builder.Mark(end);

        return returnType ?? throw new InvalidOperationException("No return value");
    }

    public override bool OverwritesB => true;
}
