using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astro8;

[JsonSerializable(typeof(Config))]
internal partial class ConfigContext : JsonSerializerContext
{
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
