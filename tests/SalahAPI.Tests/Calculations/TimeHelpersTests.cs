using SalahAPI.Calculations;
using Xunit;

namespace SalahAPI.Tests.Calculations;

public class TimeHelpersTests
{
    [Fact]
    public void ParseTimeString_ValidTime_ReturnsCorrectDateTime()
    {
        var date = new DateTime(2023, 6, 1);
        var result = TimeHelpers.ParseTimeString(date, "13:45");

        Assert.Equal(2023, result.Year);
        Assert.Equal(6, result.Month);
        Assert.Equal(1, result.Day);
        Assert.Equal(13, result.Hour);
        Assert.Equal(45, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void ParseTimeString_InvalidFormat_Throws()
    {
        var date = new DateTime(2023, 6, 1);
        Assert.Throws<ArgumentException>(() => TimeHelpers.ParseTimeString(date, "1345"));
    }

    [Theory]
    [InlineData(12, 37, 5, 12, 35)]   // round down to :35
    [InlineData(12, 40, 5, 12, 40)]   // already on boundary, no change
    [InlineData(12, 41, 5, 12, 40)]   // round down to :40
    [InlineData(12, 59, 15, 12, 45)]  // round down to :45
    public void RoundDown_GivesExpectedTime(int hour, int minute, int rounding, int expectedHour, int expectedMinute)
    {
        var time = new DateTime(2023, 1, 1, hour, minute, 0);
        var result = TimeHelpers.RoundDown(time, rounding);

        Assert.Equal(expectedHour, result.Hour);
        Assert.Equal(expectedMinute, result.Minute);
    }

    [Theory]
    [InlineData(12, 37, 5, 12, 40)]   // round up to :40
    [InlineData(12, 40, 5, 12, 40)]   // already on boundary, stays the same
    [InlineData(12, 41, 5, 12, 45)]   // round up to :45
    [InlineData(12, 46, 15, 13, 0)]   // round up to next hour
    public void RoundUp_GivesExpectedTime(int hour, int minute, int rounding, int expectedHour, int expectedMinute)
    {
        var time = new DateTime(2023, 1, 1, hour, minute, 0);
        var result = TimeHelpers.RoundUp(time, rounding);

        Assert.Equal(expectedHour, result.Hour);
        Assert.Equal(expectedMinute, result.Minute);
    }

    [Fact]
    public void ConvertToArabicNumerals_ConvertsDigits()
    {
        var result = TimeHelpers.ConvertToArabicNumerals("12:30");
        Assert.Equal("١٢:٣٠", result);
    }
}
