namespace Astro8;

public class MemoryConfig
{
    public int Size { get; set; } = 0xFFFF;

    public MemoryDeviceConfig Devices { get; set; } = new();
}
