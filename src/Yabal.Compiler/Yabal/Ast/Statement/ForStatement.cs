using Yabal.Instructions;

namespace Yabal.Ast;

public record ForStatement(SourceRange Range, Statement? Init, Statement? Update, Expression Test, BlockStatement Body) : ScopeStatement(Range)
{
    private InstructionLabel _nextLabel;
    private InstructionLabel _bodyLabel;
    private InstructionLabel _endLabel;
    private InstructionLabel _testLabel;

    public override void OnDeclare(YabalBuilder builder)
    {
        Init?.Declare(builder);
        Update?.Declare(builder);
        Body.Declare(builder);
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        _nextLabel = builder.CreateLabel();
        _bodyLabel = builder.CreateLabel();
        _endLabel = builder.CreateLabel();
        _testLabel = builder.CreateLabel();

        Block.Continue = _nextLabel;
        Block.Break = _endLabel;

        Init?.Initialize(builder);
        Update?.Initialize(builder);
        Test.Initialize(builder);
        Body.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        Init?.Build(builder);
        builder.Jump(_testLabel);

        builder.Mark(_nextLabel);
        Update?.Build(builder);

        builder.Mark(_testLabel);
        Test.CreateComparison(builder, _endLabel, _bodyLabel);

        builder.Mark(_bodyLabel);
        Body.Build(builder);
        builder.Jump(_nextLabel);
        builder.SetComment("jump to next iteration");

        builder.Mark(_endLabel);
    }

    public override Statement CloneStatement()
    {
        return new ForStatement(
            Range,
            Init?.CloneStatement(),
            Update?.CloneStatement(),
            Test.CloneExpression(),
            Body.CloneStatement()
        );
    }

    public override Statement Optimize()
    {
        return new ForStatement(
            Range,
            Init?.Optimize(),
            Update?.Optimize(),
            Test.Optimize(LanguageType.Boolean),
            Body.Optimize()
        )
        {
            Block = Block,
            _nextLabel = _nextLabel,
            _bodyLabel = _bodyLabel,
            _endLabel = _endLabel,
            _testLabel = _testLabel
        };
    }
}
