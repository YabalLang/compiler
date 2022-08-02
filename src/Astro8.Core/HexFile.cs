using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Astro8;

[SuppressMessage("ReSharper", "UseIndexFromEndExpression")]
[SuppressMessage("ReSharper", "ReplaceSliceWithRangeIndexer")]
public static class HexFile
{
    public static void LoadFile(string path, int[] data, int offset = 0)
    {
        Load(File.ReadAllText(path), data, offset);
    }

    public static void Load(string str, int[] data, int offset = 0)
    {
        var span = str.AsSpan();
        var validated = false;

        foreach (var rawLine in span.Split('\n'))
        {
            var line = rawLine;

            if (line.Length == 0)
            {
                continue;
            }

            if (line[line.Length - 1] == '\r')
            {
                line = line.Slice(0, line.Length - 1);
            }

            if (!validated)
            {
                validated = ValidateHeader(line);
                continue;
            }

            var start = line.IndexOf(' ');

            if (start == -1)
            {
                return;
            }

            for (var i = start + 1; i < line.Length;)
            {
                var value = line.Slice(i);
                var end = value.IndexOf(' ');

                if (end == -1)
                {
                    end = value.Length;
                }
                else
                {
                    end += 1;
                }

                i += end;

#if NETSTANDARD2_0
                var intValue = value.Slice(0, end).ToString();
#else
                var intValue = value[..end];
#endif

                if (int.TryParse(intValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var instruction))
                {
                    data[offset++] = instruction;
                }
            }
        }
    }

    private static bool ValidateHeader(ReadOnlySpan<char> line)
    {
        line = line.Trim();

        if (line.Length == 0)
        {
            return false;
        }

        if (line.SequenceEqual("v3.0 hex words addressed".AsSpan()))
        {
            return true;
        }

        throw new FileLoadException("Invalid file format");
    }
}
