using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CharExpression(SourceRange Range, char Value) : Expression(Range), IConstantValue, IConstantIntValue
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        builder.SetA(GetValue(Value));
        return LanguageType.Integer;
    }

    private static int GetValue(char value)
    {
        return value switch
        {
            ' ' => 0,
            '+' => 3,
            '-' => 4,
            '*' => 5,
            '/' => 6,
            '_' => 8,
            '<' => 9,
            '>' => 10,
            '|' => 11,
            'A' or 'a' => 13,
            'B' or 'b' => 14,
            'C' or 'c' => 15,
            'D' or 'd' => 16,
            'E' or 'e' => 17,
            'F' or 'f' => 18,
            'G' or 'g' => 19,
            'H' or 'h' => 20,
            'I' or 'i' => 21,
            'J' or 'j' => 22,
            'K' or 'k' => 23,
            'L' or 'l' => 24,
            'M' or 'm' => 25,
            'N' or 'n' => 26,
            'O' or 'o' => 27,
            'P' or 'p' => 28,
            'Q' or 'q' => 29,
            'R' or 'r' => 30,
            'S' or 's' => 31,
            'T' or 't' => 32,
            'U' or 'u' => 33,
            'V' or 'v' => 34,
            'W' or 'w' => 35,
            'X' or 'x' => 36,
            'Y' or 'y' => 37,
            'Z' or 'z' => 38,
            '0' => 39,
            '1' => 40,
            '2' => 41,
            '3' => 42,
            '4' => 43,
            '5' => 44,
            '6' => 45,
            '7' => 46,
            '8' => 47,
            '9' => 48,
            _ => throw new KeyNotFoundException($"Unknown character '{value}'"),
        };
    }

    object IConstantValue.Value => Value;

    int IConstantIntValue.Value => GetValue(Value);
}
