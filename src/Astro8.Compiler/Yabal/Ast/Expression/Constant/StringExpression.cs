using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record StringExpression(SourceRange Range, string Value) : AddressExpression(Range), IConstantValue, IPointerSource
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

    public override void StoreAddressInA(YabalBuilder builder)
    {
        builder.SetA_Large(_pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Pointer(LanguageType.Integer);

    object IConstantValue.Value => StringAddress.From(Value, _pointer);

    public override string ToString()
    {
        return $"\"{Value}\"";
    }

    public override StringExpression CloneExpression()
    {
        return this;
    }

    public override Pointer Pointer => _pointer;

    public override int? Bank => 0;

    int IPointerSource.Bank => 0;

    public override bool DirectCopy => false;
}
