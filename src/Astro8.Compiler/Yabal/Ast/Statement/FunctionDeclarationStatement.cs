using System.Diagnostics;
using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record FunctionParameter(string Name, LanguageType Type);

public record Function(
    string Name,
    InstructionLabel Label,
    LanguageType ReturnType,
    List<Variable> Parameters,
    YabalBuilder Builder
);

public record FunctionDeclarationStatement(
    SourceRange Range,
    string Name,
    LanguageType ReturnType,
    List<FunctionParameter> Parameters,
    BlockStatement Body
) : ScopeStatement(Range)
{
    private Function? _function;

    public override void OnDeclare(YabalBuilder builder)
    {
        var functionBuilder = new YabalBuilder(builder);
        functionBuilder.PushBlock(this);

        var parameters = new List<Variable>();

        foreach (var parameter in Parameters)
        {
            parameters.Add(functionBuilder.CreateVariable(parameter.Name, parameter.Type));
        }

        _function = new Function(
            Name,
            builder.CreateLabel(Name),
            ReturnType,
            parameters,
            functionBuilder
        );

        builder.DeclareFunction(_function);

        Body.Declare(builder);
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        Debug.Assert(_function != null);

        Body.Initialize(_function.Builder);
    }

    public override void OnBuild(YabalBuilder _)
    {
        Debug.Assert(_function != null);

        Body.Build(_function.Builder);
    }
}
