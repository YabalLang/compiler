using System.Drawing;

namespace Astro8;

using static SDL2.SDL;
using static SDL2.SDL.SDL_WindowFlags;
using static SDL2.SDL.SDL_RendererFlags;
using static SDL2.SDL.SDL_TextureAccess;

public record struct ScreenColor(uint Value)
{
    public static readonly ScreenColor White = new(0xFFFFFFFF);
    public static readonly ScreenColor Black = new(0xFF000000);

    public static implicit operator ScreenColor(uint value) => new(value);
    public static implicit operator ScreenColor(Color value) => new((uint)value.ToArgb());
}

public class Screen : IDisposable, IMemoryDevice
{
    private readonly int _pixelScale;
    private readonly uint[] _data;
    private bool _dirty;
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _texture;

    public Screen(int width = 64, int height = 64, int pixelScale = 9)
    {
        Width = width;
        Height = height;
        _pixelScale = pixelScale;
        _data = new uint[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public ScreenColor this[int index]
    {
        get => Color.FromArgb((int) _data[index]);
        set
        {
            var argb = value.Value;
            var span = _data.AsSpan();

            if (span[index] != argb)
            {
                span[index] = argb;
                _dirty = true;
            }
        }
    }

    public ScreenColor this[int x, int y]
    {
        get => this[x + y * Width];
        set => this[x + y * Width] = value;
    }

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

        _renderer = SDL_CreateRenderer(_window, -1, SDL_RENDERER_ACCELERATED);

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
            SDL_PIXELFORMAT_ARGB8888,
            (int) SDL_TEXTUREACCESS_STREAMING,
            Width,
            Height
        );

        UpdatePixels();

        return true;
    }

    public void Update()
    {
        if (_dirty)
        {
            UpdatePixels();
        }
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

    private void UpdateTexture()
    {
        SDL_RenderCopy(_renderer, _texture, default, default);
        SDL_RenderPresent(_renderer);
    }

    private unsafe void UpdatePixels()
    {
        int result;

        fixed (uint* p = _data)
        {
            result = SDL_UpdateTexture
            (
                _texture,
                default,
                (IntPtr)p,
                Width * sizeof(uint)
            );
        }

        if (result != 0)
        {
            throw new Exception("SDL_UpdateTexture failed");
        }

        UpdateTexture();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        SDL_Quit();
        GC.SuppressFinalize(this);
    }

    ~Screen()
    {
        ReleaseUnmanagedResources();
    }

    int IMemoryDevice.Length => _data.Length;

    public void Write(int address, int value)
    {
        var argb = unchecked((uint) value);
        var span = _data.AsSpan();

        if (span[address] != argb)
        {
            span[address] = argb;
            _dirty = true;
        }
    }

    public int Read(int address)
    {
        return unchecked((int) _data[address]);
    }
}
