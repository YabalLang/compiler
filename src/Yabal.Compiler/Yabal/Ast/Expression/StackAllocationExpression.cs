namespace Yabal.Ast;

public record StackAllocationExpression(SourceRange Range, LanguageType PointerType, Expression Length) : AddressExpression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        builder.HasStackAllocation = true;
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpressionCore(builder, false, suggestedType);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
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

    public override bool OverwritesB => true;

    public override bool DirectCopy => false;

    public override LanguageType Type => LanguageType.RefPointer(PointerType);

    public override Pointer? Pointer => null;

    public override void StoreAddressInA(YabalBuilder builder)
    {
        BuildExpressionCore(builder, false, null);
    }

    public override AddressExpression CloneExpression()
    {
        return new StackAllocationExpression(Range, PointerType, Length);
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new StackAllocationExpression(Range, PointerType, Length.Optimize(suggestedType));
    }
}
