using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ProgramStatement(
    SourceRange Range,
    List<Statement> Statements
) : BlockStatement(Range, Statements);
