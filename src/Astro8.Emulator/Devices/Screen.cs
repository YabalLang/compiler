using System.Drawing;

namespace Astro8;

using static SDL2.SDL;
using static SDL2.SDL.SDL_WindowFlags;
using static SDL2.SDL.SDL_RendererFlags;
using static SDL2.SDL.SDL_TextureAccess;

public record struct ScreenColor(int Value)
{
    public static readonly ScreenColor White = new(0xFFFF);
    public static readonly ScreenColor Black = new(0x0000);

    public static implicit operator ScreenColor(int value) => new(value);
}

public class Screen : IDisposable, IMemoryDevice
{
    private readonly int _pixelScale;
    private readonly uint[] _textureData;
    private bool _dirty;
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _texture;
    private readonly object _lock = new();

    public Screen(int width = 64, int height = 64, int pixelScale = 9)
    {
        Width = width;
        Height = height;
        _pixelScale = pixelScale;
        _textureData = new uint[width * height];
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

        lock (_lock)
        {
            fixed (uint* p = _textureData)
            {
                result = SDL_UpdateTexture
                (
                    _texture,
                    default,
                    (IntPtr) p,
                    Width * sizeof(uint)
                );
            }
        }

        if (result != 0)
        {
            throw new Exception("SDL_UpdateTexture failed");
        }

        _dirty = false;
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

    int IMemoryDevice.Length => _textureData.Length;

    public void Initialize(Memory memory, Span<int> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            _textureData[i] = GetRgba(span[i]);
        }

        _dirty = true;
    }

    public void Write(Memory memory, int address, int value)
    {
        if (address < 0 || address >= _textureData.Length)
        {
            return;
        }

        var rgba = GetRgba(value);

        if (_textureData[address] == rgba)
        {
            return;
        }

        lock (_lock)
        {
            _textureData[address] = rgba;
            _dirty = true;
        }
    }

    private static uint GetRgba(int value)
    {
        var r = Instruction.BitRange(value, 10, 5) * 8; // Get first 5 bits
        var g = Instruction.BitRange(value, 5, 5) * 8; // get middle bits
        var b = Instruction.BitRange(value, 0, 5) * 8; // Gets last 5 bits
        const int a = 255;

        var rgba = (uint) (r << 24 | g << 16 | b << 8 | a);
        return rgba;
    }
}
