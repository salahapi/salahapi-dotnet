using SalahAPI;
using SalahAPI.Calculations;
using System.Text.RegularExpressions;
using Xunit;

namespace SalahAPI.Tests.Calculations;

public class BuilderTests
{
    private static Location CreateTestLocation() => new(
        40.7128, -74.0060, "America/New_York", "yyyy-MM-dd", "HH:mm");

    private static CalculationMethod CreateDailyCalculationMethod()
    {
        var iqamaRules = new IqamaCalculationRules(
            changeOn: null,
            fajr:     new PrayerCalculationRule(change: "daily", roundMinutes: 5, afterAthanMinutes: 20),
            dhuhr:    new PrayerCalculationRule(change: "daily", roundMinutes: 5, afterAthanMinutes: 25),
            asr:      new PrayerCalculationRule(change: "daily", roundMinutes: 5, afterAthanMinutes: 10),
            maghrib:  new PrayerCalculationRule(change: "daily", roundMinutes: 5, afterAthanMinutes: 5),
            isha:     new PrayerCalculationRule(change: "daily", roundMinutes: 5, afterAthanMinutes: 15));

        return new CalculationMethod("MWL", 18.0, 17.0, "Standard", "MiddleOfTheNight", iqamaRules);
    }

    private static CalculationMethod CreateWeeklyCalculationMethod()
    {
        var iqamaRules = new IqamaCalculationRules(
            changeOn: "Friday",
            fajr:     new PrayerCalculationRule(change: "weekly", roundMinutes: 5, afterAthanMinutes: 20),
            dhuhr:    new PrayerCalculationRule(change: "weekly", roundMinutes: 5, afterAthanMinutes: 25),
            asr:      new PrayerCalculationRule(change: "weekly", roundMinutes: 5, afterAthanMinutes: 10),
            maghrib:  new PrayerCalculationRule(change: "weekly", roundMinutes: 5, afterAthanMinutes: 5),
            isha:     new PrayerCalculationRule(change: "weekly", roundMinutes: 5, afterAthanMinutes: 15));

        return new CalculationMethod("MWL", 18.0, 17.0, "Standard", "MiddleOfTheNight", iqamaRules);
    }

    [Fact]
    public void BuilderConstruction_DoesNotThrow()
    {
        var builder = new Builder(CreateTestLocation(), CreateDailyCalculationMethod());
        Assert.NotNull(builder);
    }

    [Fact]
    public void Build_SingleDayDaily_ReturnsHeaderPlusOneRow()
    {
        var builder = new Builder(CreateTestLocation(), CreateDailyCalculationMethod());
        var result = builder.Build("2023-01-15", "2023-01-15");

        // header + 1 data row
        Assert.Equal(2, result.Count);

        // header check
        Assert.Equal(new[]
        {
            "day", "fajr_athan", "fajr_iqama", "sunrise",
            "dhuhr_athan", "dhuhr_iqama", "asr_athan", "asr_iqama",
            "maghrib_athan", "maghrib_iqama", "isha_athan", "isha_iqama"
        }, result[0]);

        // data row date
        Assert.Equal("2023-01-15", result[1][0]);

        // all time columns are HH:mm
        for (int i = 1; i <= 11; i++)
            Assert.Matches(@"^\d{2}:\d{2}$", result[1][i]);
    }

    [Fact]
    public void Build_MultipleDaysDaily_ReturnsCorrectCount()
    {
        var builder = new Builder(CreateTestLocation(), CreateDailyCalculationMethod());
        var result = builder.Build("2023-01-15", "2023-01-20");

        // header + 6 data rows
        Assert.Equal(7, result.Count);

        // Dates are sequential
        for (int i = 0; i < 6; i++)
            Assert.Equal(new DateTime(2023, 1, 15).AddDays(i).ToString("yyyy-MM-dd"), result[i + 1][0]);
    }

    [Fact]
    public void Build_WeeklyFrequency_IqamaTimesAreConsistentWithinWeek()
    {
        var builder = new Builder(CreateTestLocation(), CreateWeeklyCalculationMethod());
        // Use a date range that falls entirely within ONE weekly batch.
        // changeOn=Friday → a batch runs from Friday to the following Thursday.
        // Jan 20 2023 is a Friday; Jan 26 is the following Thursday.
        var result = builder.Build("2023-01-20", "2023-01-26");

        // header + 7 rows
        Assert.Equal(8, result.Count);

        // All Fajr Iqama values should be identical within a single weekly batch
        var fajrIqamas = result.Skip(1).Select(r => r[2]).ToList();
        Assert.True(fajrIqamas.Distinct().Count() == 1,
            $"Expected all weekly Fajr iqamas to be equal, got: {string.Join(", ", fajrIqamas)}");
    }

    [Fact]
    public void BuildAssociative_ReturnsDictionaryPerDay()
    {
        var builder = new Builder(CreateTestLocation(), CreateDailyCalculationMethod());
        var result = builder.BuildAssociative("2023-01-15", "2023-01-17");

        Assert.Equal(3, result.Count);
        Assert.True(result[0].ContainsKey("day"));
        Assert.True(result[0].ContainsKey("fajr_athan"));
        Assert.Equal("2023-01-15", result[0]["day"]);
    }

    [Fact]
    public void BuildCsv_ProducesValidCsvString()
    {
        var builder = new Builder(CreateTestLocation(), CreateDailyCalculationMethod());
        var csv = builder.BuildCsv("2023-01-15", "2023-01-15");

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("day,fajr_athan", lines[0]);
        Assert.Contains("2023-01-15", lines[1]);
    }

    [Fact]
    public void Build_IncludeAsrMethods_AddsTwoExtraColumns()
    {
        var builder = new Builder(CreateTestLocation(), CreateDailyCalculationMethod(),
            elevation: 0, includeAsrMethods: true);

        var result = builder.Build("2023-01-15", "2023-01-15");

        Assert.Contains("asr_athan_standard", result[0]);
        Assert.Contains("asr_athan_hanafi", result[0]);
        Assert.Equal(14, result[0].Length);

        // Both extra columns should be HH:mm
        Assert.Matches(@"^\d{2}:\d{2}$", result[1][12]);
        Assert.Matches(@"^\d{2}:\d{2}$", result[1][13]);
    }

    [Fact]
    public void Build_NoIqamaRules_IqamaColumnsAreEmpty()
    {
        var method = new CalculationMethod("MWL", 18.0, 17.0);
        var builder = new Builder(CreateTestLocation(), method);

        var result = builder.Build("2023-01-15", "2023-01-15");

        // Iqama columns (odd-indexed, except day/sunrise): indices 2, 5, 7, 9, 11
        Assert.Equal("", result[1][2]);  // fajr_iqama
        Assert.Equal("", result[1][5]);  // dhuhr_iqama
        Assert.Equal("", result[1][7]);  // asr_iqama
        Assert.Equal("", result[1][9]);  // maghrib_iqama
        Assert.Equal("", result[1][11]); // isha_iqama
    }
}
