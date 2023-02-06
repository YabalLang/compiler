namespace Yabal;

public class MemoryConfig
{
    public int Size { get; set; } = 0xFFFF;

    public int Banks { get; set; } = 2;

    public MemoryDeviceConfig Devices { get; set; } = new();
}
