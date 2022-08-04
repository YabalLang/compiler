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

    private new Statement VisitStatement(IParseTree context)
    {
        var result = (Statement)Visit(context);

        if (result == null)
        {
            throw new InvalidOperationException($"{context.GetType().Name} is not supported.");
        }

        return result;
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
            context.identifierName().GetText(),
            VisitExpression(context.expression())
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
        if (expressions[0] is not YabalParser.IdentifierExpressionContext identifierName)
        {
            throw new NotImplementedException();
        }

        return new AssignExpression(
            context,
            identifierName.identifierName().GetText(),
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
            (BlockStatement) Visit(context.functionBody())
        );
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
}
