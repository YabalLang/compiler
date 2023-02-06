namespace Yabal.Ast;

public record StructDeclarationStatement(SourceRange Range, LanguageStruct Struct) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
    }

    public override void Build(YabalBuilder builder)
    {
    }

    public override Statement CloneStatement() => this;

    public override Statement Optimize() => this;
}
