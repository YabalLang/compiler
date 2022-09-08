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
            .WithMemory(0, data)
            .WithScreen()
            .WithCharacter()
            .Create();
    }

    [UnmanagedCallersOnly(EntryPoint = "Step")]
    public static int Step(int amount)
    {
        if (_cpu is not {} cpu)
        {
            return 0;
        }

        cpu.Step(amount);

        return cpu.ProgramCounter;
    }

    [UnmanagedCallersOnly(EntryPoint = "SetExpansionPort")]
    public static void SetExpansionPort(int id, int value)
    {
        if (_cpu is {} cpu)
        {
            cpu.ExpansionPorts[id] = value;
        }
    }

    [DllImport("NativeLib")]
    public static extern void UpdatePixel(int address, int color);
}
