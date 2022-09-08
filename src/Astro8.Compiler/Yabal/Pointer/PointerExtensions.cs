namespace Astro8.Yabal;

public static class PointerExtensions
{
    public static Pointer Add(this Pointer pointer, int offset)
    {
        return offset == 0 ? pointer : new PointerWithOffset(pointer, offset);
    }
}