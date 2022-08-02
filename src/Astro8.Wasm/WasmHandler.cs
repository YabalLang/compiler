using Astro8.Devices;

namespace Astro8.Wasm;

public class WasmHandler : Handler
{
    public override void SetPixel(int address, ScreenColor color)
    {
        var x = address % 64;
        var y = address / 64;

        Interop.UpdatePixel(x, y, color.R, color.G, color.B);
    }
}
