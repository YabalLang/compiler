using System.Globalization;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Yabal.Ast;
using Yabal.Exceptions;
using Zio;

namespace Yabal.Visitor;

public class YabalVisitor : YabalParserBaseVisitor<Node>
{
    private readonly TypeVisitor _typeVisitor;
    private readonly Dictionary<YabalParser.ImportStatementContext, (Uri, YabalParser.ProgramContext)> _importByContext = new();
    private Uri _file;
    private BlockCompileStack _block = new();

    public YabalVisitor(Uri file, YabalContext context)
    {
        _file = file;
        _typeVisitor = new TypeVisitor();
        Context = context;
    }

    public YabalContext Context { get; }

    public override ProgramStatement VisitProgram(YabalParser.ProgramContext context)
    {
        var typeDiscover = new TypeDiscover(_typeVisitor, this);
        var walker = new ParseTreeWalker();

        void Prepare(IParseTree current)
        {
            var importDiscover = new ImportDiscover(Context.FileReader, _file, Context);

            walker.Walk(importDiscover, current);

            foreach (var program in importDiscover.ImportByUrl.Values)
            {
                Prepare(program);
            }

            foreach (var (importContext, programContext) in importDiscover.ImportByContext)
            {
                _importByContext[importContext] = programContext;
            }

            walker.Walk(typeDiscover, current);
        }

        Prepare(context);

        return GetProgramStatement(context);
    }

    private ProgramStatement GetProgramStatement(YabalParser.ProgramContext context)
    {
        return new ProgramStatement(
            SourceRange.From(context, _file),
            context.statement().Select(VisitStatement).ToList()
        );
    }

    private Statement VisitStatement(ParserRuleContext context)
    {
        return Visit(context) switch
        {
            Statement statement => statement,
            Expression expression => new ExpressionStatement(SourceRange.From(context, _file), expression),
            _ => throw new InvalidOperationException($"{context.GetType().Name} is not supported.")
        };
    }

    private Expression VisitExpression(IParseTree context)
    {
        var result = (Expression)Visit(context);

        if (result == null)
        {
            throw new InvalidOperationException($"{context.GetType().Name} is not supported.");
        }

        return result;
    }

    private AssignableExpression VisitAssignExpression(ParserRuleContext context)
    {
        return VisitExpression(context) as AssignableExpression ??
               throw new InvalidCodeException($"Cannot assign to {context.GetType().Name}", SourceRange.From(context, _file));
    }

    private AddressExpression VisitAddressExpression(ParserRuleContext context)
    {
        return VisitExpression(context) as AddressExpression ??
               throw new InvalidCodeException($"Cannot assign to {context.GetType().Name}.", SourceRange.From(context, _file));
    }

    public override BlockStatement VisitBlockOrSingleStatement(YabalParser.BlockOrSingleStatementContext context)
    {
        if (context.statement() is { } statement)
        {
            return new BlockStatement(SourceRange.From(context, _file), new List<Statement>(1) { VisitStatement(statement) });
        }

        return VisitBlockStatement(context.blockStatement());
    }

    public override BlockStatement VisitBlockStatement(YabalParser.BlockStatementContext context)
    {
        var parent = _block;
        _block = new BlockCompileStack(parent);

        var statement = new BlockStatement(
            SourceRange.From(context, _file),
            context.statement().Select(VisitStatement).ToList()
        );

        _block = parent;

        return statement;
    }

    public override Node VisitDefaultVariableDeclaration(YabalParser.DefaultVariableDeclarationContext context)
    {
        var expression = context.expression() is { } expr ? VisitExpression(expr) : null;

        return new VariableDeclarationStatement(
            SourceRange.From(context, _file),
            GetIdentifier(context.identifierName()),
            context.Const() != null,
            expression,
            _typeVisitor.Visit(context.type())
        );
    }

    public Identifier GetIdentifier(YabalParser.IdentifierNameContext context)
    {
        return new Identifier(SourceRange.From(context, _file), context.GetText());
    }

    public override Node VisitAutoVariableDeclaration(YabalParser.AutoVariableDeclarationContext context)
    {
        var expression = VisitExpression(context.expression());

        return new VariableDeclarationStatement(
            SourceRange.From(context, _file),
            GetIdentifier(context.identifierName()),
            context.Const() != null,
            expression
        );
    }

    public override IntegerExpressionBase VisitIntegerExpression(YabalParser.IntegerExpressionContext context)
    {
        return new IntegerExpression(SourceRange.From(context, _file), ParseInt(context.GetText()));
    }

    public static int ParseInt(ReadOnlySpan<char> text)
    {
        if (text[0] == '0' && text.Length > 1)
        {
            switch (text[1])
            {
                case 'x':
                    return int.Parse(text[2..], NumberStyles.HexNumber);
                case 'b':
                    return Convert.ToInt32(text[2..].ToString(), 2);
            }
        }

        return int.Parse(text);
    }

    public override BooleanExpression VisitTrue(YabalParser.TrueContext context)
    {
        return new BooleanExpression(SourceRange.From(context, _file), true);
    }

    public override BooleanExpression VisitFalse(YabalParser.FalseContext context)
    {
        return new BooleanExpression(SourceRange.From(context, _file), false);
    }

    public override Node VisitAssignExpression(YabalParser.AssignExpressionContext context)
    {
        return new AssignExpression(
            SourceRange.From(context, _file),
            VisitAssignExpression(context.expression()[0]),
            VisitExpression(context.expression()[1])
        );
    }

    private BinaryExpression CreateBinary(
        ParserRuleContext context,
        YabalParser.ExpressionContext[] expressions,
        BinaryOperator @operator)
    {
        return new BinaryExpression(
            SourceRange.From(context, _file),
            @operator,
            VisitExpression(expressions[0]),
            VisitExpression(expressions[1])
        );
    }

    private Node CreateBinaryEqual(
        ParserRuleContext context,
        YabalParser.ExpressionContext[] expressions,
        BinaryOperator @operator)
    {
        return new AssignExpression(
            SourceRange.From(context, _file),
            VisitAssignExpression(expressions[0]),
            CreateBinary(context, expressions, @operator)
        );
    }

    public override Node VisitDivMulModBinaryExpression(YabalParser.DivMulModBinaryExpressionContext context)
    {
        var expressions = context.expression();
        BinaryOperator @operator;

        if (context.Div() != null)
            @operator = BinaryOperator.Divide;
        else if (context.Mul() != null)
            @operator = BinaryOperator.Multiply;
        else
            @operator = BinaryOperator.Modulo;

        return CreateBinary(context, expressions, @operator);
    }

    public override BinaryExpression VisitPlusSubBinaryExpression(YabalParser.PlusSubBinaryExpressionContext context)
    {
        var expressions = context.expression();
        var @operator = context.Add() != null ? BinaryOperator.Add : BinaryOperator.Subtract;

        return CreateBinary(context, expressions, @operator);
    }

    public override Node VisitPlusEqualExpression(YabalParser.PlusEqualExpressionContext context)
    {
        return CreateBinaryEqual(context, context.expression(), BinaryOperator.Add);
    }

    public override Node VisitSubEqualExpression(YabalParser.SubEqualExpressionContext context)
    {
        return CreateBinaryEqual(context, context.expression(), BinaryOperator.Subtract);
    }

    public override Node VisitMulEqualExpression(YabalParser.MulEqualExpressionContext context)
    {
        return CreateBinaryEqual(context, context.expression(), BinaryOperator.Multiply);
    }

    public override Node VisitDivEqualExpression(YabalParser.DivEqualExpressionContext context)
    {
        return CreateBinaryEqual(context, context.expression(), BinaryOperator.Divide);
    }

    public override Node VisitIdentifierExpression(YabalParser.IdentifierExpressionContext context)
    {
        return new IdentifierExpression(SourceRange.From(context, _file), GetIdentifier(context.identifierName()));
    }

    public override Node VisitNamespaceStatement(YabalParser.NamespaceStatementContext context)
    {
        if (context.blockStatement() is { } blockStatement)
        {
            return new NamespaceStatement(
                SourceRange.From(context, _file),
                new Namespace(context.identifierName().Select(i => i.GetText()).ToArray()),
                VisitBlockStatement(blockStatement));
        }

        return new GlobalNamespaceStatement(
            SourceRange.From(context, _file),
            new Namespace(context.identifierName().Select(i => i.GetText()).ToArray())
        );
    }

    public override Node VisitUseStatement(YabalParser.UseStatementContext context)
    {
        return new UsingStatement(
            SourceRange.From(context, _file),
            new Namespace(context.identifierName().Select(i => i.GetText()).ToArray())
        );
    }

    public override Node VisitOperatorFunctionDeclaration(YabalParser.OperatorFunctionDeclarationContext context)
    {
        var type = _typeVisitor.Visit(context.type());

        return new FunctionDeclarationStatement(
            SourceRange.From(context, _file),
            new FunctionCast(SourceRange.From(context, _file), type),
            type,
            GetArguments(context.functionParameterList()),
            VisitFunctionBody(context.functionBody()),
            context.Inline() != null
        );
    }

    public override Node VisitFunctionDeclaration(YabalParser.FunctionDeclarationContext context)
    {
        FunctionName name;

        if (context.identifierName() is { } identifierName)
        {
            name = new FunctionIdentifier(SourceRange.From(identifierName, _file), identifierName.GetText());
        }
        else if (context.operatorName() is { } operatorName)
        {
            var range = SourceRange.From(operatorName, _file);

            if (operatorName.Add() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Add);
            }
            else if (operatorName.Sub() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Subtract);
            }
            else if (operatorName.Mul() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Multiply);
            }
            else if (operatorName.Div() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Divide);
            }
            else if (operatorName.Mod() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Modulo);
            }
            else if (operatorName.Less() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.LessThan);
            }
            else if (operatorName.LessEqual() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.LessThanOrEqual);
            }
            else if (operatorName.Greater() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.GreaterThan);
            }
            else if (operatorName.GreaterEqual() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.GreaterThanOrEqual);
            }
            else if (operatorName.Equals() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Equal);
            }
            else if (operatorName.NotEquals() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.NotEqual);
            }
            else if (operatorName.And() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.And);
            }
            else if (operatorName.Or() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Or);
            }
            else if (operatorName.Xor() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.Xor);
            }
            else if (operatorName.ShiftLeft() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.LeftShift);
            }
            else if (operatorName.ShiftRight() is not null)
            {
                name = new FunctionOperator(range, BinaryOperator.RightShift);
            }
            else
            {
                name = new FunctionCast(
                    SourceRange.From(operatorName, _file),
                    _typeVisitor.Visit(context.returnType())
                );
            }
        }
        else
        {
            throw new NotSupportedException();
        }

        return new FunctionDeclarationStatement(
            SourceRange.From(context, _file),
            name,
            _typeVisitor.Visit(context.returnType()),
            GetArguments(context.functionParameterList()),
            VisitFunctionBody(context.functionBody()),
            context.Inline() != null
        );
    }

    private List<FunctionParameter> GetArguments(YabalParser.FunctionParameterListContext context)
    {
        return context.functionParameter()
            .Select(p =>
            {
                var type = _typeVisitor.Visit(p.type());

                if (p.Ref() != null)
                {
                    type = LanguageType.Reference(type);
                }

                return new FunctionParameter(
                    GetIdentifier(p.identifierName()),
                    type,
                    p.expression() != null
                );
            })
            .ToList();
    }

    public override BlockStatement VisitFunctionBody(YabalParser.FunctionBodyContext context)
    {
        if (context.expression() is { } expression)
        {
            return new BlockStatement(
                SourceRange.From(context, _file),
                new List<Statement>
                {
                    new ReturnStatement(
                        SourceRange.From(context, _file),
                        VisitExpression(expression)
                    )
                }
            );
        }

        return VisitBlockStatement(context.blockStatement());
    }

    public override Node VisitCallExpression(YabalParser.CallExpressionContext context)
    {
        return new CallExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression()),
            context.expressionList().expression().Select(VisitExpression).ToList()
        );
    }

    public override Node VisitExpressionStatement(YabalParser.ExpressionStatementContext context)
    {
        return new ExpressionStatement(
            SourceRange.From(context, _file),
            VisitExpression(context.expression())
        );
    }

    public override Node VisitAsmExpression(YabalParser.AsmExpressionContext context)
    {
        var instructions = new List<AsmStatement>();
        var items = context.asmItems().asmStatementItem();
        var visitor = new AsmArgumentVisitor(_file);

        if (items != null)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case YabalParser.AsmInstructionContext instruction:
                        instructions.Add(new AsmInstruction(
                            SourceRange.From(item, _file),
                            instruction.asmIdentifier().GetText(),
                            instruction.asmArgument() is {} arg ? visitor.Visit(arg) : null
                        ));
                        break;
                    case YabalParser.AsmLabelContext label:
                        instructions.Add(new AsmDefineLabel(
                            SourceRange.From(item, _file),
                            label.asmIdentifier().GetText()
                        ));
                        break;
                    case YabalParser.AsmCommentContext label:
                        instructions.Add(new AsmComment(
                            SourceRange.From(item, _file),
                            label.AsmComment().GetText()
                        ));
                        break;
                    case YabalParser.AsmRawValueContext rawValue:
                        instructions.Add(new AsmRawValue(
                            SourceRange.From(item, _file),
                            visitor.Visit(rawValue.asmArgument())
                        ));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        return new AsmExpression(SourceRange.From(context, _file), instructions);
    }

    public override Node VisitArrayAccessExpression(YabalParser.ArrayAccessExpressionContext context)
    {
        return new ArrayAccessExpression(
            SourceRange.From(context, _file),
            VisitAddressExpression(context.expression()[0]),
            VisitExpression(context.expression()[1])
        );
    }

    public override Node VisitReturnStatement(YabalParser.ReturnStatementContext context)
    {
        return new ReturnStatement(
            SourceRange.From(context, _file),
            context.expression() is {} expression ? VisitExpression(expression) : null
        );
    }


    public override Node VisitWhileStatement(YabalParser.WhileStatementContext context)
    {
        return new WhileStatement(
            SourceRange.From(context, _file),
            VisitExpression(context.expression()),
            VisitBlockOrSingleStatement(context.blockOrSingleStatement())
        );
    }

    public override Node VisitImportStatement(YabalParser.ImportStatementContext context)
    {
        var (uri, programContext) = _importByContext[context];
        var file = _file;
        _file = uri;
        var program = GetProgramStatement(programContext);
        _file = file;

        return new ImportStatement(
            SourceRange.From(context, _file),
            program
        );
    }

    public override Node VisitNotEqualExpression(YabalParser.NotEqualExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.NotEqual);
    }

    public override Node VisitComparisonExpression(YabalParser.ComparisonExpressionContext context)
    {
        BinaryOperator @operator;

        if (context.Greater() != null) @operator = BinaryOperator.GreaterThan;
        else if (context.GreaterEqual() != null) @operator = BinaryOperator.GreaterThanOrEqual;
        else if (context.Less() != null) @operator = BinaryOperator.LessThan;
        else if (context.LessEqual() != null) @operator = BinaryOperator.LessThanOrEqual;
        else throw new NotSupportedException();

        return CreateBinary(context, context.expression(), @operator);
    }

    public override Node VisitEqualExpression(YabalParser.EqualExpressionContext context)
    {
        var @operator = context.Equals() != null ? BinaryOperator.Equal : BinaryOperator.NotEqual;
        return CreateBinary(context, context.expression(), @operator);
    }

    public override Node VisitIfStatement(YabalParser.IfStatementContext context)
    {
        Expression test;
        Statement? elseStatement = null;

        if (context.elseStatement() != null)
        {
            elseStatement = VisitStatement(context.elseStatement().blockOrSingleStatement());
        }

        if (context.elseIfStatement() != null)
        {
            foreach (var elseIf in context.elseIfStatement())
            {
                test = VisitExpression(elseIf.expression());

                elseStatement = new IfStatement(
                    SourceRange.From(elseIf, _file),
                    test,
                    (BlockStatement) Visit(elseIf.blockOrSingleStatement()),
                    elseStatement);
            }
        }

        test = VisitExpression(context.expression());

        return new IfStatement(
            SourceRange.From(context, _file),
            test,
            (BlockStatement) Visit(context.blockOrSingleStatement()),
            elseStatement);
    }

    public override Node VisitCharExpression(YabalParser.CharExpressionContext context)
    {
        var value = context.@char().charValue().GetText();

        if (value.Length == 2 && value[0] == '\\')
        {
            value = value switch
            {
                "\\n" => "\n",
                "\\r" => "\r",
                "\\t" => "\t",
                "\\\"" => "\"",
                "\\'" => "'",
                "\\\\" => "\\",
                _ => throw new InvalidCodeException($"Unknown escape character '{value}'", SourceRange.From(context, _file))
            };
        }

        return new CharExpression(
            SourceRange.From(context, _file),
            value[0]
        );
    }

    public override Node VisitForStatement(YabalParser.ForStatementContext context)
    {
        return new ForStatement(
            SourceRange.From(context, _file),
            context.forInit()?.statement() is { } init ? VisitStatement(init) : null,
            context.statement() is {} statement ? VisitStatement(statement) : null,
            VisitExpression(context.expression()),
            VisitBlockOrSingleStatement(context.blockOrSingleStatement())
        );
    }

    public override Node VisitIncrementLeftExpression(YabalParser.IncrementLeftExpressionContext context)
    {
        return new UpdateExpression(
            SourceRange.From(context, _file),
            VisitAssignExpression(context.expression()),
            true,
            BinaryOperator.Add
        );
    }

    public override Node VisitIncrementRightExpression(YabalParser.IncrementRightExpressionContext context)
    {
        return new UpdateExpression(
            SourceRange.From(context, _file),
            VisitAssignExpression(context.expression()),
            false,
            BinaryOperator.Add
        );
    }

    public override Node VisitDecrementLeftExpression(YabalParser.DecrementLeftExpressionContext context)
    {
        return new UpdateExpression(
            SourceRange.From(context, _file),
            VisitAssignExpression(context.expression()),
            true,
            BinaryOperator.Subtract
        );
    }

    public override Node VisitSwitchExpression(YabalParser.SwitchExpressionContext context)
    {
        var expression = VisitExpression(context.expression());
        var items = new List<SwitchItem>();
        Expression? defaultValue = null;

        if (context.inlineSwitch().inlineSwitchItem() is { } switchItems)
        {
            foreach (var switchItem in switchItems)
            {
                if (switchItem.underscore() != null)
                {
                    if (defaultValue != null)
                    {
                        throw new InvalidCodeException("Multiple default cases are not supported", SourceRange.From(switchItem, _file));
                    }

                    defaultValue = VisitExpression(switchItem.expression());
                }
                else
                {
                    items.Add(new SwitchItem(
                        switchItem.expressionList().expression().Select(VisitExpression).ToList(),
                        VisitExpression(switchItem.expression())
                    ));
                }
            }
        }

        return new SwitchExpression(
            SourceRange.From(context, _file),
            expression,
            items,
            defaultValue
        );
    }

    public override Node VisitDecrementRightExpression(YabalParser.DecrementRightExpressionContext context)
    {
        return new UpdateExpression(
            SourceRange.From(context, _file),
            VisitAssignExpression(context.expression()),
            false,
            BinaryOperator.Subtract
        );
    }

    public override Node VisitTernaryExpression(YabalParser.TernaryExpressionContext context)
    {
        return new TernaryExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression()[0]),
            VisitExpression(context.expression()[1]),
            VisitExpression(context.expression()[2])
        );
    }

    public override Node VisitAndAlsoExpression(YabalParser.AndAlsoExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.AndAlso);
    }

    public override Node VisitOrElseExpression(YabalParser.OrElseExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.OrElse);
    }

    public override Node VisitShiftExpression(YabalParser.ShiftExpressionContext context)
    {
        var @operator = context.ShiftLeft() != null ? BinaryOperator.LeftShift : BinaryOperator.RightShift;
        return CreateBinary(context, context.expression(), @operator);
    }

    public override Node VisitAndExpression(YabalParser.AndExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.And);
    }

    public override Node VisitOrExpression(YabalParser.OrExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.Or);
    }

    public override Node VisitXorExpression(YabalParser.XorExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.Xor);
    }

    public override Node VisitExpressionExpression(YabalParser.ExpressionExpressionContext context)
    {
        return VisitExpression(context.expression());
    }

    public override Node VisitArrowFunctionExpression(YabalParser.ArrowFunctionExpressionContext context)
    {
        BlockStatement block;

        if (context.arrowFunction().expression() is { } expressionContext)
        {
            var expression = VisitExpression(expressionContext);

            block = new BlockStatement(expression.Range, new List<Statement>
            {
                new ReturnStatement(expression.Range, expression)
            });
        }
        else
        {
            block = VisitBlockStatement(context.arrowFunction().blockStatement());
        }

        return new ArrowFunctionExpression(
            SourceRange.From(context, _file),
            context.arrowFunction().identifierName().Select(GetIdentifier).ToList(),
            block
        );
    }

    public override Node VisitNegateExpression(YabalParser.NegateExpressionContext context)
    {
        return new UnaryExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression()),
            UnaryOperator.Negate
        );
    }

    public override Node VisitNotExpression(YabalParser.NotExpressionContext context)
    {
        return new UnaryExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression()),
            UnaryOperator.Not
        );
    }

    public override Node VisitMinusExpression(YabalParser.MinusExpressionContext context)
    {
        return new UnaryExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression()),
            UnaryOperator.Minus
        );
    }

    public override Node VisitCreatePointerExpression(YabalParser.CreatePointerExpressionContext context)
    {
        var createPointer = context.createPointer();

        return new CreatePointerExpression(
            SourceRange.From(context, _file),
            VisitExpression(createPointer.expression()),
            createPointer.integer() is {} integer ? ParseInt(integer.GetText()) : 0,
            createPointer.type() is {} type ? _typeVisitor.VisitType(type) : LanguageType.Integer
        );
    }

    public override Node VisitSizeOfExpression(YabalParser.SizeOfExpressionContext context)
    {
        return new SizeOfExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression())
        );
    }

    public override Node VisitContinueStatement(YabalParser.ContinueStatementContext context)
    {
        return new ContinueStatement(SourceRange.From(context, _file));
    }

    public override Node VisitBreakStatement(YabalParser.BreakStatementContext context)
    {
        return new BreakStatement(SourceRange.From(context, _file));
    }

    public override Node VisitIncludeBytesExpression(YabalParser.IncludeBytesExpressionContext context)
    {
        return IncludeFile(SourceRange.From(context, _file), context.expression(), FileType.Byte);
    }

    public override Node VisitIncludeFontExpression(YabalParser.IncludeFontExpressionContext context)
    {
        return IncludeFile(SourceRange.From(context, _file), context.expression(), FileType.Font);
    }

    public override Node VisitIncludeImageExpression(YabalParser.IncludeImageExpressionContext context)
    {
        return IncludeFile(SourceRange.From(context, _file), context.expression(), FileType.Image);
    }

    private Node IncludeFile(SourceRange context, YabalParser.ExpressionContext expression, FileType type)
    {
        var value = VisitExpression(expression);

        if (value is not StringExpression stringExpression)
        {
            throw new InvalidCodeException("Argument must be a literal string", SourceRange.From(expression, _file));
        }

        var path = stringExpression.Value;

        return new IncludeFileExpression(
            context,
            path,
            type
        );
    }

    public override Node VisitCastExpression(YabalParser.CastExpressionContext context)
    {
        return new CastExpression(
            SourceRange.From(context, _file),
            _typeVisitor.Visit(context.type()),
            VisitExpression(context.expression())
        );
    }

    public override Node VisitStringExpression(YabalParser.StringExpressionContext context)
    {
        var value = GetStringValue(context.@string());

        return new StringExpression(SourceRange.From(context, _file), value);
    }

    internal static string GetStringValue(YabalParser.StringContext context)
    {
        var sb = new StringBuilder();
        var parts = context.stringPart();

        foreach (var part in parts)
        {
            if (part.StringEscape() is { } escape)
            {
                var c = escape.GetText()[1];

                sb.Append(c switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => c
                });
            }
            else
            {
                sb.Append(part.GetText());
            }
        }

        return parts[0].GetText();
    }

    public override Node VisitLabelStatement(YabalParser.LabelStatementContext context)
    {
        return new LabelStatement(SourceRange.From(context, _file), context.identifierName().GetText());
    }

    public override Node VisitGotoStatement(YabalParser.GotoStatementContext context)
    {
        return new GotoStatement(SourceRange.From(context, _file), context.identifierName().GetText());
    }

    public override Node VisitStructDeclaration(YabalParser.StructDeclarationContext context)
    {
        var reference = _typeVisitor.Structs[context.identifierName().GetText()].Value;

        return new StructDeclarationStatement(SourceRange.From(context, _file), reference);
    }

    public override Node VisitMemberExpression(YabalParser.MemberExpressionContext context)
    {
        return new MemberExpression(
            SourceRange.From(context, _file),
            VisitAddressExpression(context.expression()),
            GetIdentifier(context.identifierName())
        );
    }

    public override Node VisitInitStructExpression(YabalParser.InitStructExpressionContext context)
    {
        var init = context.initStruct();

        return new InitStructExpression(
            SourceRange.From(context, _file),
            init.initStructItem()
                .Select(i => new InitStructItem(
                    i.identifierName() is {} identifierName ? GetIdentifier(identifierName) : null,
                    VisitExpression(i.expression())))
                .ToList(),
            init.type() is {} type ? _typeVisitor.VisitType(type) : null
        );
    }

    public override Node VisitRefExpression(YabalParser.RefExpressionContext context)
    {
        return new ReferenceExpression(
            SourceRange.From(context, _file),
            VisitExpression(context.expression())
        );
    }

    public override Node VisitStackAllocExpression(YabalParser.StackAllocExpressionContext context)
    {
        return new StackAllocationExpression(
            SourceRange.From(context, _file),
            _typeVisitor.VisitType(context.type()),
            VisitExpression(context.expression())
        );
    }
}
