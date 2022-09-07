using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Astro8.Devices;
using Astro8.Instructions;

namespace Astro8.Wasm;

public static class Interop
{
    private static int _lastA;
    private static int _lastB;
    private static int _lastC;
    private static int _lastExp;
    private static int _lastBank;
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
        UpdateContext();

        return cpu.ProgramCounter;
    }

    [UnmanagedCallersOnly(EntryPoint = "SetExpansionPort")]
    public static void SetExpansionPort(int value)
    {
        if (_cpu is {} cpu)
        {
            cpu.ExpansionPort = value;
        }
    }

    private static void UpdateContext()
    {
        if (_cpu is not {} cpu)
        {
            return;
        }

        var context = cpu.Context;

        if (_lastA != context.A)
        {
            UpdateA(context.A);
            _lastA = context.A;
        }

        if (_lastB != context.B)
        {
            UpdateB(context.B);
            _lastB = context.B;
        }

        if (_lastC != context.C)
        {
            UpdateC(context.C);
            _lastC = context.C;
        }

        if (_lastExp != cpu.ExpansionPort)
        {
            UpdateExpansionPort(cpu.ExpansionPort);
            _lastExp = cpu.ExpansionPort;
        }

        if (_lastBank != context.Bank)
        {
            UpdateBank(context.Bank);
            _lastBank = context.Bank;
        }
    }

    [DllImport("NativeLib")]
    public static extern void UpdatePixel(int address, int color);

    [DllImport("NativeLib")]
    public static extern void UpdateA(int value);

    [DllImport("NativeLib")]
    public static extern void UpdateB(int value);

    [DllImport("NativeLib")]
    public static extern void UpdateC(int value);

    [DllImport("NativeLib")]
    public static extern void UpdateExpansionPort(int value);

    [DllImport("NativeLib")]
    public static extern void UpdateBank(int value);
}
