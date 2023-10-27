using Yabal.Devices;

namespace Yabal;

public class MemoryDeviceConfig
{
    public Address Screen { get; set; } = new(1, 0xD26E);

    public Address Character { get; set; } = new(1, 0xD12A);

    public Address Program { get; set; } = new(0, 0x0000);

    public Address Keyboard { get; set; } = new(1, 0xD0FC);

    public Address Mouse { get; set; } = new(1, 0xD0FD);
}
