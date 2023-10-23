namespace Yabal.Ast;

public record CreatePointerExpression(SourceRange Range, Expression Value, int BankValue, LanguageType ElementType) : AddressExpression(Range), IConstantValue, IPointerSource
{
    public override void Initialize(YabalBuilder builder)
    {
        Value.Initialize(builder);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        Value.BuildExpression(builder, false, null);
        pointer.StoreA(builder);

        if (suggestedType.Size == 2)
        {
            builder.SetA(1);
            pointer.StoreA(builder, offset: 1);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        Value.BuildExpression(builder, isVoid, suggestedType);
    }

    public override int? Bank => BankValue;

    public override void StoreAddressInA(YabalBuilder builder, int offset)
    {
        Value.BuildExpression(builder, false, null);
    }

    public bool HasConstantValue => Pointer is not null;

    object? IConstantValue.Value => Pointer is {} pointer
        ? RawAddress.From(ElementType, pointer)
        : null;

    public void StoreConstantValue(Span<int> buffer)
    {
        if (Pointer is { } pointer)
        {
            buffer[0] = pointer.Address;
            buffer[1] = pointer.Bank;
        }
        else
        {
            throw new InvalidOperationException("Cannot store a null pointer.");
        }
    }

    public override bool OverwritesB => Value.OverwritesB;

    public override LanguageType Type => LanguageType.Pointer(ElementType);

    public override CreatePointerExpression CloneExpression()
    {
        return new CreatePointerExpression(Range, Value.CloneExpression(), BankValue, ElementType);
    }

    public override Pointer? Pointer => Value is IConstantValue { Value: int value }
        ? new AbsolutePointer(value, BankValue)
        : null;

    int IPointerSource.Bank => BankValue;
}
