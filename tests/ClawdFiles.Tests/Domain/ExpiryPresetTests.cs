using ClawdFiles.Domain.ValueObjects;

namespace ClawdFiles.Tests.Domain;

public class ExpiryPresetTests
{
    [Theory]
    [InlineData("1h", 1)]
    [InlineData("6h", 6)]
    [InlineData("12h", 12)]
    public void Parse_HourPresets_ReturnsCorrectTimeSpan(string preset, int expectedHours)
    {
        var result = ExpiryPreset.Parse(preset);
        Assert.Equal(TimeSpan.FromHours(expectedHours), result);
    }

    [Theory]
    [InlineData("1d", 1)]
    [InlineData("3d", 3)]
    public void Parse_DayPresets_ReturnsCorrectTimeSpan(string preset, int expectedDays)
    {
        var result = ExpiryPreset.Parse(preset);
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Theory]
    [InlineData("1w", 7)]
    [InlineData("2w", 14)]
    public void Parse_WeekPresets_ReturnsCorrectTimeSpan(string preset, int expectedDays)
    {
        var result = ExpiryPreset.Parse(preset);
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Fact]
    public void Parse_MonthPreset_Returns30Days()
    {
        var result = ExpiryPreset.Parse("1m");
        Assert.Equal(TimeSpan.FromDays(30), result);
    }

    [Fact]
    public void Parse_Never_ReturnsNull()
    {
        var result = ExpiryPreset.Parse("never");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_RawSeconds_ReturnsCorrectTimeSpan()
    {
        var result = ExpiryPreset.Parse("3600");
        Assert.Equal(TimeSpan.FromSeconds(3600), result);
    }

    [Fact]
    public void Parse_NullOrEmpty_ReturnsDefault7Days()
    {
        var result = ExpiryPreset.Parse(null);
        Assert.Equal(TimeSpan.FromDays(7), result);

        result = ExpiryPreset.Parse("");
        Assert.Equal(TimeSpan.FromDays(7), result);
    }

    [Fact]
    public void Parse_InvalidPreset_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ExpiryPreset.Parse("invalid"));
    }
}
