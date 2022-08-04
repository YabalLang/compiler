using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record BooleanExpression(SourceRange Range, bool Value) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder)
    {
        builder.Instruction.SetA(Value ? 1 : 0);
        return LanguageType.Boolean;
    }
}