using Yabal.Instructions;

namespace Yabal.Ast;

public record StringExpression(SourceRange Range, string Value) : AddressExpression(Range), IConstantValue, IPointerSource
{
    private InstructionPointer _pointer = null!;

    public override void Initialize(YabalBuilder builder)
    {
        _pointer = builder.GetString(Value);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        builder.SetA_Large(_pointer);
        pointer.StoreA(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        builder.SetA_Large(_pointer);
    }

    public override void StoreAddressInA(YabalBuilder builder)
    {
        builder.SetA_Large(_pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => LanguageType.Pointer(LanguageType.Char);

    object IConstantValue.Value => StringAddress.From(Value, _pointer);

    public void StoreConstantValue(Span<int> buffer)
    {
        buffer[0] = _pointer.Address;
        buffer[1] = _pointer.Bank;
    }

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
