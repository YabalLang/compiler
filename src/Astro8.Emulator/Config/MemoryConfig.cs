namespace Astro8.Config;

public class MemoryConfig
{
    public int Size { get; set; } = 0xFFFF;

    public List<MemoryDeviceConfig>? Devices { get; set; }
}