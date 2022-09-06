using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record StructDeclarationStatement(SourceRange Range, LanguageStruct Struct) : Statement(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
    }

    public override void Build(YabalBuilder builder)
    {
    }
}
