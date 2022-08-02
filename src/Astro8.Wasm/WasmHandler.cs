using Astro8.Devices;

namespace Astro8.Wasm;

public class WasmHandler : Handler
{
    public override void SetPixel(int address, ScreenColor color)
    {
        Interop.UpdatePixel(address, color.Value);
    }
}
