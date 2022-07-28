using System.Text.Json.Serialization;

namespace Astro8;

[JsonSerializable(typeof(Config))]
internal partial class ConfigContext : JsonSerializerContext
{
}