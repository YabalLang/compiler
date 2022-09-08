using System.Globalization;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public class YabalVisitor : YabalParserBaseVisitor<Node>
{
    private readonly TypeVisitor _typeVisitor = new();
    private BlockCompileStack _block = new();

    public List<(string, FileType)> Files { get; } = new();

    public override ProgramStatement VisitProgram(YabalParser.ProgramContext context)
    {
        var discover = new TypeDiscover(_typeVisitor);
        var walker = new ParseTreeWalker();
        walker.Walk(discover, context);

        return new ProgramStatement(
            context,
            context.statement().Select(VisitStatement).ToList()
        );
    }

    private Statement VisitStatement(ParserRuleContext context)
    {
        return Visit(context) switch
        {
            Statement statement => statement,
            Expression expression => new ExpressionStatement(context, expression),
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

    private AssignableExpression VisitAssignExpression(IParseTree context)
    {
        return VisitExpression(context) as AssignableExpression ??
               throw new InvalidOperationException($"Cannot assign to {context.GetType().Name}.");
    }

    private AddressExpression VisitAddressExpression(IParseTree context)
    {
        return VisitExpression(context) as AddressExpression ??
               throw new InvalidOperationException($"Cannot assign to {context.GetType().Name}.");
    }

    public override BlockStatement VisitBlockStatement(YabalParser.BlockStatementContext context)
    {
        var parent = _block;
        _block = new BlockCompileStack(parent);

        var statement = new BlockStatement(
            context,
            context.statement().Select(VisitStatement).ToList()
        );

        _block = parent;

        return statement;
    }

    public override Node VisitDefaultVariableDeclaration(YabalParser.DefaultVariableDeclarationContext context)
    {
        var name = context.identifierName().GetText();
        var expression = context.expression() is { } expr ? VisitExpression(expr) : null;

        return new VariableDeclarationStatement(
            context,
            name,
            context.Const() != null,
            expression,
            _typeVisitor.Visit(context.type())
        );
    }

    public override Node VisitAutoVariableDeclaration(YabalParser.AutoVariableDeclarationContext context)
    {
        var name = context.identifierName().GetText();
        var expression = VisitExpression(context.expression());

        return new VariableDeclarationStatement(
            context,
            name,
            context.Const() != null,
            expression
        );
    }

    public override IntegerExpressionBase VisitIntegerExpression(YabalParser.IntegerExpressionContext context)
    {
        return new IntegerExpression(context, ParseInt(context.GetText()));
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
        return new BooleanExpression(context, true);
    }

    public override BooleanExpression VisitFalse(YabalParser.FalseContext context)
    {
        return new BooleanExpression(context, false);
    }

    public override Node VisitAssignExpression(YabalParser.AssignExpressionContext context)
    {
        return new AssignExpression(
            context,
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
            context,
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
            context,
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
        return new IdentifierExpression(context, context.identifierName().GetText());
    }

    public override Node VisitFunctionDeclaration(YabalParser.FunctionDeclarationContext context)
    {
        return new FunctionDeclarationStatement(
            context,
            context.identifierName().GetText(),
            _typeVisitor.Visit(context.returnType()),
            context.functionParameterList().functionParameter()
                .Select(p => new FunctionParameter(
                    p.identifierName().GetText(),
                    _typeVisitor.Visit(p.type())
                ))
                .ToList(),
            VisitFunctionBody(context.functionBody()),
            context.Inline() != null
        );
    }

    public override BlockStatement VisitFunctionBody(YabalParser.FunctionBodyContext context)
    {
        if (context.expression() is { } expression)
        {
            return new BlockStatement(
                context,
                new List<Statement>
                {
                    new ReturnStatement(
                        context,
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
            context,
            VisitExpression(context.expression()),
            context.expressionList().expression().Select(VisitExpression).ToList()
        );
    }

    public override Node VisitExpressionStatement(YabalParser.ExpressionStatementContext context)
    {
        return new ExpressionStatement(
            context,
            VisitExpression(context.expression())
        );
    }

    public override Node VisitAsmExpression(YabalParser.AsmExpressionContext context)
    {
        var instructions = new List<AsmStatement>();
        var items = context.asmItems().asmStatementItem();

        if (items != null)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case YabalParser.AsmInstructionContext instruction:
                        instructions.Add(new AsmInstruction(
                            item,
                            instruction.asmIdentifier().GetText(),
                            instruction.asmArgument() is {} arg ? AsmArgumentVisitor.Instance.Visit(arg) : null
                        ));
                        break;
                    case YabalParser.AsmLabelContext label:
                        instructions.Add(new AsmDefineLabel(
                            item,
                            label.asmIdentifier().GetText()
                        ));
                        break;
                    case YabalParser.AsmRawValueContext rawValue:
                        instructions.Add(new AsmRawValue(
                            item,
                            AsmArgumentVisitor.Instance.Visit(rawValue.asmArgument())
                        ));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        return new AsmExpression(context, instructions);
    }

    public override Node VisitArrayAccessExpression(YabalParser.ArrayAccessExpressionContext context)
    {
        return new ArrayAccessExpression(
            context,
            VisitAddressExpression(context.expression()[0]),
            VisitExpression(context.expression()[1])
        );
    }

    public override Node VisitReturnStatement(YabalParser.ReturnStatementContext context)
    {
        return new ReturnStatement(
            context,
            VisitExpression(context.expression())
        );
    }


    public override Node VisitWhileStatement(YabalParser.WhileStatementContext context)
    {
        return new WhileStatement(
            context,
            VisitExpression(context.expression()),
            VisitBlockStatement(context.blockStatement())
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
            elseStatement = VisitStatement(context.elseStatement().blockStatement());
        }

        if (context.elseIfStatement() != null)
        {
            foreach (var elseIf in context.elseIfStatement())
            {
                test = VisitExpression(elseIf.expression());

                elseStatement = new IfStatement(
                    elseIf,
                    test,
                    (BlockStatement) Visit(elseIf.blockStatement()),
                    elseStatement);
            }
        }

        test = VisitExpression(context.expression());

        return new IfStatement(
            context,
            test,
            (BlockStatement) Visit(context.blockStatement()),
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
                _ => throw new KeyNotFoundException("Unknown escape character")
            };
        }

        return new CharExpression(
            context,
            value[0]
        );
    }

    public override Node VisitForStatement(YabalParser.ForStatementContext context)
    {
        return new ForStatement(
            context,
            context.forInit()?.statement() is { } init ? VisitStatement(init) : null,
            context.statement() is {} statement ? VisitStatement(statement) : null,
            VisitExpression(context.expression()),
            VisitBlockStatement(context.blockStatement())
        );
    }

    public override Node VisitIncrementLeftExpression(YabalParser.IncrementLeftExpressionContext context)
    {
        return new UpdateExpression(
            context,
            VisitAssignExpression(context.expression()),
            true,
            BinaryOperator.Add
        );
    }

    public override Node VisitIncrementRightExpression(YabalParser.IncrementRightExpressionContext context)
    {
        return new UpdateExpression(
            context,
            VisitAssignExpression(context.expression()),
            false,
            BinaryOperator.Add
        );
    }

    public override Node VisitDecrementLeftExpression(YabalParser.DecrementLeftExpressionContext context)
    {
        return new UpdateExpression(
            context,
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
                        throw new NotSupportedException("Multiple default cases are not supported");
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
            context,
            expression,
            items,
            defaultValue
        );
    }

    public override Node VisitDecrementRightExpression(YabalParser.DecrementRightExpressionContext context)
    {
        return new UpdateExpression(
            context,
            VisitAssignExpression(context.expression()),
            false,
            BinaryOperator.Subtract
        );
    }

    public override Node VisitTernaryExpression(YabalParser.TernaryExpressionContext context)
    {
        return new TernaryExpression(
            context,
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

    public override Node VisitExpressionExpression(YabalParser.ExpressionExpressionContext context)
    {
        return VisitExpression(context.expression());
    }

    public override Node VisitNegateExpression(YabalParser.NegateExpressionContext context)
    {
        return new UnaryExpression(
            context,
            VisitExpression(context.expression()),
            UnaryOperator.Negate
        );
    }

    public override Node VisitNotExpression(YabalParser.NotExpressionContext context)
    {
        return new UnaryExpression(
            context,
            VisitExpression(context.expression()),
            UnaryOperator.Not
        );
    }

    public override Node VisitMinusExpression(YabalParser.MinusExpressionContext context)
    {
        return new UnaryExpression(
            context,
            VisitExpression(context.expression()),
            UnaryOperator.Minus
        );
    }

    public override Node VisitCreatePointerExpression(YabalParser.CreatePointerExpressionContext context)
    {
        var createPointer = context.createPointer();

        return new CreatePointerExpression(
            context,
            VisitExpression(createPointer.expression()),
            createPointer.integer() is {} integer ? ParseInt(integer.GetText()) : 0,
            createPointer.type() is {} type ? _typeVisitor.VisitType(type) : LanguageType.Pointer(LanguageType.Integer)
        );
    }

    public override Node VisitSizeOfExpression(YabalParser.SizeOfExpressionContext context)
    {
        return new SizeOfExpression(
            context,
            VisitExpression(context.expression())
        );
    }

    public override Node VisitContinueStatement(YabalParser.ContinueStatementContext context)
    {
        return new ContinueStatement(context);
    }

    public override Node VisitBreakStatement(YabalParser.BreakStatementContext context)
    {
        return new BreakStatement(context);
    }

    public override Node VisitIncludeBytesExpression(YabalParser.IncludeBytesExpressionContext context)
    {
        return IncludeFile(context, context.expression(), FileType.Byte);
    }

    public override Node VisitIncludeImageExpression(YabalParser.IncludeImageExpressionContext context)
    {
        return IncludeFile(context, context.expression(), FileType.Image);
    }

    private Node IncludeFile(SourceRange context, YabalParser.ExpressionContext expression, FileType type)
    {
        var value = VisitExpression(expression);

        if (value is not StringExpression stringExpression)
        {
            throw new NotSupportedException("IncludeBytes only supports string literals");
        }

        var path = stringExpression.Value;

        Files.Add((path, type));

        return new IncludeFileExpression(
            context,
            path,
            type
        );
    }

    public override Node VisitStringExpression(YabalParser.StringExpressionContext context)
    {
        var sb = new StringBuilder();
        var parts = context.@string().stringPart();

        foreach (var part in parts)
        {
            if (part.StringEscape() is {} escape)
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

        return new StringExpression(context, parts[0].GetText());
    }

    public override Node VisitLabelStatement(YabalParser.LabelStatementContext context)
    {
        return new LabelStatement(context, context.identifierName().GetText());
    }

    public override Node VisitGotoStatement(YabalParser.GotoStatementContext context)
    {
        return new GotoStatement(context, context.identifierName().GetText());
    }

    public override Node VisitStructDeclaration(YabalParser.StructDeclarationContext context)
    {
        var offset = 0;

        var reference = _typeVisitor.Structs[context.identifierName().GetText()];
        var bitOffset = 0;

        foreach (var item in context.structItem())
        {
            if (item.structField() is { } field)
            {
                var type = _typeVisitor.VisitType(field.type());
                var bitSize = field.integer() is { } integer ? (int?) ParseInt(integer.GetText()) : null;

                reference.Fields.Add(new LanguageStructField(
                    field.identifierName().GetText(),
                    type,
                    offset,
                    bitSize.HasValue ? new Bit(bitOffset, bitSize.Value) : null
                ));

                if (bitSize is { } size)
                {
                    bitOffset += size;

                    if (bitOffset > 16)
                    {
                        throw new NotSupportedException("Bitfields cannot span more than 16 bits");
                    }

                    if (bitOffset == 16)
                    {
                        offset++;
                    }
                }
                else
                {
                    offset++;
                }
            }
        }

        _typeVisitor.Structs[reference.Name] = reference;

        return new StructDeclarationStatement(context, reference);
    }

    public override Node VisitMemberExpression(YabalParser.MemberExpressionContext context)
    {
        return new MemberExpression(
            context,
            VisitAddressExpression(context.expression()),
            context.identifierName().GetText()
        );
    }

    public override Node VisitInitStructExpression(YabalParser.InitStructExpressionContext context)
    {
        var init = context.initStruct();

        return new InitStructExpression(
            context,
            init.initStructItem()
                .Select(i => new InitStructItem(i.identifierName()?.GetText(), VisitExpression(i.expression())))
                .ToList(),
            init.type() is {} type ? _typeVisitor.VisitType(type) : null
        );
    }
}
