using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Astro8.Instructions;
using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public record Variable(InstructionPointer Pointer, LanguageType Type);

public class BlockStack
{
    private readonly Dictionary<string, Variable> _variables = new();

    public IReadOnlyDictionary<string, Variable> Variables => _variables;

    public int StackOffset { get; set; }

    public bool IsGlobal { get; set; }

    public FunctionDeclarationStatement? Function { get; set; }

    public void DeclareVariable(string name, Variable variable)
    {
        _variables[name] = variable;
    }
}

public class YabalVisitor : YabalParserBaseVisitor<Node>
{
    public override ProgramStatement VisitProgram(YabalParser.ProgramContext context)
    {
        return new ProgramStatement(
            context,
            context.statement().Select(VisitStatement).ToList()
        );
    }

    private new Statement VisitStatement(ParserRuleContext context)
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

        return result.Optimize();
    }

    public override BlockStatement VisitBlockStatement(YabalParser.BlockStatementContext context)
    {
        return new BlockStatement(
            context,
            context.statement().Select(VisitStatement).ToList()
        );
    }

    public override Node VisitDefaultVariableDeclaration(YabalParser.DefaultVariableDeclarationContext context)
    {
        return new VariableDeclarationStatement(
            context,
            context.identifierName().GetText(),
            context.expression() is {} expr ? VisitExpression(expr) : null,
            TypeVisitor.Instance.Visit(context.type())
        );
    }

    public override Node VisitAutoVariableDeclaration(YabalParser.AutoVariableDeclarationContext context)
    {
        return new VariableDeclarationStatement(
            context,
            context.identifierName().GetText(),
            VisitExpression(context.expression())
        );
    }

    public override IntegerExpression VisitIntegerExpression(YabalParser.IntegerExpressionContext context)
    {
        return new IntegerExpression(context, int.Parse(context.GetText()));
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
            VisitExpression(context.expression()[0]),
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
            VisitExpression(expressions[0]),
            CreateBinary(context, expressions, @operator)
        );
    }

    public override BinaryExpression VisitDivMulBinaryExpression(YabalParser.DivMulBinaryExpressionContext context)
    {
        var expressions = context.expression();
        var @operator = context.Div() != null ? BinaryOperator.Divide : BinaryOperator.Multiply;

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
            TypeVisitor.Instance.Visit(context.returnType()),
            context.functionParameterList().functionParameter()
                .Select(p => new FunctionParameter(
                    p.identifierName().GetText(),
                    TypeVisitor.Instance.Visit(p.type())
                ))
                .ToList(),
            VisitFunctionBody(context.functionBody())
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
        var instructions = new List<AsmInstruction>();
        var items = context.asmItems().asmStatementItem();

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item is YabalParser.AsmInstructionContext instruction)
                {
                    instructions.Add(new AsmInstruction(
                        instruction.asmIdentifier().GetText(),
                        instruction.asmArgument() is {} arg ? AsmArgumentVisitor.Instance.Visit(arg) : null
                    ));
                }
                else
                {
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
            VisitExpression(context.expression()[0]),
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

    public override Node VisitLessExpression(YabalParser.LessExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.LessThan);
    }

    public override Node VisitLessEqualExpression(YabalParser.LessEqualExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.LessThanOrEqual);
    }

    public override Node VisitGreaterExpression(YabalParser.GreaterExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.GreaterThan);
    }

    public override Node VisitGreaterEqualExpression(YabalParser.GreaterEqualExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.GreaterThanOrEqual);
    }

    public override Node VisitEqualExpression(YabalParser.EqualExpressionContext context)
    {
        return CreateBinary(context, context.expression(), BinaryOperator.Equal);
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
            VisitExpression(context.expression()),
            true,
            BinaryOperator.Add
        );
    }

    public override Node VisitIncrementRightExpression(YabalParser.IncrementRightExpressionContext context)
    {
        return new UpdateExpression(
            context,
            VisitExpression(context.expression()),
            false,
            BinaryOperator.Add
        );
    }

    public override Node VisitDecrementLeftExpression(YabalParser.DecrementLeftExpressionContext context)
    {
        return new UpdateExpression(
            context,
            VisitExpression(context.expression()),
            true,
            BinaryOperator.Subtract
        );
    }

    public override Node VisitDecrementRightExpression(YabalParser.DecrementRightExpressionContext context)
    {
        return new UpdateExpression(
            context,
            VisitExpression(context.expression()),
            false,
            BinaryOperator.Subtract
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
}
