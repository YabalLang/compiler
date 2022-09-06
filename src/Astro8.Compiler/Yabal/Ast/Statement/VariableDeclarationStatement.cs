using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record VariableDeclarationStatement(SourceRange Range, string Name, bool Constant, Expression? Value = null, LanguageType? Type = null) : Statement(Range)
{
    public Variable Variable { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        Value?.Initialize(builder);

        var type = Type ?? Value?.Type;

        if (type is null)
        {
            throw new Exception("Variable type is not specified");
        }

        Variable = builder.CreateVariable(Name, type, Value as IConstantValue);
    }

    public override void Build(YabalBuilder builder)
    {
        if (Value != null)
        {
            builder.SetValue(Variable.Pointer, Value);
        }
    }
}
