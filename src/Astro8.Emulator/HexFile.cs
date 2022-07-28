using System.Globalization;

namespace Astro8;

public static class HexFile
{
    public static IEnumerable<int> LoadFile(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);

        foreach (var value in Load(reader))
        {
            yield return value;
        }
    }

    public static IEnumerable<int> Load(TextReader reader)
    {
        CheckHeader(reader);

        while (reader.ReadLine() is { } line)
        {
            var start = line.AsSpan().IndexOf(' ');

            if (start == -1)
            {
                yield break;
            }

            for (var i = start + 1; i < line.Length;)
            {
                if (i + 5 > line.Length)
                {
                    continue;
                }

                int result;

                {
                    var value = line.AsSpan()[i..];
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

                    if (!int.TryParse(value[..end], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
                    {
                        continue;
                    }
                }

                yield return result;
            }
        }
    }

    private static void CheckHeader(TextReader reader)
    {
        while (true)
        {
            var line = reader.ReadLine();

            if (line is null)
            {
                throw new FileLoadException("Empty file provided");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Equals("v3.0 hex words addressed", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            throw new FileLoadException("Invalid file format");
        }
    }
}
