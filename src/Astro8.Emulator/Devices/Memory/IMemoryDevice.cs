namespace Astro8;

public interface IMemoryDevice
{
    int Length { get; }

    void Initialize(Memory memory, Span<int> span)
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
