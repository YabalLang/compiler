namespace Astro8.Devices;

public class ArrayDevice : MemoryDevice
{
    private readonly int[] _array;

    public ArrayDevice(int address, int[] array)
        : base(address, array.Length)
    {
        _array = array;
    }

    public override void Initialize(Span<int> span, bool isState)
    {
        if (!isState)
        {
            _array.AsSpan().CopyTo(span);
        }
    }

    public override void Write(int address, int value)
    {
    }
}
