using Yabal.Instructions;

namespace Yabal.Ast;

public record IncludeFileExpression(SourceRange Range, string Path, FileType FileType) : Expression(Range), IConstantValue, IPointerSource, IBankSource
{
    private FileAddress _address = null!;
    private readonly LanguageType _type = LanguageType.Pointer(LanguageType.Integer);

    public override void Initialize(YabalBuilder builder)
    {
        _address = builder.GetFile(Range, Path, FileType);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        builder.SetA_Large(_address.Pointer);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        builder.SetA_Large(_address.Pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => _type;

    public object Value => _address;

    public void StoreConstantValue(Span<int> buffer)
    {
        buffer[0] = _address.Pointer.Address;
        buffer[1] = _address.Pointer.Bank;
    }

    public override IncludeFileExpression CloneExpression()
    {
        return new IncludeFileExpression(Range, Path, FileType)
        {
            _address = _address
        };
    }

    int IBankSource.Bank => 0;
    Pointer IPointerSource.Pointer => _address.Pointer;
}

public enum FileType
{
    Byte,
    Image,
    Font
}
