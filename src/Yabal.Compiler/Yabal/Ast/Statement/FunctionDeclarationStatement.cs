using System.Diagnostics;
using Yabal.Instructions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record FunctionParameter(Identifier Name, LanguageType Type, bool HasDefault);

public record Function(
    SourceRange Range,
    Either<Identifier, BinaryOperator> Name,
    InstructionLabel Label,
    LanguageType ReturnType,
    YabalBuilder Builder,
    bool Inline,
    BlockStatement Body,
    List<FunctionParameter> Parameters,
    int RequiredParameterCount)
{
    public BlockStack? Block { get; set; }

    public List<Expression> References { get; } = new();

    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Parameters.Select(i => $"{i.Type} {i.Name}"))})";
    }
}

public record FunctionDeclarationStatement(
    SourceRange Range,
    Either<Identifier, BinaryOperator> Name,
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
        var functionBuilder = new YabalBuilder(builder)
        {
            ReturnType = ReturnType,
        };

        _function = new Function(
            Range,
            Name,
            builder.CreateLabel(),
            ReturnType,
            functionBuilder,
            Inline,
            Body,
            Parameters,
            Parameters.Count(i => !i.HasDefault)
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

        _returnLabel = builder.CreateLabel();

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

        if (Inline)
        {
            return;
        }

        var builder = _function.Builder;

        TemporaryVariable? stackAllocationAddress = null;

        if (_function.Builder.HasStackAllocation)
        {
            stackAllocationAddress = _.GetTemporaryVariable();

            builder.LoadA(builder.StackAllocPointer);
            builder.StoreA(stackAllocationAddress);
        }

        Body.Build(builder);
        builder.Mark(_returnLabel);

        if (stackAllocationAddress != null)
        {
            builder.SwapA_C();
            builder.LoadA(stackAllocationAddress);
            builder.StoreA(builder.StackAllocPointer);
            builder.SwapA_C();
            stackAllocationAddress.Dispose();
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
