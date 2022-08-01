using System.Globalization;

namespace Astro8;

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

            if (line[^1] == '\r')
            {
                line = line[..^1];
            }

            if (!validated)
            {
                validated = ValidateHeader(new string(line));
                continue;
            }

            var start = line.IndexOf(' ');

            if (start == -1)
            {
                return;
            }

            for (var i = start + 1; i < line.Length;)
            {
                var value = line[i..];
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

                if (int.TryParse(value[..end], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var instruction))
                {
                    data[offset++] = instruction;
                }
            }
        }
    }

    private static bool ValidateHeader(string? line)
    {
        if (line is null)
        {
            throw new FileLoadException("Empty file provided");
        }

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (line.Equals("v3.0 hex words addressed", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        throw new FileLoadException("Invalid file format");
    }
}
