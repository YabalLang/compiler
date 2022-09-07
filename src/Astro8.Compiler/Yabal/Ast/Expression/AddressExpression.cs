using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AddressExpression(SourceRange Range, Pointer Pointer, LanguageType PointerType) : Expression(Range), IAddressExpression
{
    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();
    }

    public void StoreAddressInA(YabalBuilder builder)
    {
        builder.SetA_Large(Pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => PointerType;

    Pointer IAddressExpression.Pointer => Pointer;

    public int? Bank => Pointer.Bank;

    public override AddressExpression CloneExpression() => new(Range, Pointer, PointerType);

    IAddressExpression IAddressExpression.Clone() => CloneExpression();
}
