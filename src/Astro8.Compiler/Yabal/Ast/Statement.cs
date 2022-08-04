namespace Astro8.Yabal.Ast;

public abstract record Statement(SourceRange Range) : Node(Range)
{
}
