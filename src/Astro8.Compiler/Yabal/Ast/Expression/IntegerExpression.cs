using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IntegerExpression(SourceRange Range, int Value) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        if (Value <= InstructionReference.MaxDataLength)
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
