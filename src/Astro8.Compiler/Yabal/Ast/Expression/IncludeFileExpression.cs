using Astro8.Instructions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Astro8.Yabal.Ast;

public record IncludeFileExpression(SourceRange Range, string Path, FileType FileType) : Expression(Range), IConstantValue
{
    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(builder.GetFile(Path, FileType));
    }

    public override bool OverwritesB => false;

    public override LanguageType Type { get; } = LanguageType.Array(LanguageType.Integer);

    public object Value => FileAddress.From(Path, FileType);

    public override Expression CloneExpression()
    {
        return new IncludeFileExpression(Range, Path, FileType);
    }
}

public enum FileType
{
    Byte,
    Image
}
