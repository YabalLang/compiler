using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astro8;

[JsonSerializable(typeof(Config))]
internal partial class ConfigContext : JsonSerializerContext
{
    public static Config Load(string? directory)
    {
        Config? config = null;

        var path = "config.jsonc";

        if (directory != null)
        {
            path = Path.Combine(directory, path);
        }

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);

            var context = new ConfigContext(
                new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    Converters =
                    {
                        new IntJsonConverter(),
                        new AddressConverter()
                    }
                }
            );

            config = JsonSerializer.Deserialize(json, context.Config);
        }

        return config ?? new Config();
    }
}
