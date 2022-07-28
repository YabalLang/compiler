namespace Astro8;

public interface IMemoryDevice
{
    int Length { get; }

    void Write(int address, int value);

    int Read(int address);
}
