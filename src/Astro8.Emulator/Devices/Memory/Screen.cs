namespace Astro8.Devices;

public abstract class Screen : IMemoryDevice
{
    private readonly int[] _pixels;
    private readonly int[] _overlay;

    protected Screen(int width = 64, int height = 64)
    {
        Width = width;
        Height = height;
        Length = width * height;
        _pixels = new int[width * height];
        _overlay = new int[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    protected abstract void SetPixel(int address, ScreenColor color);

    private void UpdatePixel(int address)
    {
        var color = _overlay[address];

        if (color == 0)
        {
            color = _pixels[address];
        }

        SetPixel(address, color);
    }

    public virtual int Length { get; }

    public void Initialize(Memory memory, Span<int> span, bool isState)
    {
        for (var i = 0; i < span.Length; i++)
        {
            SetPixel(i, span[i]);
        }
    }

    public void Write(Memory memory, int address, int value)
    {
        if (_pixels[address] == value)
        {
            return;
        }

        _pixels[address] = value;
        UpdatePixel(address);
    }

    public void WriteOverlay(int address, int value)
    {
        if (address < 0 || address >= _overlay.Length)
        {
            return;
        }

        if (_overlay[address] == value)
        {
            return;
        }

        _overlay[address] = value;
        UpdatePixel(address);
    }
}
