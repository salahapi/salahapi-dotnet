using SalahAPI.Calculations;
using Xunit;

namespace SalahAPI.Tests.Calculations;

public class IqamaCalculatorTests
{
    private static DayData MakeDayData(DateTime date, int fajrHour, int fajrMin,
        int sunriseHour, int sunriseMin, int dhuhrHour, int dhuhrMin,
        int asrHour, int asrMin, int maghribHour, int maghribMin,
        int ishaHour, int ishaMin)
    {
        var athan = new Dictionary<string, DateTime>
        {
            ["fajr"]    = new DateTime(date.Year, date.Month, date.Day, fajrHour, fajrMin, 0),
            ["sunrise"] = new DateTime(date.Year, date.Month, date.Day, sunriseHour, sunriseMin, 0),
            ["dhuhr"]   = new DateTime(date.Year, date.Month, date.Day, dhuhrHour, dhuhrMin, 0),
            ["asr"]     = new DateTime(date.Year, date.Month, date.Day, asrHour, asrMin, 0),
            ["maghrib"] = new DateTime(date.Year, date.Month, date.Day, maghribHour, maghribMin, 0),
            ["isha"]    = new DateTime(date.Year, date.Month, date.Day, ishaHour, ishaMin, 0),
        };
        return new DayData(date, athan);
    }

    [Fact]
    public void CalculateIqama_NullRule_ReturnsEmpty()
    {
        var daysData = new Dictionary<int, DayData>
        {
            [0] = MakeDayData(new DateTime(2023, 1, 15), 5, 52, 7, 20, 11, 59, 14, 30, 16, 38, 17, 58)
        };

        var result = IqamaCalculator.CalculateIqama(daysData, "fajr", null);
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateIqama_StaticRule_ReturnsStaticTime()
    {
        var daysData = new Dictionary<int, DayData>
        {
            [0] = MakeDayData(new DateTime(2023, 1, 15), 5, 52, 7, 20, 11, 59, 14, 30, 16, 38, 17, 58)
        };

        var rule = new PrayerCalculationRule(@static: "06:15");
        var result = IqamaCalculator.CalculateIqama(daysData, "fajr", rule);

        Assert.Single(result);
        Assert.Equal(6, result[0].Hour);
        Assert.Equal(15, result[0].Minute);
    }

    [Fact]
    public void CalculateIqama_DailyAfterAthan_AddsMinutesAndRounds()
    {
        var day = new DateTime(2023, 1, 15);
        var daysData = new Dictionary<int, DayData>
        {
            [0] = MakeDayData(day, 5, 52, 7, 20, 11, 59, 14, 30, 16, 38, 17, 58)
        };

        // Round up to 5-minute boundary then add 20 minutes
        // Fajr = 05:52 → RoundUp(5) = 05:55 → +20 = 06:15
        var rule = new PrayerCalculationRule(change: "daily", roundMinutes: 5, afterAthanMinutes: 20);
        var result = IqamaCalculator.CalculateIqama(daysData, "fajr", rule);

        Assert.Single(result);
        Assert.Equal(6, result[0].Hour);
        Assert.Equal(15, result[0].Minute);
    }

    [Fact]
    public void CalculateIqama_WeeklyChange_AllDaysSameIqama()
    {
        var daysData = new Dictionary<int, DayData>();
        for (int i = 0; i < 7; i++)
        {
            var date = new DateTime(2023, 1, 15).AddDays(i);
            // Fajr varies by a few minutes each day
            daysData[i] = MakeDayData(date, 5, 50 + i, 7, 20, 11, 59, 14, 30, 16, 38, 17, 58);
        }

        var rule = new PrayerCalculationRule(change: "weekly", roundMinutes: 5, afterAthanMinutes: 20);
        var result = IqamaCalculator.CalculateIqama(daysData, "fajr", rule);

        Assert.Equal(7, result.Count);

        // All iqama times must be identical (weekly calculation uses the worst-case day)
        var uniqueTimes = result.Values.Select(dt => dt.ToString("HH:mm")).Distinct().ToList();
        Assert.Single(uniqueTimes);
    }

    [Fact]
    public void CalculateIqama_BeforeEnd_CalculatesCorrectly()
    {
        var day = new DateTime(2023, 1, 15);
        var daysData = new Dictionary<int, DayData>
        {
            [0] = MakeDayData(day, 5, 52, 7, 20, 11, 59, 14, 30, 16, 38, 17, 58)
        };

        // Sunrise = 07:20 → RoundDown(5) = 07:20 → -30 = 06:50
        var rule = new PrayerCalculationRule(change: "daily", roundMinutes: 5, beforeEndMinutes: 30);
        var result = IqamaCalculator.CalculateIqama(daysData, "fajr", rule, endPrayerName: "sunrise");

        Assert.Single(result);
        Assert.Equal(6, result[0].Hour);
        Assert.Equal(50, result[0].Minute);
    }
}
