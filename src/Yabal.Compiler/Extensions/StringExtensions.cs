using System.Text;

namespace Yabal;

public static class StringExtensions
{
    public static string GetPeek(this string input, SourceRange range)
    {
        return GetPeek(input, range.Index, range.Length);
    }

    private static int GetLineCount(ReadOnlySpan<char> input)
    {
        var count = 0;
        var len = input.Length;

        for (var i = 0; i != len; ++i)
        {
            switch(input[i])
            {
                case '\r':
                    count++;

                    if (i + 1 != len && input[i + 1] == '\n')
                    {
                        i++;
                    }

                    break;
                case '\n':
                    count++;
                    break;
            }
        }

        return count;
    }

    public static string GetPeek(this string input, int index, int length)
    {
        const string lineSeparator = " | ";

        var text = input.AsSpan();
        var lineIndex = GetLineIndex(text, index, out var size);
        var offset = index;

        var line = 1;
        var lineWidth = 1;

        var sb = new StringBuilder();

        if (lineIndex > 0)
        {
            var previous = text.Slice(0, lineIndex - size);

            offset -= lineIndex;
            line = GetLineCount(previous) + 2;
            lineWidth = (int) Math.Floor(Math.Log10(line) + 1);

            var lineOffset = 0;
            var previousLineIndex = GetLineIndex(previous, previous.Length, out size);

            while (true)
            {
                if (lineOffset >= 2)
                {
                    break;
                }

                var currentLine = line - ++lineOffset;

                if (currentLine < 1)
                {
                    break;
                }

                var previousLine = previous.Slice(previousLineIndex);
                var linePrefix = currentLine.ToString();
                var missing = lineWidth - linePrefix.Length;

                if (missing > 0)
                {
                    linePrefix = new string(' ', missing) + linePrefix;
                }

                var prefix = linePrefix + lineSeparator;

                if (!previousLine.IsWhiteSpace())
                {
                    sb.Insert(0, prefix + previousLine.ToString() + "\n");
                    break;
                }

                sb.Insert(0, prefix + "\n");
                previous = text.Slice(0, previousLineIndex - size);
                previousLineIndex = GetLineIndex(previous, previous.Length, out size);
            }

            text = text.Slice(lineIndex);
        }

        var endIndex = text.IndexOfAny('\r', '\n');

        if (endIndex != -1)
        {
            text = text.Slice(0, endIndex);
        }

        sb.Append(line);
        sb.Append(lineSeparator);
        sb.Append(text);
        sb.Append('\n');
        sb.Append(new string(' ', offset + lineWidth + lineSeparator.Length));
        sb.Append(new string('^', Math.Max(1, length)));

        return sb.ToString();
    }

    /// <summary>
    /// Get beginning of the line index.
    /// </summary>
    private static int GetLineIndex(ReadOnlySpan<char> text, int indexPlusOne, out int size)
    {
        indexPlusOne--;
        for (; indexPlusOne >= 0; indexPlusOne--)
        {
            switch (text[indexPlusOne])
            {
                case '\r':
                    size = 1;
                    return indexPlusOne + 1;
                case '\n':
                    size = indexPlusOne > 1 && text[indexPlusOne - 1] == '\r' ? 2 : 1;
                    return indexPlusOne + 1;
            }
        }

        size = 0;
        return 0;
    }
}
