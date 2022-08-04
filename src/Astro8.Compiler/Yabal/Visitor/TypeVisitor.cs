using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public class TypeVisitor : YabalParserBaseVisitor<LanguageType>
{
    public static readonly TypeVisitor Instance = new();

    public override LanguageType VisitIntType(YabalParser.IntTypeContext context)
    {
        return LanguageType.Integer;
    }

    public override LanguageType VisitBoolType(YabalParser.BoolTypeContext context)
    {
        return LanguageType.Boolean;
    }

    public override LanguageType VisitClassType(YabalParser.ClassTypeContext context)
    {
        throw new NotImplementedException();
    }

    public override LanguageType VisitArrayType(YabalParser.ArrayTypeContext context)
    {
        return new LanguageType(
            StaticType.Array,
            ElementType: Visit(context.type())
        );
    }

    public override LanguageType VisitVoidReturnType(YabalParser.VoidReturnTypeContext context)
    {
        return LanguageType.Void;
    }
}
