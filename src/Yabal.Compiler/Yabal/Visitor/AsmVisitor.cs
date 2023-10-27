using Yabal.Ast;

namespace Yabal.Visitor;

public class AsmArgumentVisitor : YabalParserBaseVisitor<AsmArgument>
{
    private Uri _file;

    public AsmArgumentVisitor(Uri file)
    {
        _file = file;
    }

    public override AsmArgument VisitAsmInteger(YabalParser.AsmIntegerContext context)
    {
        return new AsmInteger(SourceRange.From(context, _file), YabalVisitor.ParseInt(context.GetText()));
    }

    public override AsmArgument VisitAsmAddress(YabalParser.AsmAddressContext context)
    {
        return new AsmVariable(SourceRange.From(context, _file), new Identifier(SourceRange.From(context, _file), context.asmIdentifier().GetText()));
    }

    public override AsmArgument VisitAsmLabelReference(YabalParser.AsmLabelReferenceContext context)
    {
        return new AsmLabel(SourceRange.From(context, _file), context.asmIdentifier().GetText());
    }
}
