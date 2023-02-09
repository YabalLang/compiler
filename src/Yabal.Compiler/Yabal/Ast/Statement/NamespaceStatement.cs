namespace Yabal.Ast;

public record NamespaceStatement(SourceRange Range, Namespace Namespace, BlockStatement Body) : ScopeStatement(Range)
{
    public override void OnDeclare(YabalBuilder builder)
    {
        if (builder.Block.Namespace.Namespaces.Count > 0)
        {
            builder.Block.Namespace = new Namespace(
                builder.Block.Namespace.Namespaces.Concat(Namespace.Namespaces).ToArray()
            );
        }
        else
        {
            builder.Block.Namespace = Namespace;
        }

        Body.Declare(builder);
    }

    public override void OnInitialize(YabalBuilder builder)
    {
        Body.Initialize(builder);
    }

    public override void OnBuild(YabalBuilder builder)
    {
        Body.Build(builder);
    }

    public override Statement CloneStatement()
    {
        return new NamespaceStatement(Range, Namespace, Body.CloneStatement());
    }

    public override Statement Optimize()
    {
        return new NamespaceStatement(Range, Namespace, Body.Optimize());
    }
}
