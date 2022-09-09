using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record IncludeFileExpression(SourceRange Range, string Path, FileType FileType) : Expression(Range), IConstantValue, IPointerSource
{
    private InstructionPointer _pointer = null!;

    public override void Initialize(YabalBuilder builder)
    {
        _pointer = builder.GetFile(Path, FileType);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.SetA_Large(_pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type { get; } = LanguageType.Pointer(LanguageType.Integer);

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
    Image
}
