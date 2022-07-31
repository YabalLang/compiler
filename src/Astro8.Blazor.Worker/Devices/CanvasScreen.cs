using Astro8.Devices;

namespace Astro8.Blazor.Devices;

public record struct SetPixel(int X, int Y, ScreenColor Color);

public class CanvasScreen : Screen
{
    private readonly CpuService _service;

    public CanvasScreen(CpuService service, int width = 64, int height = 64)
        : base(width, height)
    {
        _service = service;
    }

    public List<SetPixel> PendingPixels { get; } = new();

    protected override void SetPixel(int address, ScreenColor color)
    {
        var x = address % Width;
        var y = address / Width;
        PendingPixels.Add(new SetPixel(x, y, color));
    }
}
