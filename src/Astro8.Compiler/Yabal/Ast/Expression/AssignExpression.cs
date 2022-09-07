using System.Diagnostics.CodeAnalysis;
using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AssignExpression(SourceRange Range, IAssignExpression Object, Expression Value) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Object.Initialize(builder);
        Value.Initialize(builder);

        Object.MarkModified();
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        Object.Assign(builder, Value);
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Value.Type;

    public override Expression CloneExpression()
    {
        return new AssignExpression(Range, Object.Clone(), Value.CloneExpression());
    }

    public override Expression Optimize()
    {
        return new AssignExpression(Range, Object, Value.Optimize());
    }
}
