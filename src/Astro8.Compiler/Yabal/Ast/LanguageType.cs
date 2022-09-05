namespace Astro8.Yabal.Ast;

public record LanguageType(StaticType StaticType, LanguageType? ElementType = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
    public static readonly LanguageType Assembly = new(StaticType.Assembly);

    public static LanguageType Pointer(LanguageType elementType) => new(StaticType.Pointer, elementType);

    public override int GetHashCode()
    {
        return HashCode.Combine(StaticType, ElementType);
    }

    public virtual bool Equals(LanguageType? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (other.StaticType == StaticType.Assembly || StaticType == StaticType.Assembly)
            return true;
        return StaticType == other.StaticType && ElementType == other.ElementType;
    }

    public override string ToString()
    {
        if (StaticType == StaticType.Pointer)
        {
            return $"{ElementType}[]";
        }

        return StaticType.ToString();
    }
}
