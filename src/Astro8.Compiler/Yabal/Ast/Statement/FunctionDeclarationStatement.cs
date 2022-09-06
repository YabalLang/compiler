using System.Diagnostics;
using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record FunctionParameter(string Name, LanguageType Type);

public record Function(
    string Name,
    InstructionLabel Label,
    LanguageType ReturnType,
    List<FunctionParameter> Parameters,
    YabalBuilder Builder
);

public record FunctionDeclarationStatement(
    SourceRange Range,
    string Name,
    LanguageType ReturnType,
    List<FunctionParameter> Parameters,
    BlockStatement Body
) : Statement(Range)
{
    private Function? _function;

    public override void BeforeBuild(YabalBuilder builder)
    {
        Body.BeforeBuild(builder);

        _function = new Function(
            Name,
            builder.CreateLabel(Name),
            ReturnType,
            Parameters,
            new YabalBuilder(builder)
        );

        builder.DeclareFunction(_function);
    }

    public override void Build(YabalBuilder _)
    {
        Debug.Assert(_function != null);
        var builder = _function.Builder;

        builder.PushBlock(this);

        var block = builder.Block;

        foreach (var parameter in Parameters)
        {
            var pointer = builder.Stack.GetNext(block, parameter.Type.Size);
            var variable = new Variable(
                parameter.Name,
                pointer,
                parameter.Type
            );

            pointer.AssignedVariables.Add(variable);
            block.DeclareVariable(parameter.Name, variable);
        }

        Body.Build(builder);
        builder.PopBlock();
    }
}
