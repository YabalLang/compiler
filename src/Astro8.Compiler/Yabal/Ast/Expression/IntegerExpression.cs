using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IntegerExpression(SourceRange Range, int Value) : Expression(Range)
{
    public bool IsSmall => Value is >= 0 and <= InstructionReference.MaxDataLength;

    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        if (IsSmall)
        {
            builder.SetA(Value);
        }
        else
        {
            var pointer = builder.GetLargeValue(Value);
            builder.LoadA(pointer);
        }

        return LanguageType.Integer;
    }
}
