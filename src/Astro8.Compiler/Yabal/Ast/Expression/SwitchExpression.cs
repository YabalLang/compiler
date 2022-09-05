using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record SwitchItem(List<Expression> Cases, Expression Value);

public record SwitchExpression(SourceRange Range, Expression Value, List<SwitchItem> Items, Expression? Default) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        LanguageType? returnType = null;

        void CheckReturnType(LanguageType type, SourceRange range)
        {
            if (returnType == null)
            {
                returnType = type;
            }
            else if (returnType != type)
            {
                builder.AddError(ErrorLevel.Error, range, ErrorMessages.SwitchReturnTypeMismatch(returnType, type));
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
                var caseType = caseValue.BuildExpression(builder, isVoid);

                if (caseType != valueType)
                {
                    builder.AddError(ErrorLevel.Error, caseValue.Range, ErrorMessages.SwitchCaseTypeMismatch(valueType, caseType));
                }

                builder.LoadB(tempValue);
                builder.Sub();
                builder.JumpIfZero(returnValue);
                builder.Jump(next);
            }

            builder.Mark(returnValue);
            CheckReturnType(item.Value.BuildExpression(builder, isVoid), item.Value.Range);
            builder.Jump(end);
            builder.Mark(next);
        }

        if (Default != null)
        {
            CheckReturnType(Default.BuildExpression(builder, isVoid), Default.Range);
        }

        builder.Mark(end);

        return returnType ?? throw new InvalidOperationException("No return value");
    }

    public override bool OverwritesB => true;
}
