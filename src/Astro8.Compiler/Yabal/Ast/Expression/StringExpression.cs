using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record StringExpression(SourceRange Range, string Value) : Expression(Range), IConstantValue
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(builder.GetString(Value));
        return LanguageType.Pointer(LanguageType.Integer);
    }

    public override bool OverwritesB => false;

    object IConstantValue.Value { get; } = StringAddress.From(Value);
}
