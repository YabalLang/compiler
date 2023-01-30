namespace Astro8;

public class MemoryDeviceConfig
{
    // TODO: Allow to specify bank

    public int Screen { get; set; } = 0xD26F;

    public int Character { get; set; } = 0xD12A;

    public int Program { get; set; } = 0x0000;

    public int Keyboard { get; set; } = 0xD0FC;

    public int Mouse { get; set; } = 0xD0FD;
}
