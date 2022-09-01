namespace Astro8.Utils;

public enum OutputFormat
{
    [Alias("a", "asm")] Assembly,
    [Alias("c", "asmc")] AssemblyWithComments,
    [Alias("h", "aexe")] AstroExecutable,
    [Alias("l", "hex")] Logisim
}
