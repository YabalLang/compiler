namespace Astro8.Yabal.Ast;

public record LanguageType(StaticType StaticType, int Pointer = 0, LanguageType? ElementType = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
}