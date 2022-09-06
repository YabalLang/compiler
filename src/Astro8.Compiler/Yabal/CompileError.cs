using Astro8.Yabal;

namespace Astro8.Instructions;

public record CompileError(SourceRange Range, ErrorLevel Level, string Message);