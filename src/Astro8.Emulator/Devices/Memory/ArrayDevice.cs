namespace Astro8;

public class ArrayDevice : IMemoryDevice
{
    private readonly int[] _array;

    public ArrayDevice(int[] array)
    {
        _array = array;
    }

    public int Length => _array.Length;

    public void Initialize(Memory memory, Span<int> span)
    {
        _array.AsSpan().CopyTo(span);
    }
}
