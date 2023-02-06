using Yabal.Instructions;

namespace Yabal.Ast;

public record IncludeFileExpression(SourceRange Range, string Path, FileType FileType) : Expression(Range), IConstantValue, IPointerSource
{
    private InstructionPointer _pointer = null!;
    private readonly LanguageType _type = LanguageType.Pointer(LanguageType.Integer);

    public override void Initialize(YabalBuilder builder)
    {
        _pointer = builder.GetFile(Path, FileType);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.SetA_Large(_pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => _type;

    public object Value => FileAddress.From(Path, FileType, _pointer);

    public override IncludeFileExpression CloneExpression()
    {
        return new IncludeFileExpression(Range, Path, FileType)
        {
            _pointer = _pointer
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
