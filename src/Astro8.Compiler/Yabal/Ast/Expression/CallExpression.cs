using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CallExpression(
    SourceRange Range,
    Expression Callee,
    List<Expression> Arguments
) : Expression(Range)
{
    public Function Function { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        if (Callee is not IdentifierExpression identifier)
        {
            throw new NotSupportedException("Callee must be an identifier");
        }

        Function = builder.GetFunction(identifier.Name);

        foreach (var argument in Arguments)
        {
            argument.Initialize(builder);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.Call(Function.Label, Arguments);
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Function.ReturnType;
}
