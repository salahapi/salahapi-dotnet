using SalahAPI;
using SalahAPI.Calculations;
using TimeZoneConverter;
using Xunit;

namespace SalahAPI.Tests.Calculations;

/// <summary>
/// Parameter Case Sensitivity Tests
///
/// All string parameters (rule change frequency, override conditions, changeOn
/// weekday, calculation method name, Asr school, high-latitude adjustment,
/// midnight mode, time format, timezone) must be matched case-insensitively,
/// since configuration is served by an API that may use different casing
/// (e.g. "isna", "friday", "middleOfTheNight").
/// </summary>
public class ParameterCaseSensitivityTests
{
    // -----------------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------------

    /// <summary>
    /// Two January days (no DST) with known Athan times.
    /// </summary>
    private static Dictionary<int, DayData> JanuaryDaysData() => new()
    {
        [0] = new DayData(
            new DateTime(2023, 1, 1),
            new Dictionary<string, DateTime>
            {
                ["fajr"] = new DateTime(2023, 1, 1, 5, 30, 0),
                ["sunrise"] = new DateTime(2023, 1, 1, 7, 30, 0),
            }),
        [1] = new DayData(
            new DateTime(2023, 1, 2),
            new Dictionary<string, DateTime>
            {
                ["fajr"] = new DateTime(2023, 1, 2, 5, 31, 0),
                ["sunrise"] = new DateTime(2023, 1, 2, 7, 31, 0),
            }),
    };

    /// <summary>
    /// Days around the fall DST change with Dhuhr Athan times from the
    /// production CSV (Oct 31 is DST, Nov 1 Athan is after the fall-back).
    /// </summary>
    private static Dictionary<int, DayData> FallChangeDhuhrDaysData() => new()
    {
        [0] = new DayData(
            new DateTime(2026, 10, 31),
            new Dictionary<string, DateTime> { ["dhuhr"] = new DateTime(2026, 10, 31, 12, 52, 0) }),
        [1] = new DayData(
            new DateTime(2026, 11, 1),
            new Dictionary<string, DateTime> { ["dhuhr"] = new DateTime(2026, 11, 1, 11, 52, 0) }),
        [2] = new DayData(
            new DateTime(2026, 11, 2),
            new Dictionary<string, DateTime> { ["dhuhr"] = new DateTime(2026, 11, 2, 11, 52, 0) }),
    };

    /// <summary>
    /// Calculation method with identical simple rules for all prayers.
    /// </summary>
    private static CalculationMethod CalculationMethod(
        string? changeOn,
        string change,
        string highLatitudeAdjustment = "MiddleOfTheNight",
        string methodName = "ISNA")
    {
        return new CalculationMethod(
            methodName,
            15.0,
            15.0,
            "Standard",
            highLatitudeAdjustment,
            new IqamaCalculationRules(
                changeOn,
                new PrayerCalculationRule(null, change, 5, null, null, 20, null, null),
                new PrayerCalculationRule(null, change, 5, null, null, 25, null, null),
                new PrayerCalculationRule(null, change, 5, null, null, 10, null, null),
                new PrayerCalculationRule(null, change, 5, null, null, 5, null, null),
                new PrayerCalculationRule(null, change, 5, null, null, 15, null, null)));
    }

    private static Location NewYorkLocation() => new(40.7128, -74.0060, "America/New_York");

    private static Location SeattleLocation() => new(47.6062, -122.3321, "America/Los_Angeles");

    private static string BuildCsv(Location location, CalculationMethod method, string start, string end)
        => new Builder(location, method).BuildCsv(start, end);

    // -----------------------------------------------------------------------
    // Rule change frequency
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("weekly")]
    [InlineData("Weekly")]
    [InlineData("WEEKLY")]
    [InlineData("wEeKlY")]
    public void WeeklyChangeParameterIsCaseInsensitive(string change)
    {
        var rule = new PrayerCalculationRule(null, change, 5, null, null, 20, null, null);

        var results = IqamaCalculator.CalculateIqama(JanuaryDaysData(), "fajr", rule);

        // Weekly semantics: one value for both days (latest candidate 05:31 -> 05:35 + 20)
        Assert.Equal(new DateTime(2023, 1, 1, 5, 55, 0), results[0]);
        Assert.Equal(new DateTime(2023, 1, 2, 5, 55, 0), results[1]);
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("Daily")]
    [InlineData("DAILY")]
    [InlineData("dAiLy")]
    public void DailyChangeParameterIsCaseInsensitive(string change)
    {
        var rule = new PrayerCalculationRule(null, change, 5, null, null, 20, null, null);

        var results = IqamaCalculator.CalculateIqama(JanuaryDaysData(), "fajr", rule);

        // Daily semantics: per-day values (05:30 + 20 = 05:50, 05:31 -> 05:35 + 20 = 05:55)
        Assert.Equal(new DateTime(2023, 1, 1, 5, 50, 0), results[0]);
        Assert.Equal(new DateTime(2023, 1, 2, 5, 55, 0), results[1]);
    }

    // -----------------------------------------------------------------------
    // Override conditions
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("daylightSavingsTime")]
    [InlineData("DaylightSavingstime")]
    [InlineData("DAYLIGHTSAVINGSTIME")]
    [InlineData("daylightsavingstime")]
    [InlineData("dAyLiGhTsAvInGsTiMe")]
    public void DaylightSavingsTimeOverrideConditionIsCaseInsensitive(string condition)
    {
        var baseRule = new PrayerCalculationRule("12:30", null, null, null, null, null, null, new List<PrayerCalculationOverrideRule>
        {
            new(condition, new PrayerCalculationRule("13:30")),
        });

        var results = IqamaCalculator.CalculateIqama(FallChangeDhuhrDaysData(), "dhuhr", baseRule);

        Assert.Equal(new DateTime(2026, 10, 31, 13, 30, 0), results[0]); // Oct 31 (DST) should use the override
        Assert.Equal(new DateTime(2026, 11, 1, 12, 30, 0), results[1]);  // Nov 1 Athan is after the fall-back, should use standard
        Assert.Equal(new DateTime(2026, 11, 2, 12, 30, 0), results[2]);  // Nov 2 (standard) should use standard
    }

    [Theory]
    [InlineData("ramadan")]
    [InlineData("Ramadan")]
    [InlineData("RAMADAN")]
    [InlineData("rAmAdAn")]
    public void RamadanOverrideConditionIsCaseInsensitive(string condition)
    {
        // Feb 25, 2026 falls within Ramadan 1447
        var daysData = new Dictionary<int, DayData>
        {
            [0] = new DayData(
                new DateTime(2026, 2, 25),
                new Dictionary<string, DateTime>
                {
                    ["fajr"] = new DateTime(2026, 2, 25, 5, 32, 0),
                    ["sunrise"] = new DateTime(2026, 2, 25, 6, 57, 0),
                }),
        };

        var baseRule = new PrayerCalculationRule(null, "weekly", 15, "04:30", "06:45", null, 30, new List<PrayerCalculationOverrideRule>
        {
            new(condition, new PrayerCalculationRule(null, "daily", 1, null, null, 15, null, null)),
        });

        var results = IqamaCalculator.CalculateIqama(daysData, "fajr", baseRule, "sunrise");

        // The Ramadan override applies (daily, Athan + 15): 05:32 + 15 = 05:47
        Assert.Equal(new DateTime(2026, 2, 25, 5, 47, 0), results[0]);
    }

    // -----------------------------------------------------------------------
    // changeOn weekday
    // -----------------------------------------------------------------------

    [Fact]
    public void ChangeOnParameterIsCaseInsensitive()
    {
        var canonicalFriday = BuildCsv(
            NewYorkLocation(), CalculationMethod("Friday", "weekly"), "2023-01-07", "2023-01-12");

        foreach (var changeOn in new[] { "friday", "FRIDAY", "FriDay" })
        {
            var result = BuildCsv(
                NewYorkLocation(), CalculationMethod(changeOn, "weekly"), "2023-01-07", "2023-01-12");

            Assert.True(string.Equals(canonicalFriday, result, StringComparison.Ordinal),
                $"changeOn '{changeOn}' must batch weeks like 'Friday'");
        }

        // A lowercase weekday must not silently fall back to the Friday default
        var canonicalSunday = BuildCsv(
            NewYorkLocation(), CalculationMethod("Sunday", "weekly"), "2023-01-07", "2023-01-12");

        var lowerSunday = BuildCsv(
            NewYorkLocation(), CalculationMethod("sunday", "weekly"), "2023-01-07", "2023-01-12");

        Assert.True(string.Equals(canonicalSunday, lowerSunday, StringComparison.Ordinal),
            "changeOn 'sunday' must batch weeks like 'Sunday'");

        Assert.NotEqual(canonicalFriday, canonicalSunday); // Friday and Sunday batching must differ for this range
    }

    // -----------------------------------------------------------------------
    // High-latitude adjustment (Builder config)
    // -----------------------------------------------------------------------

    [Fact]
    public void HighLatitudeAdjustmentParameterIsCaseInsensitive()
    {
        // Seattle in June: middleOfTheNight and AngleBased produce different times
        var canonicalMotn = BuildCsv(
            SeattleLocation(), CalculationMethod(null, "daily", "MiddleOfTheNight"), "2026-06-01", "2026-06-30");

        foreach (var adjustment in new[] { "middleOfTheNight", "MIDDLEOFTHENIGHT", "middle_of_the_night", "NIGHTMIDDLE", "MIDDLE_OF_THE_NIGHT" })
        {
            var result = BuildCsv(
                SeattleLocation(), CalculationMethod(null, "daily", adjustment), "2026-06-01", "2026-06-30");

            Assert.True(string.Equals(canonicalMotn, result, StringComparison.Ordinal),
                $"highLatitudeAdjustment '{adjustment}' must behave like 'MiddleOfTheNight'");
        }

        var canonicalAngle = BuildCsv(
            SeattleLocation(), CalculationMethod(null, "daily", "AngleBased"), "2026-06-01", "2026-06-30");

        foreach (var adjustment in new[] { "ANGLE_BASED", "anglebased", "angle_based" })
        {
            var result = BuildCsv(
                SeattleLocation(), CalculationMethod(null, "daily", adjustment), "2026-06-01", "2026-06-30");

            Assert.True(string.Equals(canonicalAngle, result, StringComparison.Ordinal),
                $"highLatitudeAdjustment '{adjustment}' must behave like 'AngleBased'");
        }

        Assert.NotEqual(canonicalMotn, canonicalAngle); // The adjustment families must differ for this range
    }

    // -----------------------------------------------------------------------
    // PrayerTimes parameters
    // -----------------------------------------------------------------------

    private static Dictionary<string, string> GetTimes(
        string? methodName = null, string? school = null, string? adjustment = null,
        string? midnightMode = null, string? format = null)
    {
        return new PrayerTimes(methodName ?? Method.MethodIsna, school ?? PrayerTimes.SchoolStandard)
            .GetTimes(
                new DateTime(2026, 6, 15),
                47.6062,
                -122.3321,
                0,
                adjustment ?? PrayerTimes.LatitudeAdjustmentMethodMotn,
                midnightMode,
                format ?? PrayerTimes.TimeFormat24H,
                "America/Los_Angeles");
    }

    private static void AssertSameTimes(
        Dictionary<string, string> expected, Dictionary<string, string> actual, string because)
    {
        var differences = expected
            .Where(kv => !actual.TryGetValue(kv.Key, out var v) || !string.Equals(kv.Value, v, StringComparison.Ordinal))
            .Select(kv => $"{kv.Key}: expected {kv.Value}, got {(actual.TryGetValue(kv.Key, out var v) ? v : "<missing>")}");

        Assert.True(
            expected.Count == actual.Count && !differences.Any(),
            $"{because}. Differences: {string.Join("; ", differences)}");
    }

    [Fact]
    public void CalculationMethodNameIsCaseInsensitive()
    {
        var canonical = GetTimes(Method.MethodIsna);

        foreach (var name in new[] { "isna", "Isna", "iSnA" })
        {
            AssertSameTimes(canonical, GetTimes(name), $"Method name '{name}' must behave like 'ISNA'");
        }

        Assert.Equal(Method.MethodIsna, new PrayerTimes("isna").GetMethod()); // casing normalized to the canonical constant

        Assert.NotEqual(GetTimes(Method.MethodMwl), canonical); // ISNA and MWL must produce different times for this date
    }

    [Fact]
    public void AsrSchoolParameterIsCaseInsensitive()
    {
        var canonicalHanafi = GetTimes(school: PrayerTimes.SchoolHanafi);

        foreach (var school in new[] { "hanafi", "Hanafi", "HanaFi" })
        {
            AssertSameTimes(canonicalHanafi, GetTimes(school: school), $"Asr school '{school}' must behave like 'HANAFI'");
        }

        var canonicalStandard = GetTimes(school: PrayerTimes.SchoolStandard);

        foreach (var school in new[] { "standard", "Standard", "STANDARD" })
        {
            AssertSameTimes(canonicalStandard, GetTimes(school: school), $"Asr school '{school}' must behave like 'STANDARD'");
        }

        Assert.NotEqual(canonicalHanafi, canonicalStandard); // The Asr schools must produce different times for this date
    }

    [Fact]
    public void HighLatitudeAdjustmentMethodParameterIsCaseInsensitive()
    {
        // June 15 in Seattle: MOTN and ANGLE_BASED produce different times
        var canonicalMotn = GetTimes(adjustment: PrayerTimes.LatitudeAdjustmentMethodMotn);

        foreach (var adjustment in new[] { "middle_of_the_night", "middleOfTheNight", "MIDDLE_OF_THE_NIGHT", "NightMiddle" })
        {
            AssertSameTimes(canonicalMotn, GetTimes(adjustment: adjustment),
                $"Adjustment '{adjustment}' must behave like 'MIDDLE_OF_THE_NIGHT'");
        }

        var canonicalAngle = GetTimes(adjustment: PrayerTimes.LatitudeAdjustmentMethodAngle);

        foreach (var adjustment in new[] { "angle_based", "angleBased", "AngleBased" })
        {
            AssertSameTimes(canonicalAngle, GetTimes(adjustment: adjustment),
                $"Adjustment '{adjustment}' must behave like 'ANGLE_BASED'");
        }

        Assert.NotEqual(canonicalMotn, canonicalAngle); // The adjustment families must differ for this date
    }

    [Fact]
    public void MidnightModeParameterIsCaseInsensitive()
    {
        var canonicalJafari = GetTimes(midnightMode: PrayerTimes.MidnightModeJafari);

        foreach (var mode in new[] { "jafari", "Jafari" })
        {
            AssertSameTimes(canonicalJafari, GetTimes(midnightMode: mode),
                $"Midnight mode '{mode}' must behave like 'JAFARI'");
        }

        Assert.NotEqual(GetTimes(midnightMode: PrayerTimes.MidnightModeStandard), canonicalJafari); // The midnight modes must differ for this date
    }

    [Fact]
    public void TimeFormatParameterIsCaseInsensitive()
    {
        AssertSameTimes(GetTimes(format: "24h"), GetTimes(format: "24H"),
            "Format '24H' must behave like '24h'");
        AssertSameTimes(GetTimes(format: "12hNS"), GetTimes(format: "12hNs"),
            "Format '12hNs' must behave like '12hNS'");
        Assert.NotEqual(GetTimes(format: "24h"), GetTimes(format: "12h")); // The formats must produce different output
    }

    // -----------------------------------------------------------------------
    // Timezone
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("america/los_angeles")]
    [InlineData("AMERICA/LOS_ANGELES")]
    [InlineData("America/los_angeles")]
    public void LocationTimezoneParameterIsCaseInsensitive(string timezone)
    {
        var canonical = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        var variant = TZConvert.GetTimeZoneInfo(timezone);

        // One instant in DST and one in standard time
        var dst = new DateTime(2026, 10, 31, 12, 52, 0);
        var std = new DateTime(2026, 11, 1, 11, 52, 0);

        Assert.Equal(TimeZoneInfo.ConvertTimeToUtc(dst, canonical), TimeZoneInfo.ConvertTimeToUtc(dst, variant));
        Assert.Equal(TimeZoneInfo.ConvertTimeToUtc(std, canonical), TimeZoneInfo.ConvertTimeToUtc(std, variant));
        Assert.Equal(canonical.IsDaylightSavingTime(dst), variant.IsDaylightSavingTime(dst));
        Assert.Equal(canonical.IsDaylightSavingTime(std), variant.IsDaylightSavingTime(std));
    }

    // -----------------------------------------------------------------------
    // End-to-end
    // -----------------------------------------------------------------------

    private static CalculationMethod FromApiConfig(
        string name, string changeOn, string fajrChange, string dhuhrCondition,
        string maghribChange)
    {
        return new CalculationMethod(
            name,
            15.0,
            15.0,
            "standard",
            "middleOfTheNight",
            new IqamaCalculationRules(
                changeOn,
                new PrayerCalculationRule(null, fajrChange, 15, "04:30", "06:45", null, 30, null),
                new PrayerCalculationRule("12:30", null, null, null, null, null, null, new List<PrayerCalculationOverrideRule>
                {
                    new(dhuhrCondition, new PrayerCalculationRule("13:30")),
                }),
                new PrayerCalculationRule(null, "weekly", 15, null, null, 0, null, null),
                new PrayerCalculationRule(null, maghribChange, 1, null, null, 10, null, null),
                new PrayerCalculationRule(null, "daily", 1, "19:45", "23:30", 10, null, null)));
    }

    /// <summary>
    /// End-to-end: a configuration assembled with API-style casing (the live
    /// endpoint serves "isna", "friday", "middleOfTheNight", "standard", "hanafi")
    /// plus some aggressive casing variants must produce exactly the same output
    /// as the same configuration in canonical casing.
    /// </summary>
    [Fact]
    public void ApiCasedConfigurationMatchesCanonicalCasing()
    {
        var apiCased = FromApiConfig(
            name: "isna", changeOn: "friday", fajrChange: "WEEKLY",
            dhuhrCondition: "DAYLIGHTSAVINGSTIME", maghribChange: "DAILY");

        var canonical = FromApiConfig(
            name: "ISNA", changeOn: "friday", fajrChange: "weekly",
            dhuhrCondition: "daylightSavingsTime", maghribChange: "daily");

        var location = new Location(47.777856, -122.190302, "America/Los_Angeles");

        var apiCasedResult = BuildCsv(location, apiCased, "2026-10-29", "2026-11-12");
        var canonicalResult = BuildCsv(location, canonical, "2026-10-29", "2026-11-12");

        Assert.True(string.Equals(canonicalResult, apiCasedResult, StringComparison.Ordinal),
            "API-cased configuration must produce identical output to canonical casing");

        // The method name must actually be applied (not silently falling back to MWL)
        var mwlResult = BuildCsv(location, FromApiConfig(
            name: "MWL", changeOn: "friday", fajrChange: "WEEKLY",
            dhuhrCondition: "DAYLIGHTSAVINGSTIME", maghribChange: "DAILY"), "2026-10-29", "2026-11-12");

        Assert.NotEqual(apiCasedResult, mwlResult); // The 'isna' method name must be honored, not treated as an unknown method
    }
}
