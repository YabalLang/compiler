namespace Astro8.Devices;

public interface IMemoryDevice
{
    int Length { get; }

    void Initialize(Memory memory, Span<int> span, bool isState)
    {
    }

    void Write(Memory memory, int address, int value)
    {
    }

    int Read(Memory memory, Span<int> span, int address)
    {
        return span[address];
    }
}
