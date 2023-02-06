namespace Yabal.Tests;

public class SpanTest
{
    [Fact]
    public void True()
    {
        Span<bool?> flags = new bool?[] {null, true};

        Assert.True(flags.HasFlag(0b01));
    }

    [Fact]
    public void False()
    {
        Span<bool?> flags = new bool?[] {null, false};

        Assert.False(flags.HasFlag(0b01));
    }
}
