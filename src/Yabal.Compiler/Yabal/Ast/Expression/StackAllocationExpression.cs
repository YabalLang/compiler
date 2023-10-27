namespace Yabal.Ast;

public record StackAllocationExpression(SourceRange Range, LanguageType PointerType, Expression Length) : AddressExpression(Range), IConstantValue, IPointerSource, IBankSource
{
    private int? _fixedOffset;

    public override void Initialize(YabalBuilder builder)
    {
        builder.HasStackAllocation = true;

        if (builder.Block.IsGlobal && Length is IConstantValue { Value: int length })
        {
            var size = PointerType.Size;
            _fixedOffset = builder.Options.StackAllocationStart;
            builder.Options.StackAllocationStart += size * length;
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpressionCore(builder, false, suggestedType);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        if (_fixedOffset.HasValue)
        {
            builder.SetA(_fixedOffset.Value);
        }
        else
        {
            using var temp = builder.GetTemporaryVariable(global: true);

            builder.LoadA(builder.StackAllocPointer);
            builder.StoreA(temp);

            Length.BuildExpression(builder, false, suggestedType);
            builder.LoadB(builder.StackAllocPointer);
            builder.Add();
            builder.StoreA(builder.StackAllocPointer);

            builder.LoadA(temp);
        }
    }

    public override bool OverwritesB => true;

    public override bool DirectCopy => false;

    public override LanguageType Type => LanguageType.RefPointer(PointerType);

    public override Pointer? Pointer => _fixedOffset.HasValue ? new AbsolutePointer(_fixedOffset.Value, 0) : null;

    public override void StoreAddressInA(YabalBuilder builder, int offset)
    {
        BuildExpressionCore(builder, false, null);
    }

    public override AddressExpression CloneExpression()
    {
        return new StackAllocationExpression(Range, PointerType, Length)
        {
            _fixedOffset = _fixedOffset
        };
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new StackAllocationExpression(Range, PointerType, Length.Optimize(suggestedType))
        {
            _fixedOffset = _fixedOffset
        };
    }

    object? IConstantValue.Value => Pointer is {} pointer
        ? RawAddress.From(LanguageType.Pointer(PointerType), pointer)
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

    int IBankSource.Bank => 0;
}
