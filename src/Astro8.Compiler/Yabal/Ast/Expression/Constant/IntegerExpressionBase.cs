using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IntegerExpression(SourceRange Range, int Value) : IntegerExpressionBase(Range)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public abstract record IntegerExpressionBase(SourceRange Range) : Expression(Range), IExpressionToB, IConstantValue
{
    public abstract int Value { get; init; }

    public bool IsSmall => Value is >= 0 and <= InstructionReference.MaxData;

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
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
    }

    void IExpressionToB.BuildExpressionToB(YabalBuilder builder)
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
    }

    bool IExpressionToB.OverwritesA => !IsSmall;

    object? IConstantValue.Value => Value;

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Integer;

    public override Expression CloneExpression()
    {
        return this;
    }
}
