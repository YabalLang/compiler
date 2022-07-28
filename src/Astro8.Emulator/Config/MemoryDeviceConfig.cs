using System.ComponentModel.DataAnnotations;

namespace Astro8;

public class MemoryDeviceConfig
{
    [Required] public string Type { get; set; } = null!;

    [Required] public int Address { get; set; }
}