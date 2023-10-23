using Yabal.Exceptions;

namespace Yabal.Ast;

public record InitStructItem(Identifier? Name, Expression Value);

public record InitStructExpression(SourceRange Range, List<InitStructItem> Items, LanguageType? StructType) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        foreach (var item in Items)
        {
            item.Value.Initialize(builder);
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        builder.InitStruct(suggestedType, pointer, this);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        throw new InvalidCodeException("Cannot use struct initializer as an expression", Range);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => StructType ?? throw new InvalidOperationException();

    public override Expression CloneExpression()
    {
        return new InitStructExpression(
            Range,
            Items.Select(i => i with { Value = i.Value.CloneExpression() }).ToList(),
            StructType
        );
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        var items = Items.Select(i => i with { Value = i.Value.Optimize(suggestedType) }).ToList();

        if (suggestedType is not { StructReference: { } structRef } ||
            !items.All(i => i.Value is IConstantValue { HasConstantValue: true }))
        {
            return new InitStructExpression(Range, items, StructType);
        }

        var data = new int[suggestedType.Size];

        for (var i = 0; i < items.Count; i++)
        {
            var (name, expression) = items[i];
            var range = name?.Range ?? expression.Range;
            var field = name != null
                ? structRef.Fields.FirstOrDefault(f => f.Name == name.Name)
                : structRef.Fields[i];

            if (field == null)
            {
                throw new InvalidCodeException(
                    $"Struct {structRef.Name} does not have a field named {name?.Name ?? $"at index {i}"}", range);
            }

            var value = expression as IConstantValue;

            if (value == null)
            {
                throw new InvalidCodeException("Struct initializer must be constant", range);
            }

            var fieldOffset = field.Offset;

            if (field.Bit is { } bit)
            {
                var current = data[fieldOffset];
                var bits = (1 << bit.Size) - 1;
                var intValue = (int)value.Value!;

                var mask = bits << bit.Offset;
                var masked = current & ~mask;

                data[fieldOffset] = masked | (intValue << bit.Offset);
            }
            else
            {
                value.StoreConstantValue(data.AsSpan(fieldOffset, field.Type.Size));
            }
        }

        return new ConstantMemoryExpression(Range, suggestedType, data);
    }
}
