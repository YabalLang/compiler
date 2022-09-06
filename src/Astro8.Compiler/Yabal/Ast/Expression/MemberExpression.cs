using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record MemberExpression(SourceRange Range, IAddressExpression Expression, string Name) : Expression(Range), IAddressExpression
{
    public LanguageStructField Field { get; set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);

        var field = Expression.Type.StructReference?.Fields.FirstOrDefault(f => f.Name == Name);
        Field = field ?? throw new InvalidOperationException($"Struct {Expression.Type} does not contain a field named {Name}");
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Expression.Type.StructReference?.Fields.FirstOrDefault(i => i.Name == Name)?.Type ?? LanguageType.Unknown;

    public Pointer? Pointer => Expression is { Pointer: {} pointer }
        ? pointer.Add(Field.Offset)
        : null;

    public void StoreAddressInA(YabalBuilder builder)
    {
        Expression.StoreAddressInA(builder);
        builder.SetB(Field.Offset);
        builder.Add();
    }

    public override string ToString()
    {
        return $"{Expression}.{Name}";
    }
}
