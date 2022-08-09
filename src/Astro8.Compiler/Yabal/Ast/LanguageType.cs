namespace Astro8.Yabal.Ast;

public record LanguageType(StaticType StaticType, LanguageType? ElementType = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
    public static LanguageType Array(LanguageType elementType) => new(StaticType.Array, elementType);
}
