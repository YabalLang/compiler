using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ImportStatement(SourceRange Range, string Path, Dictionary<string, string>? Mappings = null) : ScopeStatement(Range)
{
    private ProgramStatement _program = null!;

    public override void OnDeclare(YabalBuilder builder)
    {
        var code = builder.FileSystem.File.ReadAllText(Path);
        var program = builder.Parse(code);

        _program = program;

        Block = builder.PushBlock();
        program.Declare(builder);
        builder.PopBlock();
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        _program.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        _program.Build(builder);
    }

    public override Statement CloneStatement()
    {
        return new ImportStatement(Range, Path, Mappings)
        {
            Block = Block,
            _program = _program
        };
    }

    public override Statement Optimize()
    {
        return new ImportStatement(Range, Path, Mappings)
        {
            Block = Block,
            _program = _program.Optimize()
        };
    }
}
