using Antlr4.Runtime;
using Yabal.Ast;
using Yabal.Exceptions;
using Zio;

namespace Yabal.Visitor;

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

public class ImportDiscover : YabalParserBaseListener
{
    private readonly FileReader _fileSystem;
    private readonly Uri _file;

    public ImportDiscover(FileReader fileSystem, Uri file)
    {
        _fileSystem = fileSystem;
        _file = file;
    }

    public Dictionary<Uri, YabalParser.ProgramContext> ImportByUrl { get; } = new();

    public Dictionary<YabalParser.ImportStatementContext, (Uri, YabalParser.ProgramContext)> ImportByContext { get; } = new();

    public override void EnterImportStatement(YabalParser.ImportStatementContext context)
    {
        var path = YabalVisitor.GetStringValue(context.@string());

        var (uri, code) = _fileSystem.ReadAllTextAsync(SourceRange.From(context, _file), path).GetAwaiter().GetResult();
        var inputStream = new AntlrInputStream(code);
        var lexer = new YabalLexer(inputStream);

        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new YabalParser(commonTokenStream)
        {
            ErrorHandler = new BailErrorStrategy(),
        };

        var result = parser.program();

        ImportByUrl[uri] = result;
        ImportByContext[context] = (uri, result);
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
