namespace Yabal.Ast;

public record ProgramStatement(
    SourceRange Range,
    List<Statement> Statements
) : BlockStatement(Range, Statements, false)
{
    public override ProgramStatement Optimize()
    {
        return new ProgramStatement(Range, Statements.Select(s => s.Optimize()).ToList());
    }
}
