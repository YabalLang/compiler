namespace Astro8.Yabal.Ast;

public record LanguageStructField(string Name, LanguageType Type, int Offset);

public record LanguageStruct(string Name)
{
    public List<LanguageStructField> Fields { get; } = new();

    public int Size => Fields.Max(f => f.Offset + f.Type.Size);
}

public record LanguageType(StaticType StaticType, LanguageType? ElementType = null, LanguageStruct? StructReference = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
    public static readonly LanguageType Assembly = new(StaticType.Assembly);
    public static readonly LanguageType Unknown = new(StaticType.Unknown);

    public static LanguageType Array(LanguageType elementType) => new(StaticType.Array, elementType);

    public static LanguageType Address(LanguageType elementType) => new(StaticType.Address, elementType);

    public static LanguageType Struct(LanguageStruct structReference) => new(StaticType.Struct, null, structReference);

    public int Size => StaticType switch
    {
        StaticType.Integer => 1,
        StaticType.Boolean => 1,
        StaticType.Void => 0,
        StaticType.Assembly => 0,
        StaticType.Array => 1,
        StaticType.Address => ElementType?.Size ?? 0,
        StaticType.Struct => StructReference?.Size ?? 0,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override string ToString()
    {
        if (StaticType == StaticType.Array)
        {
            return $"{ElementType}[]";
        }

        if (StaticType == StaticType.Address)
        {
            return $"*{ElementType}";
        }

        if (StaticType == StaticType.Struct)
        {
            return StructReference?.Name ?? "";
        }

        return StaticType.ToString();
    }
}
