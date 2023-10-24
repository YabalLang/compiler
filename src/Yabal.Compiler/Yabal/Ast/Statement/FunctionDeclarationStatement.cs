using System.Diagnostics;
using Yabal.Instructions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record FunctionParameter(Identifier Name, LanguageType Type, bool HasDefault);

public record FunctionName(SourceRange Range);

public record FunctionIdentifier(SourceRange Range, string Name) : FunctionName(Range)
{
    public override string ToString()
    {
        return Name;
    }
}

public record FunctionOperator(SourceRange Range, BinaryOperator Operator) : FunctionName(Range)
{
    public override string ToString()
    {
        return $"operator:{Operator}";
    }
}

public record FunctionCast(SourceRange Range, LanguageType Type) : FunctionName(Range)
{
    public override string ToString()
    {
        return $"cast:{Type}";
    }
}

public record Function(
    SourceRange Range,
    Namespace Namespace,
    FunctionName? Name,
    InstructionLabel Label,
    LanguageType ReturnType,
    YabalBuilder Builder,
    bool Inline,
    BlockStatement Body,
    List<FunctionParameter> Parameters,
    int RequiredParameterCount)
    : IVariable
{
    private bool _isUsed;

    public bool ReadOnly => true;

    public BlockStack? Block { get; set; }

    public List<Identifier> References { get; } = new();

    public bool CanBeRemoved => References.Count == 0 && !_isUsed;

    public bool DidWarnFuzzy { get; set; }

    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Parameters.Select(i => $"{i.Type} {i.Name}"))})";
    }

    bool IVariable.IsGlobal => true;

    public bool IsDirectReference => true;

    Pointer IVariable.Pointer => Label;

    LanguageType IVariable.Type => new(StaticType.Function, FunctionType: new LanguageFunction(ReturnType, Parameters.Select(i => i.Type).ToList()));

    Expression? IVariable.Initializer => null;

    bool IVariable.Constant { get; set; } = true;

    void IVariable.AddReference(Identifier identifierExpression)
    {
        References.Add(identifierExpression);
    }

    void IVariable.AddUsage()
    {
        _isUsed = true;
    }

    public void MarkUsed()
    {
        _isUsed = true;
    }
}

public record FunctionDeclarationStatement(
    SourceRange Range,
    FunctionName? Name,
    LanguageType ReturnType,
    List<FunctionParameter> Parameters,
    BlockStatement Body,
    bool Inline
) : ScopeStatement(Range)
{
    private Function? _function;
    private InstructionLabel _returnLabel = null!;

    public Function Function => _function!;

    public override void OnDeclare(YabalBuilder builder)
    {
        var functionBuilder = new YabalBuilder(builder)
        {
            ReturnType = ReturnType,
        };

        _function = new Function(
            Range,
            builder.Block.Namespace,
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

    public override FunctionDeclarationStatement CloneStatement()
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

    public override FunctionDeclarationStatement Optimize()
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
