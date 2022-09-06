using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

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
