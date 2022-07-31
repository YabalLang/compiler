namespace Astro8.Devices;

public class ArrayDevice : IMemoryDevice
{
    private readonly int[] _array;

    public ArrayDevice(int[] array)
    {
        _array = array;
    }

    public int Length => _array.Length;

    public void Initialize(Memory memory, Span<int> span, bool isState)
    {
        if (!isState)
        {
            _array.AsSpan().CopyTo(span);
        }
    }
}
