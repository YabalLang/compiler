using Astro8.Instructions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Astro8.Yabal.Ast;

public record IncludeFileExpression(SourceRange Range, string Path, FileType Type) : Expression(Range), IConstantValue
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(builder.GetFile(Path, Type));
        return LanguageType.Array(LanguageType.Integer);
    }

    public override bool OverwritesB => false;

    public object Value => FileAddress.From(Path, Type);

}

public enum FileType
{
    Byte,
    Image
}
