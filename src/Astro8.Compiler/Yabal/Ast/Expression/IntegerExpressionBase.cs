using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IntegerExpression(SourceRange Range, int Value) : IntegerExpressionBase(Range);

public abstract record IntegerExpressionBase(SourceRange Range) : Expression(Range), IExpressionToB, IConstantValue
{
    public abstract int Value { get; init; }

    public bool IsSmall => Value is >= 0 and <= InstructionReference.MaxDataLength;

    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (IsSmall)
        {
            builder.SetA(Value);
            builder.SetComment("load small integer");
        }
        else
        {
            builder.SetA_Large(Value);
            builder.SetComment("load large integer");
        }

        return LanguageType.Integer;
    }

    public LanguageType BuildExpressionToB(YabalBuilder builder)
    {
        if (IsSmall)
        {
            builder.SetB(Value);
            builder.SetComment("load small integer");
        }
        else
        {
            builder.LoadA_Large(Value);
            builder.SwapA_B();
            builder.SetComment("load large integer");
        }

        return LanguageType.Integer;
    }

    bool IExpressionToB.OverwritesA => !IsSmall;

    object? IConstantValue.Value => Value;

    public override bool OverwritesB => false;
}
