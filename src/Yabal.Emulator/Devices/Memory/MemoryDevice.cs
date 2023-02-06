namespace Yabal.Devices;

public abstract class MemoryDevice
{
    protected MemoryDevice(int address, int length)
    {
        Length = length;
        Address = address;
    }

    public int Address { get; }

    public int Length { get; }

    public virtual void Initialize(Span<int> span, bool isState)
    {
    }

    public virtual void Write(int address, int value)
    {
    }
}
