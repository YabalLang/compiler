using Yabal.Instructions;

namespace Yabal.Ast;

public record WhileStatement(SourceRange Range, Expression Expression, BlockStatement Body) : ScopeStatement(Range)
{
    private InstructionLabel _nextLabel = null!;
    private InstructionLabel _bodyLabel = null!;
    private InstructionLabel _endLabel = null!;

    public override void OnDeclare(YabalBuilder builder)
    {
        Body.Declare(builder);
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        _nextLabel = builder.CreateLabel();
        _bodyLabel = builder.CreateLabel();
        _endLabel = builder.CreateLabel();

        Block.Continue = _nextLabel;
        Block.Break = _endLabel;

        Expression.Initialize(builder);
        Body.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        var expression = Expression.Optimize(LanguageType.Boolean);

        builder.Mark(_nextLabel);

        if (expression is not IConstantValue {Value: true})
        {
            expression.CreateComparison(builder, _endLabel, _bodyLabel);
            builder.Mark(_bodyLabel);
        }

        Body.Build(builder);
        builder.Jump(_nextLabel);
        builder.Mark(_endLabel);
    }

    public override Statement CloneStatement()
    {
        return new WhileStatement(Range, Expression.CloneExpression(), Body.CloneStatement());
    }

    public override Statement Optimize()
    {
        return new WhileStatement(Range, Expression.Optimize(LanguageType.Boolean), Body.Optimize())
        {
            Block = Block,
            _nextLabel = _nextLabel,
            _bodyLabel = _bodyLabel,
            _endLabel = _endLabel
        };
    }
}
