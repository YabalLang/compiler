namespace Astro8;

public class ArrayDevice : IMemoryDevice
{
    private readonly int[] _array;

    public ArrayDevice(int[] array)
    {
        _array = array;
    }

    public int Length => _array.Length;

    public void Write(int address, int value)
    {
        _array[address] = value;
    }

    public int Read(int address)
    {
        return _array[address];
    }
}
