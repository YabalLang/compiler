using System.Threading.Channels;

namespace Astro8.Devices;

using static SDL2.SDL;
using static SDL2.SDL.SDL_WindowFlags;
using static SDL2.SDL.SDL_RendererFlags;
using static SDL2.SDL.SDL_TextureAccess;

public record struct SetPixel(int Address, ScreenColor Color);

public class DesktopHandler : Handler, IDisposable
{
    private bool _screenEnabled;
    private readonly uint[] _textureData;
    private readonly int _pixelScale;
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _texture;
    private readonly Channel<SetPixel> _channel;
    private readonly ChannelReader<SetPixel> _reader;
    private readonly ChannelWriter<SetPixel> _writer;
    private string? _pendingTitle;

    public DesktopHandler(int width = 64, int height = 64, int pixelScale = 9)
    {
        Width = width;
        Height = height;
        _textureData = new uint[width * height];
        _pixelScale = pixelScale;
        _channel = Channel.CreateUnbounded<SetPixel>();
        _reader = _channel.Reader;
        _writer = _channel.Writer;
    }

    public int Width { get; }

    public int Height { get; }

    public bool Init()
    {
        if (SDL_Init(SDL_INIT_VIDEO) != 0)
        {
            return false;
        }

        _window = SDL_CreateWindow(
            "C# Astro-8 Emulator",
            SDL_WINDOWPOS_UNDEFINED,
            SDL_WINDOWPOS_UNDEFINED,
            Width * _pixelScale,
            Height * _pixelScale,
            SDL_WINDOW_SHOWN
        );

        if (_window == IntPtr.Zero)
        {
            return false;
        }

        _renderer = SDL_CreateRenderer(_window, -1, SDL_RENDERER_ACCELERATED | SDL_RENDERER_PRESENTVSYNC);

        if (_renderer == IntPtr.Zero)
        {
            ReleaseUnmanagedResources();
            return false;
        }

        SDL_RenderSetLogicalSize(_renderer, Width * _pixelScale, Height * _pixelScale);
        SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "nearest");
        var result = SDL_RenderSetScale(_renderer, _pixelScale, _pixelScale);

        if (result != 0)
        {
            ReleaseUnmanagedResources();
            return false;
        }

        _texture = SDL_CreateTexture(
            _renderer,
            SDL_PIXELFORMAT_RGBA8888,
            (int) SDL_TEXTUREACCESS_STREAMING,
            Width,
            Height
        );
        _screenEnabled = true;

        UpdatePixels();

        return true;
    }

    public void Update()
    {
        if (!_screenEnabled) return;

        UpdatePixels();

        if (_pendingTitle != null)
        {
            SDL_SetWindowTitle(_window, _pendingTitle);
            _pendingTitle = null;
        }
    }

    private void UpdateTexture()
    {
        if (!_screenEnabled) return;

        SDL_RenderCopy(_renderer, _texture, default, default);
        SDL_RenderPresent(_renderer);
    }

    private unsafe void UpdatePixels()
    {
        int result;

        fixed (uint* p = _textureData)
        {
            var changed = false;

            while (_reader.TryRead(out var pixel))
            {
                var argb = (uint)pixel.Color.ARGB;

                if (*(p + pixel.Address) == argb)
                {
                    continue;
                }

                *(p + pixel.Address) = argb;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            result = SDL_UpdateTexture
            (
                _texture,
                default,
                (IntPtr) p,
                Width * sizeof(uint)
            );
        }

        if (result != 0)
        {
            throw new Exception("SDL_UpdateTexture failed");
        }

        UpdateTexture();
    }

    private void ReleaseUnmanagedResources()
    {
        if (_texture != IntPtr.Zero) {
            SDL_DestroyTexture(_texture);
            _texture = IntPtr.Zero;
        }

        if (_renderer != IntPtr.Zero) {
            SDL_DestroyRenderer(_renderer);
            _renderer = IntPtr.Zero;
        }

        if (_window != IntPtr.Zero) {
            SDL_DestroyWindow(_window);
            _window = IntPtr.Zero;
        }
    }

    public override void SetPixel(int address, ScreenColor color)
    {
        if (address < 0 || address >= _textureData.Length)
        {
            return;
        }

        _writer.TryWrite(
            new SetPixel(address, color)
        );
    }

    public override void LogSpeed(int steps, float value)
    {
        // _pendingTitle = $"C# Astro-8 Emulator | {value}";
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        SDL_Quit();
        GC.SuppressFinalize(this);
    }

    ~DesktopHandler()
    {
        ReleaseUnmanagedResources();
    }

}
