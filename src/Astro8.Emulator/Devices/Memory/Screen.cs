namespace Astro8.Devices;

public sealed class Screen<THandler> : MemoryDevice
    where THandler : Handler
{
    private readonly THandler _handler;
    private readonly int[] _pixels;
    private readonly int[] _overlay;

    public Screen(int bank, int address, THandler handler, int width, int height)
        : base(address, width * height)
    {
        _handler = handler;
        Bank = bank;
        Width = width;
        Height = height;
        _pixels = new int[width * height];
        _overlay = new int[width * height];
    }

    public int Bank { get; }

    public int Width { get; }

    public int Height { get; }

    private void UpdatePixel(int address)
    {
        var color = _overlay[address];

        if (color == 0)
        {
            color = _pixels[address];
        }

        _handler.SetPixel(address, color);
    }

    public override void Initialize(Span<int> span, bool isState)
    {
        for (var i = 0; i < span.Length; i++)
        {
            _handler.SetPixel(i, span[i]);
        }
    }

    public override void Write(int address, int value)
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
