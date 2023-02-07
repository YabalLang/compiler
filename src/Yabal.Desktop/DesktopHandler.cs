using System.Buffers;
using System.Threading.Channels;
using Yabal;
using Yabal.Devices;

namespace Yabal.Devices;

using static SDL2.SDL;
using static SDL2.SDL.SDL_WindowFlags;
using static SDL2.SDL.SDL_RendererFlags;
using static SDL2.SDL.SDL_TextureAccess;

public record struct SetPixel(int Address, ScreenColor Color);

public class DesktopHandler : Handler, IDisposable
{
    private bool _screenEnabled;
    private readonly uint[] _textureData;
    private readonly int _length;
    private readonly int _pixelScale;
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _texture;
    private string? _pendingTitle;
    private bool _updateTexture;

    public DesktopHandler(int width = 64, int height = 64, int pixelScale = 9)
    {
        Width = width;
        Height = height;
        _textureData = new uint[width * height];
        _length = _textureData.Length;
        _pixelScale = pixelScale;
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
        if (!_updateTexture)
        {
            return;
        }

        _updateTexture = false;

        lock (_textureData)
        {
            fixed (uint* p = _textureData)
            {
                var result = SDL_UpdateTexture
                (
                    _texture,
                    default,
                    (nint)p,
                    Width * sizeof(uint)
                );

                if (result != 0)
                {
                    throw new Exception("SDL_UpdateTexture failed");
                }
            }
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
        if (address < 0 || address >= _length)
        {
            return;
        }

        lock (_textureData)
        {
            _textureData[address] = (uint)color.ARGB;
        }
    }

    public override void LogSpeed(int steps, float value)
    {
        // _pendingTitle = $"C# Astro-8 Emulator | {value}";
    }

    public override void FlushScreen()
    {
        _updateTexture = true;
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
