using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace Astro8.Yabal;

public readonly record struct SourceRange(int StartLine, int StartColumn, int EndLine, int EndColumn, int Index, int Length) : IComparable<SourceRange>
{
    public static readonly SourceRange Zero = default;

    public static (int stopLine, int stopColumn) CalculateStop(int line, int column, string text)
    {
        var match = Regex.Matches(text, @"(\r\n|\r|\n)");

        int stopLine;
        int stopColumn;

        if (match.Count > 0)
        {
            var lastMatch = match[^1];
            stopLine = line + match.Count;
            stopColumn = column + text.Length - (lastMatch.Index + lastMatch.Length);
        }
        else
        {
            stopLine = line;
            stopColumn = column + text.Length;
        }

        return (stopLine, stopColumn);
    }

    public static implicit operator SourceRange(ParserRuleContext context)
    {
        var startLine = context.Start.Line;
        var startColumn = context.Start.Column;
        var end = context.stop ?? context.start;
        var text = end.Text;
        if (text == "<EOF>") text = string.Empty;
        var (endLine, endColumn) = CalculateStop(end.Line, end.Column, text);

        return new SourceRange(
            startLine,
            startColumn,
            endLine,
            endColumn,
            context.Start.StartIndex,
            (end.StartIndex - context.Start.StartIndex) + text.Length
        );
    }

    public static SourceRange From(IToken token)
    {
        var startLine = token.Line;
        var startColumn = token.Column;
        var text = token.Text;
        var (endLine, endColumn) = CalculateStop(token.Line, token.Column, text);

        if (text == "<EOF>")
        {
            text = string.Empty;
        }

        return new SourceRange(
            startLine,
            startColumn,
            endLine,
            endColumn,
            token.StartIndex,
            text.Length
        );
    }

    public bool IsInRange(int line, int column)
    {
        return line >= StartLine && column >= StartColumn &&
               line <= EndLine && column <= EndColumn;
    }

    public static SourceRange Combine(IEnumerable<SourceRange> ranges)
    {
        var array = ranges as IReadOnlyList<SourceRange> ?? ranges.ToArray();
        var start = array.MinBy(r => r.Index);
        var end = array.MaxBy(r => r.Index + r.Length);

        return new SourceRange(
            start.StartLine,
            start.StartColumn,
            end.EndLine,
            end.EndColumn,
            start.Index,
            end.Index + end.Length - start.Index);
    }

    public override string ToString()
    {
        return $"{StartLine}:{StartColumn} - {EndLine}:{EndColumn} (index: {Index}, length: {Length})";
    }

    public int CompareTo(SourceRange other)
    {
        return Index.CompareTo(other.Index);
    }
}
