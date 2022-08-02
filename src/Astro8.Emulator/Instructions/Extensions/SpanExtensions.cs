using System.Diagnostics.CodeAnalysis;

namespace Astro8;

public static class StringExtensions
{
    public static bool HasFlag(this Span<bool?> flags, int value)
    {
        for (var i = 0; i < flags.Length; i++)
        {
            var flag = flags[i];

            if (!flag.HasValue)
            {
                continue;
            }

            var mask = 1 << (flags.Length - i - 1);
            var hasFlag = (value & mask) != 0;

            if (flag.Value != hasFlag)
            {
                return false;
            }
        }

        return true;
    }

    public static int IndexOf(this string[] array, ReadOnlySpan<char> value)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var item = array[i];

            if (item.AsSpan().Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool TryGetValue<T>(
        this IReadOnlyDictionary<string, T> dictionary,
        ReadOnlySpan<char> key,
        StringComparison comparisonType,
        [NotNullWhen(true)] out T? value)
        where T : notnull
    {
        if (dictionary is Dictionary<string, T> dict)
        {
            foreach (var kv in dict)
            {
                if (kv.Key.AsSpan().Equals(key, comparisonType))
                {
                    value = kv.Value;
                    return true;
                }
            }
        }
        else
        {
            foreach (var kv in dictionary)
            {
                if (kv.Key.AsSpan().Equals(key, comparisonType))
                {
                    value = kv.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    public static SpanSplitEnumerator Split(this ReadOnlySpan<char> span, char value)
    {
        return new SpanSplitEnumerator(span, value);
    }

    public static bool TrySplit(this ReadOnlySpan<char> span, char value, out ReadOnlySpan<char> left, out ReadOnlySpan<char> right)
    {
        var start = span.IndexOf(value);

        if (start == -1)
        {
            left = default;
            right = default;
            return false;
        }

        left = span.Slice(0, start);
        right = span.Slice(start + 1);
        return true;
    }

    public ref struct SpanSplitEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        private readonly char _value;

        public SpanSplitEnumerator(ReadOnlySpan<char> remaining, char value)
        {
            _remaining = remaining;
            _value = value;
            Current = default;
        }

        public SpanSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var span = _remaining;
            if (span.Length == 0)
            {
                return false;
            }

            var index = span.IndexOf(_value);

            if (index == -1)
            {
                _remaining = ReadOnlySpan<char>.Empty;
                Current = span;
                return true;
            }

            Current = span.Slice(0, index);
            _remaining = span.Slice(index + 1);
            return true;
        }

        public ReadOnlySpan<char> Current { get; private set; }
    }
}
