using Yabal.Devices;

namespace Yabal;

public abstract class Handler
{
    public abstract void SetPixel(int address, ScreenColor color);

    public abstract void LogSpeed(int steps, float value);

    public abstract void FlushScreen();

    public abstract void Halt();

    public abstract void ShowVariable(int line, int offset, Span<int> value);
}
