using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record StringExpression(SourceRange Range, string Value) : Expression(Range), IConstantValue
{
    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.SetA_Large(builder.GetString(Value));
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.String;

    object IConstantValue.Value { get; } = StringAddress.From(Value);

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}
