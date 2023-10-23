using Yabal.Instructions;

namespace Yabal.Ast;

public record IncludeFileExpression(SourceRange Range, string Path, FileType FileType) : Expression(Range), IConstantValue, IPointerSource
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
        pointer.StoreA(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        builder.SetA_Large(_address.Pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => _type;

    public object Value => _address;

    public override IncludeFileExpression CloneExpression()
    {
        return new IncludeFileExpression(Range, Path, FileType)
        {
            _address = _address
        };
    }

    int IPointerSource.Bank => 0;
}

public enum FileType
{
    Byte,
    Image,
    Font
}
