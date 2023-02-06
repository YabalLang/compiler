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
    private readonly IFileSystem _fileSystem;
    private readonly Uri _file;

    public ImportDiscover(IFileSystem fileSystem, Uri file)
    {
        _fileSystem = fileSystem;
        _file = file;
    }

    public Dictionary<Uri, YabalParser.ProgramContext> ImportByUrl { get; } = new();

    public Dictionary<YabalParser.ImportStatementContext, (Uri, YabalParser.ProgramContext)> ImportByContext { get; } = new();

    public override void EnterImportStatement(YabalParser.ImportStatementContext context)
    {
        var path = YabalVisitor.GetStringValue(context.@string());

        Uri? uri;

        if (path.StartsWith("."))
        {
            uri = new Uri(_file, path);
        }
        else if (path.StartsWith("/"))
        {
            uri = new Uri(_file, "." + path);
        }
        else if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
        {
            uri = new Uri("https://yabal.dev/x/" + path);
        }

        if (uri == null)
        {
            throw new InvalidOperationException();
        }

        var code = GetFromUri(context, uri);
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

    private string? GetFromUri(YabalParser.ImportStatementContext context, Uri uri)
    {
        return uri.Scheme switch
        {
            "file" => GetFromFile(context, uri),
            "http" or "https" => GetFromHttp(context, uri),
            _ => throw new InvalidCodeException("Invalid import scheme '" + uri.Scheme + "'", SourceRange.From(context, _file))
        };
    }

    private string? GetFromFile(ParserRuleContext context, Uri uri)
    {
        try
        {
            return _fileSystem.ReadAllText(uri.LocalPath);
        }
        catch (Exception)
        {
            throw new InvalidCodeException("Could not find or read file '" + _fileSystem.ConvertPathToInternal(uri.LocalPath) + "'", SourceRange.From(context, _file));
        }
    }

    private string? GetFromHttp(ParserRuleContext context, Uri uri)
    {
        try
        {
            using var client = new HttpClient();

            var response = client.GetAsync(uri).Result;
            response.EnsureSuccessStatusCode();

            return response.Content.ReadAsStringAsync().Result;
        }
        catch (Exception)
        {
            throw new InvalidCodeException("Failed to import '" + uri + "'", SourceRange.From(context, _file));
        }
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
