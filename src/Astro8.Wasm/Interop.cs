using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Astro8.Devices;
using Astro8.Instructions;

namespace Astro8.Wasm;

public static class Interop
{
    private static Cpu<WasmHandler>? _cpu;

    [UnmanagedCallersOnly(EntryPoint = "Compile")]
    public static unsafe void Compile(byte* bytes, int byteLength)
    {
        _cpu?.Halt();
        _cpu = null;

        var code = Encoding.UTF8.GetString(bytes, byteLength);
        var data = new int[0xFFFF];
        HexFile.Load(code, data);

        _cpu = CpuBuilder.Create<WasmHandler>()
            .WithMemory(data)
            .WithScreen(0xEFFF)
            .WithCharacter(0x3FFE)
            .Create();
    }

    [UnmanagedCallersOnly(EntryPoint = "Step")]
    public static void Step(int amount)
    {
        _cpu?.Step(amount);
    }

    [UnmanagedCallersOnly(EntryPoint = "SetExpansionPort")]
    public static void SetExpansionPort(int value)
    {
        if (_cpu != null)
        {
            _cpu.ExpansionPort = value;
        }
    }

    [DllImport("NativeLib")]
    public static extern unsafe void UpdatePixel(int address, int color);
}
