using System.ComponentModel.DataAnnotations;

namespace Astro8.Config;

public class MemoryDeviceConfig
{
    [Required] public string Type { get; set; } = null!;

    [Required] public int Address { get; set; }
}