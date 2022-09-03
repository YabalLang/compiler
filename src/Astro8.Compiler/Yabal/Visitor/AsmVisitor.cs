using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public class AsmArgumentVisitor : YabalParserBaseVisitor<AsmArgument>
{
    public static readonly AsmArgumentVisitor Instance = new();

    public override AsmArgument VisitAsmInteger(YabalParser.AsmIntegerContext context)
    {
        return new AsmInteger(YabalVisitor.ParseInt(context.GetText()));
    }

    public override AsmArgument VisitAsmAddress(YabalParser.AsmAddressContext context)
    {
        return new AsmVariable(context.asmIdentifier().GetText());
    }

    public override AsmArgument VisitAsmLabelReference(YabalParser.AsmLabelReferenceContext context)
    {
        return new AsmLabel(context.asmIdentifier().GetText());
    }
}
