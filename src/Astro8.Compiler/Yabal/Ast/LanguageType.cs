namespace Astro8.Yabal.Ast;

public record LanguageType(StaticType StaticType, LanguageType? ElementType = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
    public static readonly LanguageType Assembly = new(StaticType.Assembly);
    public static LanguageType Pointer(LanguageType elementType) => new(StaticType.Pointer, elementType);
}
