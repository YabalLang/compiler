using System.Runtime.InteropServices;
using System.Text;
using Yabal.Devices;

namespace Yabal.Browser;

public static class Interop
{
	private static Cpu<WasmHandler>? _cpu;

	[UnmanagedCallersOnly(EntryPoint = "Compile")]
	public static unsafe void Compile(byte* bytes, int byteLength)
	{
		_cpu?.Halt();
		_cpu = null;

		try
		{
			var code = Encoding.UTF8.GetString(bytes, byteLength);
			int[] data;

			using (var context = new YabalContext())
			{
				var builder = new YabalBuilder(context)
				{
					Debug = true
				};

				builder.CompileCodeWithoutFiles(code);

				var program = builder.Build();

				data = program.ToArray();
			}

			_cpu = CpuBuilder.Create<WasmHandler>()
				.WithMemory(0, data)
				.WithScreen()
				.WithCharacter()
				.WithDebug()
				.Create();
		}
		catch (Exception)
		{
			Console.WriteLine("Failed to compile");
		}
	}

	[UnmanagedCallersOnly(EntryPoint = "Step")]
	public static int Step(int amount)
	{
		if (_cpu is not {} cpu)
		{
			return 0;
		}

		try
		{
			cpu.Step(amount);
			return cpu.ProgramCounter;
		}
		catch (Exception)
		{
			cpu.Halt();
			return -1;
		}
	}

	[DllImport("NativeLib")]
	public static extern unsafe void UpdateScreen(int* screen);

	[DllImport("NativeLib")]
	public static extern unsafe void ShowVariable(int line, int offset, int size, int* screen);

	[DllImport("NativeLib")]
	public static extern unsafe void Halt();
}
