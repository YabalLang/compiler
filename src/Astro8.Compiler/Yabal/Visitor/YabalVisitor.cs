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
        return (Statement) Visit(context);
    }

    private new Expression VisitExpression(IParseTree context)
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

    public override VariableDeclarationStatement VisitVariableDeclaration(YabalParser.VariableDeclarationContext context)
    {
        return new VariableDeclarationStatement(
            context,
            context.identifierName().GetText(),
            context.expression() is {} expr ? VisitExpression(expr) : null,
            context.type() is {} type ? TypeVisitor.Instance.Visit(type) : null
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

    public override AssignStatement VisitAssignStatement(YabalParser.AssignStatementContext context)
    {
        return new AssignStatement(
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

        return new AssignStatement(
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
}
