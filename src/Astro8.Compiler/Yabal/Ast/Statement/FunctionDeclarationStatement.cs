using System.Diagnostics;
using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record FunctionParameter(Identifier Name, LanguageType Type);

public record Function(string Name,
    InstructionLabel Label,
    LanguageType ReturnType,
    YabalBuilder Builder,
    bool Inline,
    BlockStatement Body,
    List<FunctionParameter> Parameters)
{
    public BlockStack? Block { get; set; }
}

public record FunctionDeclarationStatement(
    SourceRange Range,
    string Name,
    LanguageType ReturnType,
    List<FunctionParameter> Parameters,
    BlockStatement Body,
    bool Inline
) : ScopeStatement(Range)
{
    private Function? _function;
    private InstructionLabel _returnLabel = null!;

    public override void OnDeclare(YabalBuilder builder)
    {
        var functionBuilder = new YabalBuilder(builder);

        _function = new Function(
            Name,
            builder.CreateLabel(Name),
            ReturnType,
            functionBuilder,
            Inline,
            Body,
            Parameters
        );

        builder.DeclareFunction(_function);

        if (!Inline)
        {
            Block = functionBuilder.PushBlock(this);

            foreach (var parameter in Parameters)
            {
                functionBuilder.CreateVariable(parameter.Name, parameter.Type);
            }

            Body.Declare(builder);
        }
        else
        {
            _function.Block = builder.Block;
        }
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        Debug.Assert(_function != null);

        _returnLabel = builder.CreateLabel(Name);

        Block.Return = _returnLabel;

        if (!Inline)
        {
            Body.Initialize(_function.Builder);
            builder.Variables.AddRange(_function.Builder.Variables);
        }
    }

    public override void OnBuild(YabalBuilder _)
    {
        Debug.Assert(_function != null);

        if (!Inline)
        {
            Body.Build(_function.Builder);
            _function.Builder.Mark(_returnLabel);
        }
    }

    public override Statement CloneStatement()
    {
        return new FunctionDeclarationStatement(
            Range,
            Name,
            ReturnType,
            Parameters,
            Body.CloneStatement(),
            Inline
        );
    }

    public override Statement Optimize()
    {
        var body = Body.Optimize();

        return new FunctionDeclarationStatement(
            Range,
            Name,
            ReturnType,
            Parameters,
            body,
            Inline
        )
        {
            Block = Block,
            _function = _function,
            _returnLabel = _returnLabel
        };
    }
}
