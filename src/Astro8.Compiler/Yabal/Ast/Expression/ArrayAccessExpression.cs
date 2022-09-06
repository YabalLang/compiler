using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record ArrayAccessExpression(SourceRange Range, Expression Array, Expression Key) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (Array is IConstantValue { Value: IAddress constantAddress } &&
            Key is IConstantValue { Value: int constantKey } &&
            constantAddress.Get(builder) is {} value)
        {
            var elementSize = constantAddress.Type.Size;

            if (constantAddress.Type.StaticType == StaticType.Struct)
            {
                switch (value)
                {
                    case { IsLeft: true, Left: var left }:
                        builder.SetA_Large(left + constantKey * elementSize);
                        break;
                    case { IsRight: true, Right: var right }:
                        builder.SetA_Large(right);
                        builder.SetPointerOffset(constantKey * elementSize);
                        break;
                }
            }
            else
            {
                switch (value)
                {
                    case { IsLeft: true, Left: var left }:
                        builder.LoadA_Large(left + constantKey * elementSize);
                        break;
                    case { IsRight: true, Right: var right }:
                        builder.LoadA_Large(right);
                        builder.SetPointerOffset(constantKey * elementSize);
                        break;
                }
            }

            return constantAddress.Type;
        }

        var type = StoreAddressInA(builder, Array, Key);

        if (type.ElementType == null)
        {
            builder.AddError(ErrorLevel.Error, Array.Range, ErrorMessages.ValueIsNotAnArray);
            builder.SetA(0);
            return LanguageType.Integer;
        }

        if (type.ElementType.StaticType != StaticType.Struct)
        {
            builder.LoadA_FromAddressUsingA();
        }

        return type.ElementType;
    }

    public override bool OverwritesB => Array.OverwritesB || Key is not IntegerExpressionBase { Value: 0 };

    public static LanguageType StoreAddressInA(YabalBuilder builder, Expression array, Expression key)
    {
        var type = array.BuildExpression(builder, false);

        if (type != LanguageType.Assembly && (type.StaticType != StaticType.Array || type.ElementType == null))
        {
            builder.AddError(ErrorLevel.Error, array.Range, ErrorMessages.InvalidArrayAccess);
            builder.SetA(0);
            return LanguageType.Integer;
        }

        var elementSize = type.ElementType?.Size ?? 1;

        if (key is IntegerExpressionBase { Value: var intValue })
        {
            var offset = intValue * elementSize;

            if (offset == 0)
            {
                return type;
            }

            builder.SetB(offset);
            builder.Add();

            builder.SetComment($"add {intValue} to pointer address");
        }
        else if (!key.OverwritesB && elementSize == 1)
        {
            builder.SwapA_B();
            var keyType = key.BuildExpression(builder, false);

            if (keyType.StaticType != StaticType.Integer)
            {
                builder.AddError(ErrorLevel.Error, key.Range, ErrorMessages.ArrayOnlyIntegerKey);
                builder.SetB(0);
            }

            builder.Add();

            builder.SetComment("add to pointer address");
        }
        else
        {
            using var variable = builder.GetTemporaryVariable();
            builder.StoreA(variable);

            var keyType = key.BuildExpression(builder, false);

            if (keyType.StaticType != StaticType.Integer)
            {
                builder.AddError(ErrorLevel.Error, key.Range, ErrorMessages.ArrayOnlyIntegerKey);
                builder.SetB(0);
            }

            if (elementSize > 1)
            {
                builder.SetB(elementSize);
                builder.Mult();
            }

            builder.LoadB(variable);
            builder.Add();

            builder.SetComment("add to pointer address");
        }

        return type;
    }

    public override Expression Optimize(BlockCompileStack block)
    {
        if (Array is not IConstantValue {Value: IAddress address} ||
            Key is not IConstantValue {Value: int index})
        {
            return this;
        }

        var value = address.GetValue(index);

        if (value.HasValue)
        {
            return new IntegerExpression(Range, value.Value);
        }

        return this;
    }
}
