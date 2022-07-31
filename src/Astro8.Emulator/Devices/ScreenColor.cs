using Astro8.Instructions;

namespace Astro8.Devices;

public readonly record struct ScreenColor(int Value)
{
    public static readonly ScreenColor White = new(0xFFFF);
    public static readonly ScreenColor Black = new(0x0000);

    public int R => InstructionReference.BitRange(Value, 10, 5) * 8;

    public int G => InstructionReference.BitRange(Value, 5, 5) * 8;

    public int B => InstructionReference.BitRange(Value, 0, 5) * 8;

    public int A => 255;

    public int ARGB =>  (R << 24 | G << 16 | B << 8 | A);

    public static implicit operator ScreenColor(int value) => new(value);
}