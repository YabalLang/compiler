namespace Astro8;

public class ProgramConfig
{
    public string Path { get; set; } = "program_machine_code";

    public int Size { get; set; } = 0x3FFE;
}

public class Config
{
    public CpuConfig Cpu { get; set; } = new();

    public ProgramConfig Program { get; set; } = new();

    public ScreenConfig Screen { get; set; } = new();

    public MemoryConfig Memory { get; set; } = new();
}
