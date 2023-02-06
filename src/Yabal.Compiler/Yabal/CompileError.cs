namespace Yabal;

public record CompileError(SourceRange Range, ErrorLevel Level, string Message);
