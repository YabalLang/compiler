using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record StringExpression(SourceRange Range, string Value) : Expression(Range), IConstantValue
{
    private InstructionPointer _pointer = null!;

    public override void Initialize(YabalBuilder builder)
    {
        _pointer = builder.GetString(Value);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.SetA_Large(_pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Array(LanguageType.Integer);

    object IConstantValue.Value => StringAddress.From(Value, _pointer);

    public override string ToString()
    {
        return $"\"{Value}\"";
    }

    public override Expression CloneExpression()
    {
        return this;
    }
}
