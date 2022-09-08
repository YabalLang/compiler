using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public class TypeDiscover : YabalParserBaseListener
{
    private readonly TypeVisitor _typeVisitor;

    public TypeDiscover(TypeVisitor typeVisitor)
    {
        _typeVisitor = typeVisitor;
    }

    public override void EnterStructType(YabalParser.StructTypeContext context)
    {
        var reference = new LanguageStruct(
            context.identifierName().GetText()
        );

        _typeVisitor.Structs[reference.Name] = reference;
    }
}

public class TypeVisitor : YabalParserBaseVisitor<LanguageType>
{
    public Dictionary<string, LanguageStruct> Structs { get; } = new();

    public override LanguageType VisitIntType(YabalParser.IntTypeContext context)
    {
        return LanguageType.Integer;
    }

    public override LanguageType VisitBoolType(YabalParser.BoolTypeContext context)
    {
        return LanguageType.Boolean;
    }

    public override LanguageType VisitStructType(YabalParser.StructTypeContext context)
    {
        return LanguageType.Struct(
            Structs[context.identifierName().GetText()]
        );
    }

    public override LanguageType VisitArrayType(YabalParser.ArrayTypeContext context)
    {
        return new LanguageType(
            StaticType.Pointer,
            ElementType: Visit(context.type())
        );
    }

    public override LanguageType VisitVoidReturnType(YabalParser.VoidReturnTypeContext context)
    {
        return LanguageType.Void;
    }
}
