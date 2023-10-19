using Yabal.Visitor;

namespace Yabal.Ast;

public abstract record Statement(SourceRange Range) : Node(Range)
{
    public virtual void Declare(YabalBuilder builder)
    {
    }

    public abstract void Build(YabalBuilder builder);

    public abstract Statement CloneStatement();

    public abstract Statement Optimize();
}

public abstract record ScopeStatement(SourceRange Range) : Statement(Range)
{
    private BlockStack? _block;

    protected BlockStack Block
    {
        get => _block ?? throw new InvalidOperationException("Block not set");
        set => _block = value;
    }

    protected virtual BlockStack CreateBlock(YabalBuilder builder)
    {
        return builder.PushBlock();
    }

    public virtual void OnDeclare(YabalBuilder builder)
    {
    }

    public sealed override void Declare(YabalBuilder builder)
    {
        OnDeclare(builder);
    }

    public virtual void OnInitialize(YabalBuilder builder)
    {
    }

    public sealed override void Initialize(YabalBuilder builder)
    {
        if (_block == null)
        {
            _block = CreateBlock(builder);
        }
        else
        {
            builder.PushBlock(_block);
        }
        OnInitialize(builder);
        builder.PopBlock();
    }

    public virtual void OnBuild(YabalBuilder builder)
    {
    }

    public sealed override void Build(YabalBuilder builder)
    {
        builder.PushBlock(Block);
        OnBuild(builder);
        builder.PopBlock();
    }
}
