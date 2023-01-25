namespace Astro8.Yabal.Ast;

public record struct Bit(int Offset, int Size);

public record LanguageStructField(string Name, LanguageType Type, int Offset, Bit? Bit);

public record LanguageStruct(string Name)
{
    public List<LanguageStructField> Fields { get; } = new();

    public int Size => Fields
        .Select(f => f.Offset + f.Type.Size)
        .DefaultIfEmpty()
        .Max();
}

public record LanguageType(StaticType StaticType, LanguageType? ElementType = null, LanguageStruct? StructReference = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
    public static readonly LanguageType Assembly = new(StaticType.Assembly);
    public static readonly LanguageType Unknown = new(StaticType.Unknown);

    public static LanguageType Pointer(LanguageType elementType) => new(StaticType.Pointer, elementType);

    public static LanguageType Struct(LanguageStruct structReference) => new(StaticType.Struct, null, structReference);

    public int Size => StaticType switch
    {
        StaticType.Integer => 1,
        StaticType.Boolean => 1,
        StaticType.Void => 0,
        StaticType.Assembly => 1,
        StaticType.Pointer => 2,
        StaticType.Struct => StructReference?.Size ?? 0,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override string ToString()
    {
        if (StaticType == StaticType.Pointer)
        {
            return $"*{ElementType}";
        }

        if (StaticType == StaticType.Struct)
        {
            return StructReference?.Name ?? "";
        }

        return StaticType switch
        {
            StaticType.Integer => "int",
            StaticType.Boolean => "bool",
            StaticType.Void => "void",
            StaticType.Pointer => "ptr",
            StaticType.Struct => "struct",
            StaticType.Assembly => "assembly",
            StaticType.Unknown => "unknown",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
