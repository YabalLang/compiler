using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record FunctionDeclarationStatement(
    SourceRange Range,
    LanguageType ReturnType,
    BlockStatement Body
) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        builder.PushBlock();
        // None
        builder.PopBlock();
    }
}
