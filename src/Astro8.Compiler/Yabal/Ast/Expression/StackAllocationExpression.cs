using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record StackAllocationExpression(SourceRange Range, LanguageType PointerType, Expression Length) : AddressExpression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        builder.HasStackAllocation = true;
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        using var temp = builder.GetTemporaryVariable(global: true);

        builder.LoadA(builder.StackAllocPointer);
        builder.StoreA(temp);

        Length.BuildExpression(builder, false);
        builder.LoadB(builder.StackAllocPointer);
        builder.Add();
        builder.StoreA(builder.StackAllocPointer);

        builder.LoadA(temp);
    }

    public override bool OverwritesB => true;

    public override bool DirectCopy => false;

    public override LanguageType Type => LanguageType.ReferencePointer(PointerType);

    public override Pointer? Pointer => null;

    public override void StoreAddressInA(YabalBuilder builder)
    {
        BuildExpressionCore(builder, false);
    }

    public override AddressExpression CloneExpression()
    {
        return new StackAllocationExpression(Range, PointerType, Length);
    }

    public override Expression Optimize()
    {
        return new StackAllocationExpression(Range, PointerType, Length.Optimize());
    }
}
