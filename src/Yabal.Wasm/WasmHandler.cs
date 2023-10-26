using Yabal.Devices;

namespace Yabal.Browser;

public class WasmHandler : Handler
{
	private readonly byte[] _screen = new byte[108 * 108 * 4];

	public override void SetPixel(int address, ScreenColor color)
	{
		var index = address * 4;
		_screen[index + 0] = (byte)color.R;
		_screen[index + 1] = (byte)color.G;
		_screen[index + 2] = (byte)color.B;
		_screen[index + 3] = (byte)color.A;
	}

	public override void LogSpeed(int steps, float value)
	{

	}

	public override unsafe void FlushScreen()
	{
		fixed (byte* screen = _screen)
		{
			Interop.UpdateScreen((int*)screen);
		}
	}

	public override void Halt()
	{
		Interop.Halt();
		FlushScreen();
	}
}
