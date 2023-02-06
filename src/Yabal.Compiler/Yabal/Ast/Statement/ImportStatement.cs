namespace Yabal.Ast;

public record ImportStatement(SourceRange Range, ProgramStatement Program, Dictionary<string, string>? Mappings = null) : ScopeStatement(Range)
{
    public override void OnDeclare(YabalBuilder builder)
    {
        Block = builder.PushBlock();
        Program.Declare(builder);
        builder.PopBlock();
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        Program.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        Program.Build(builder);
    }

    public override Statement CloneStatement()
    {
        return new ImportStatement(Range, Program, Mappings)
        {
            Block = Block
        };
    }

    public override Statement Optimize()
    {
        return new ImportStatement(Range, Program, Mappings)
        {
            Block = Block
        };
    }
}
