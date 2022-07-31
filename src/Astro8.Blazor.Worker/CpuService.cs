using System.Diagnostics;
using Astro8.Blazor.Devices;
using Astro8.Devices;

namespace Astro8.Blazor;

public class CpuService
{
    public event EventHandler<int[]>? SetPixel;

    public bool Start(string code)
    {
        var screen = new CanvasScreen(this);
        var characterScreen = new CharacterDevice(screen);

        void FlushPixels()
        {
            if (screen.PendingPixels.Count == 0 || SetPixel == null)
            {
                return;
            }

            var data = new int[screen.PendingPixels.Count * 5];

            for (var i = 0; i < screen.PendingPixels.Count; i++)
            {
                var pixel = screen.PendingPixels[i];
                data[i * 5 + 0] = pixel.X;
                data[i * 5 + 1] = pixel.Y;
                data[i * 5 + 2] = pixel.Color.R;
                data[i * 5 + 3] = pixel.Color.G;
                data[i * 5 + 4] = pixel.Color.B;
            }

            screen.PendingPixels.Clear();

            SetPixel(this, data);
        }

        var instructions = HexFile.Load(code)
            .Take(0x3FFE)
            .ToArray();

        var program = new ArrayDevice(instructions);
        var memory = new Memory();

        memory.Map(0x0000, program);
        memory.Map(0x3FFE, characterScreen);
        memory.Map(0xEFFF, screen);

        var cpu = new Cpu(memory);
        var sw = Stopwatch.StartNew();

        while (cpu.Step())
        {
            if (sw.ElapsedMilliseconds > 16)
            {
                FlushPixels();
                sw.Restart();
            }
        }

        FlushPixels();

        return true;
    }
}
