using System.Text.Json;
using System.Text.Json.Serialization;
using Yabal;

namespace Yabal;

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
                    AllowTrailingCommas = true,
                    Converters =
                    {
                        new IntJsonConverter(),
                        new AddressConverter()
                    }
                }
            );

            try
            {
                config = JsonSerializer.Deserialize(json, context.Config);
            }
            catch(Exception e)
            {
                Console.WriteLine($"Failed to parse config file: {path}: {e.Message}");
                config = null;
            }
        }

        return config ?? new Config();
    }
}
