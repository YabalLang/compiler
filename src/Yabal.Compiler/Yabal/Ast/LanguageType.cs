using Yabal.Exceptions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record struct Bit(int Offset, int Size);

public record LanguageStructField(string Name, LanguageType Type, int Offset, Bit? Bit);

internal enum LanguageStructState
{
    Declared,
    Visiting,
    Initialized
}

public record LanguageStruct(Identifier Identifier)
{
    internal LanguageStructState State { get; set; }

    public string Name => Identifier.Name;

    public List<LanguageStructField> Fields { get; } = new();

    public int Size
    {
        get
        {
            return Fields
                .Select(f => f.Offset + f.Type.Size)
                .DefaultIfEmpty()
                .Max();
        }
    }
}

public record LanguageFunction(
    LanguageType ReturnType,
    List<LanguageType> Parameters)
{
    public virtual bool Equals(LanguageFunction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ReturnType.Equals(other.ReturnType) && Parameters.SequenceEqual(other.Parameters);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReturnType, Parameters.Aggregate(0, HashCode.Combine));
    }
}

public record LanguageType(
    StaticType StaticType,
    LanguageType? ElementType = null,
    LanguageStruct? StructReference = null,
    bool IsReference = false,
    LanguageFunction? FunctionType = null)
{
    public static readonly LanguageType Integer = new(StaticType.Integer);
    public static readonly LanguageType Boolean = new(StaticType.Boolean);
    public static readonly LanguageType Void = new(StaticType.Void);
    public static readonly LanguageType Assembly = new(StaticType.Assembly);
    public static readonly LanguageType Unknown = new(StaticType.Unknown);
    public static readonly LanguageType Char = new(StaticType.Char);

    public static LanguageType Pointer(LanguageType elementType) => new(StaticType.Pointer, elementType);

    public static LanguageType Struct(LanguageStruct structReference) => new(StaticType.Struct, null, structReference);

    public static LanguageType Reference(LanguageType elementType)
    {
        return elementType.IsReference
            ? elementType
            : new LanguageType(StaticType.Reference, elementType, elementType.StructReference, IsReference: true);
    }

    public static LanguageType RefPointer(LanguageType elementType) => new(StaticType.Pointer, elementType, IsReference: true);

    public int Size => StaticType switch
    {
        StaticType.Integer => 1,
        StaticType.Boolean => 1,
        StaticType.Void => 0,
        StaticType.Assembly => 1,
        StaticType.Pointer => 2,
        StaticType.Reference => 1,
        StaticType.Char => 1,
        StaticType.Struct => StructReference?.Size ?? 0,
        StaticType.Function => 1,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override string ToString()
    {
        return StaticType switch
        {
            StaticType.Integer => "int",
            StaticType.Boolean => "bool",
            StaticType.Void => "void",
            StaticType.Pointer => $"{ElementType}[]",
            StaticType.Struct => StructReference?.Name ?? "struct",
            StaticType.Assembly => "assembly",
            StaticType.Reference => $"ref {ElementType}",
            StaticType.Unknown => "unknown",
            StaticType.Char => "char",
            StaticType.Function => GetFunctionName(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string GetFunctionName()
    {
        return FunctionType?.Parameters is null or { Count: 0 }
            ? $"func<{FunctionType?.ReturnType ?? Void}>"
            : $"func<{string.Join(", ", FunctionType.Parameters)}, {FunctionType?.ReturnType ?? Void}>";
    }

    public virtual bool Equals(LanguageType? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return StaticType == other.StaticType && Equals(ElementType, other.ElementType) && Equals(StructReference, other.StructReference) && Equals(FunctionType, other.FunctionType);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)StaticType, ElementType, StructReference, IsReference);
    }
}
