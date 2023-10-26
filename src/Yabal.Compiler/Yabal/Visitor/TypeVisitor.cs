using Antlr4.Runtime;
using Yabal.Ast;
using Yabal.Exceptions;
using Zio;

namespace Yabal.Visitor;

public class TypeDiscover : YabalParserBaseListener
{
    private readonly TypeVisitor _typeVisitor;
    private readonly YabalVisitor _visitor;

    public TypeDiscover(TypeVisitor typeVisitor, YabalVisitor visitor)
    {
        _typeVisitor = typeVisitor;
        _visitor = visitor;
    }

    public override void EnterStructDeclaration(YabalParser.StructDeclarationContext context)
    {
        var reference = new LanguageStruct(
            _visitor.GetIdentifier(context.identifierName())
        );

        _typeVisitor.Structs[reference.Name] = (context, reference);
    }
}

public class ImportDiscover : YabalParserBaseListener
{
    private readonly FileReader _fileSystem;
    private readonly Uri _file;
    private readonly YabalContext _context;

    public ImportDiscover(FileReader fileSystem, Uri file, YabalContext context)
    {
        _fileSystem = fileSystem;
        _file = file;
        _context = context;
    }

    public Dictionary<Uri, YabalParser.ProgramContext> ImportByUrl { get; } = new();

    public Dictionary<YabalParser.ImportStatementContext, (Uri, YabalParser.ProgramContext)> ImportByContext { get; } = new();

    public override void EnterImportStatement(YabalParser.ImportStatementContext context)
    {
        var path = YabalVisitor.GetStringValue(context.@string());
        var uri = _fileSystem.GetUri(SourceRange.From(context, _file), path);

        if (_context.LoadedFiles.Contains(uri))
        {
            return;
        }

        _context.LoadedFiles.Add(uri);

        var (_, code) = _fileSystem.ReadAllTextAsync(SourceRange.From(context, _file), path).GetAwaiter().GetResult();
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
    public Dictionary<string, (YabalParser.StructDeclarationContext Context, LanguageStruct Value)> Structs { get; } = new();

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
        var name = context.identifierName().GetText();

        return name switch
        {
            "char" => LanguageType.Char,
            _ => LanguageType.Struct(GetStruct(context.identifierName()))
        };
    }

    public override LanguageType VisitArrayType(YabalParser.ArrayTypeContext context)
    {
        return new LanguageType(
            StaticType.Pointer,
            ElementType: Visit(context.type())
        );
    }

    public override LanguageType VisitFunctionType(YabalParser.FunctionTypeContext context)
    {
        return new LanguageType(
            StaticType.Function,
            FunctionType: new LanguageFunction(
                VisitReturnType(context.returnType()),
                context.typeList().type().Select(VisitType).ToList()
            )
        );
    }

    public override LanguageType VisitVoidReturnType(YabalParser.VoidReturnTypeContext context)
    {
        return LanguageType.Void;
    }

    public LanguageStruct GetStruct(YabalParser.IdentifierNameContext identifier)
    {
        var name = identifier.GetText();

        if (!Structs.TryGetValue(name, out var value))
        {
            throw new InvalidCodeException($"Unknown struct '{name}'", SourceRange.From(identifier, new Uri("file://unknown"))); // TODO: Fix this
        }

        var (context, reference) = value;

        if (reference.State == LanguageStructState.Initialized)
        {
            return reference;
        }

        var file = reference.Identifier.Range.File;

        if (reference.State == LanguageStructState.Visiting)
        {
            throw new InvalidCodeException("Recursive struct definition", SourceRange.From(context, file));
        }

        reference.State = LanguageStructState.Visiting;

        var offset = 0;
        var bitOffset = 0;

        foreach (var item in context.structItem())
        {
            if (item.structField() is { } field)
            {
                var type = VisitType(field.type());
                var bitSize = field.integer() is { } integer ? (int?)YabalVisitor.ParseInt(integer.GetText()) : null;

                reference.Fields.Add(new LanguageStructField(
                    field.identifierName().GetText(),
                    type,
                    offset,
                    bitSize.HasValue ? new Bit(bitOffset, bitSize.Value) : null
                ));

                if (bitSize.HasValue)
                {
                    if (type.StaticType != StaticType.Integer)
                    {
                        throw new InvalidCodeException("Bitfields can only be applied to integers", SourceRange.From(field, file));
                    }

                    bitOffset += bitSize.Value;

                    if (bitOffset > 16)
                    {
                        throw new InvalidCodeException("Bitfields cannot span more than 16 bits", SourceRange.From(field, file));
                    }

                    if (bitOffset == 16)
                    {
                        offset++;
                        bitOffset = 0;
                    }
                }
                else
                {
                    if (bitOffset > 0)
                    {
                        bitOffset = 0;
                        offset++;
                    }

                    offset += type.Size;
                }
            }
        }

        reference.State = LanguageStructState.Initialized;

        return reference;
    }
}
