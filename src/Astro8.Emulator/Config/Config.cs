using System.Text.Json;

namespace Astro8.Config;

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

    public static Config Load()
    {
        Config? config = null;

        if (File.Exists("config.jsonc"))
        {
            var json = File.ReadAllText("config.jsonc");

            var context = new ConfigContext(
                new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    Converters =
                    {
                        new IntJsonConverter()
                    }
                }
            );

            config = JsonSerializer.Deserialize(json, context.Config);
        }

        return config ?? new Config();
    }
}
