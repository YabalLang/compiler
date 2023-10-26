using Yabal.Devices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Yabal.Devices;

namespace Yabal.Bot.Handler;

public class ImageHandler : global::Yabal.Handler
{
    private ImageFrame<Rgba32>? _frame;

    public ImageHandler()
    {
        Image = new Image<Rgba32>(108, 108);
        _frame = Image.Frames.RootFrame;
    }

    public bool DidFlush { get; private set; }

    public Image<Rgba32> Image { get; }

    public override void SetPixel(int address, ScreenColor color)
    {
        if (_frame == null)
        {
            _frame = Image.Frames.CreateFrame();

            var meta = _frame.Metadata.GetGifMetadata();
            meta.FrameDelay = 5;
        }

        _frame[address % 108, address / 108] = new Rgba32(color.R, color.G, color.B, color.A);
    }

    public override void LogSpeed(int steps, float value)
    {
    }

    public override void FlushScreen()
    {
        if (Image.Frames.Count > 10)
        {
            return;
        }

        DidFlush = true;
        _frame = null;
    }

    public override void Halt()
    {
    }

    public override void ShowVariable(int line, int offset, Span<int> value)
    {
    }
}
