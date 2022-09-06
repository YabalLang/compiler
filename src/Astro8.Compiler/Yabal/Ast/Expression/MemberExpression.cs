using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record MemberExpression(SourceRange Range, Expression Expression, string Name) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        var type = StoreAddressInA(builder);
        builder.LoadA_FromAddressUsingA();
        return type;
    }

    public LanguageType StoreAddressInA(YabalBuilder builder)
    {
        var type = Expression.BuildExpression(builder, false);

        if (type.StaticType != StaticType.Struct)
        {
            builder.AddError(ErrorLevel.Error, Expression.Range, ErrorMessages.MemberAccessOnNonStruct);
            builder.SetA(0);
            return LanguageType.Integer;
        }

        var field = type.StructReference?.Fields.FirstOrDefault(i => i.Name == Name);

        if (field == null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.MemberNotFound(Name));
            builder.SetA(0);
            return LanguageType.Integer;
        }

        if (field.Offset > 0)
        {
            builder.SetB(field.Offset);
            builder.Add();
        }

        return field.Type;
    }

    public override bool OverwritesB => true;
}
