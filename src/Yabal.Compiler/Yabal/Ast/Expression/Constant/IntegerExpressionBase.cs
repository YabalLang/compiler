using Yabal.Instructions;

namespace Yabal.Ast;

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

    public void StoreConstantValue(Span<int> buffer)
    {
        buffer[0] = Value;
    }

    public bool IsSmall => Value is >= 0 and <= InstructionReference.MaxData;

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpressionCore(builder, false, suggestedType);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
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
            builder.SetB_Large(Value);
            builder.SetComment("load large integer");
        }
    }

    bool IExpressionToB.OverwritesA => false;

    object? IConstantValue.Value => Value;

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Integer;

    public override Expression CloneExpression()
    {
        return this;
    }
}
