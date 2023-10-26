namespace Yabal;

public class MemoryConfig
{
    public int Size { get; set; } = 0xFFFF;

    public int Banks { get; set; } = 10;

    public MemoryDeviceConfig Devices { get; set; } = new();
}
