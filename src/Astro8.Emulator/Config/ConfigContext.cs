using System.Text.Json.Serialization;

namespace Astro8.Config;

[JsonSerializable(typeof(Config))]
internal partial class ConfigContext : JsonSerializerContext
{
}