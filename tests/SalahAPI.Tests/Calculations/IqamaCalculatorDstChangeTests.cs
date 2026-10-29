using System.Globalization;
using SalahAPI;
using SalahAPI.Calculations;
using Xunit;

namespace SalahAPI.Tests.Calculations;

/// <summary>
/// Validates Iqama time calculation around the DST changes against production data.
///
/// Fall change: clocks fall back on Sunday, November 1, 2026 at 2:00 AM in
/// America/Los_Angeles (prayer_times_db_2026-10-29.csv).
///
/// Spring change: clocks spring forward on Sunday, March 14, 2027 at 2:00 AM in
/// America/Los_Angeles (prayer_times_db_2026-10-29_2.csv).
///
/// Expected values are taken from the production output and the calculation rules
/// mirror the live configuration served by:
/// https://bothellmosque.org/wp-json/muslim-prayer-times/v1/salah-api
///
/// The Asr Iqama is driven by the Hanafi Asr Athan (asrIqamaCalculationMethod:
/// "hanafi"), which is not part of the CSV export. Those Athan times are calculated
/// with PrayerTimes using the API configuration (ISNA method, middleOfTheNight
/// high-latitude adjustment, Bothell coordinates). The calculated standard Asr
/// Athan is validated against the CSV's asr_athan column first, so the same engine
/// can be trusted to produce the Hanafi times.
/// </summary>
public class IqamaCalculatorDstChangeTests
{
    private const string Timezone = "America/Los_Angeles";

    private const double Latitude = 47.777856;
    private const double Longitude = -122.190302;

    /// <summary>
    /// Production rows around the fall change (prayer_times_db_2026-10-29.csv).
    ///
    /// Columns: day, fajr_athan, fajr_iqama, sunrise, dhuhr_athan, dhuhr_iqama,
    /// asr_athan, asr_iqama, maghrib_athan, maghrib_iqama, isha_athan, isha_iqama
    /// </summary>
    private static readonly string[][] FallCsvRows =
    {
        new[] { "10/29/26", "6:22 AM", "6:45 AM", "7:49 AM", "12:52 PM", "1:30 PM", "3:28 PM", "4:30 PM", "5:56 PM", "6:06 PM", "7:22 PM", "7:45 PM" },
        new[] { "10/30/26", "6:24 AM", "6:45 AM", "7:50 AM", "12:52 PM", "1:30 PM", "3:27 PM", "4:15 PM", "5:55 PM", "6:05 PM", "7:20 PM", "7:45 PM" },
        new[] { "10/31/26", "6:25 AM", "6:45 AM", "7:52 AM", "12:52 PM", "1:30 PM", "3:26 PM", "4:15 PM", "5:53 PM", "6:03 PM", "7:19 PM", "7:45 PM" },
        new[] { "11/1/26", "5:27 AM", "6:15 AM", "6:53 AM", "11:52 AM", "12:30 PM", "2:25 PM", "3:15 PM", "4:51 PM", "5:01 PM", "6:17 PM", "7:45 PM" },
        new[] { "11/2/26", "5:28 AM", "6:15 AM", "6:55 AM", "11:52 AM", "12:30 PM", "2:23 PM", "3:15 PM", "4:50 PM", "5:00 PM", "6:16 PM", "7:45 PM" },
        new[] { "11/3/26", "5:29 AM", "6:15 AM", "6:57 AM", "11:52 AM", "12:30 PM", "2:22 PM", "3:15 PM", "4:48 PM", "4:58 PM", "6:15 PM", "7:45 PM" },
        new[] { "11/4/26", "5:31 AM", "6:15 AM", "6:58 AM", "11:52 AM", "12:30 PM", "2:21 PM", "3:15 PM", "4:47 PM", "4:57 PM", "6:13 PM", "7:45 PM" },
        new[] { "11/5/26", "5:32 AM", "6:15 AM", "7:00 AM", "11:52 AM", "12:30 PM", "2:20 PM", "3:15 PM", "4:45 PM", "4:55 PM", "6:12 PM", "7:45 PM" },
        new[] { "11/6/26", "5:33 AM", "6:30 AM", "7:01 AM", "11:52 AM", "12:30 PM", "2:19 PM", "3:00 PM", "4:44 PM", "4:54 PM", "6:11 PM", "7:45 PM" },
        new[] { "11/7/26", "5:35 AM", "6:30 AM", "7:03 AM", "11:52 AM", "12:30 PM", "2:18 PM", "3:00 PM", "4:43 PM", "4:53 PM", "6:10 PM", "7:45 PM" },
        new[] { "11/8/26", "5:36 AM", "6:30 AM", "7:04 AM", "11:53 AM", "12:30 PM", "2:17 PM", "3:00 PM", "4:41 PM", "4:51 PM", "6:08 PM", "7:45 PM" },
        new[] { "11/9/26", "5:37 AM", "6:30 AM", "7:06 AM", "11:53 AM", "12:30 PM", "2:15 PM", "3:00 PM", "4:40 PM", "4:50 PM", "6:07 PM", "7:45 PM" },
        new[] { "11/10/26", "5:39 AM", "6:30 AM", "7:07 AM", "11:53 AM", "12:30 PM", "2:14 PM", "3:00 PM", "4:39 PM", "4:49 PM", "6:06 PM", "7:45 PM" },
        new[] { "11/11/26", "5:40 AM", "6:30 AM", "7:09 AM", "11:53 AM", "12:30 PM", "2:13 PM", "3:00 PM", "4:37 PM", "4:47 PM", "6:05 PM", "7:45 PM" },
        new[] { "11/12/26", "5:41 AM", "6:30 AM", "7:10 AM", "11:53 AM", "12:30 PM", "2:12 PM", "3:00 PM", "4:36 PM", "4:46 PM", "6:04 PM", "7:45 PM" },
    };

    /// <summary>
    /// Production rows around the spring change (prayer_times_db_2026-10-29_2.csv).
    ///
    /// Columns: day, fajr_athan, fajr_iqama, sunrise, dhuhr_athan, dhuhr_iqama,
    /// asr_athan, asr_iqama, maghrib_athan, maghrib_iqama, isha_athan, isha_iqama
    /// </summary>
    private static readonly string[][] SpringCsvRows =
    {
        new[] { "3/9/27", "5:09 AM", "6:00 AM", "6:34 AM", "12:19 PM", "12:30 PM", "3:27 PM", "4:30 PM", "6:06 PM", "6:16 PM", "7:30 PM", "7:45 PM" },
        new[] { "3/10/27", "5:07 AM", "6:00 AM", "6:32 AM", "12:19 PM", "12:30 PM", "3:28 PM", "4:30 PM", "6:08 PM", "6:18 PM", "7:32 PM", "7:45 PM" },
        new[] { "3/11/27", "5:05 AM", "6:00 AM", "6:30 AM", "12:19 PM", "12:30 PM", "3:29 PM", "4:30 PM", "6:09 PM", "6:19 PM", "7:33 PM", "7:45 PM" },
        new[] { "3/12/27", "5:03 AM", "5:45 AM", "6:28 AM", "12:18 PM", "12:30 PM", "3:30 PM", "4:30 PM", "6:11 PM", "6:21 PM", "7:35 PM", "7:45 PM" },
        new[] { "3/13/27", "5:01 AM", "5:45 AM", "6:26 AM", "12:18 PM", "12:30 PM", "3:31 PM", "4:30 PM", "6:12 PM", "6:22 PM", "7:36 PM", "7:46 PM" },
        new[] { "3/14/27", "5:59 AM", "6:45 AM", "7:24 AM", "1:18 PM", "1:30 PM", "4:32 PM", "5:30 PM", "7:14 PM", "7:24 PM", "8:38 PM", "8:48 PM" },
        new[] { "3/15/27", "5:57 AM", "6:45 AM", "7:22 AM", "1:18 PM", "1:30 PM", "4:33 PM", "5:30 PM", "7:15 PM", "7:25 PM", "8:39 PM", "8:49 PM" },
        new[] { "3/16/27", "5:55 AM", "6:45 AM", "7:20 AM", "1:17 PM", "1:30 PM", "4:34 PM", "5:30 PM", "7:17 PM", "7:27 PM", "8:41 PM", "8:51 PM" },
        new[] { "3/17/27", "5:53 AM", "6:45 AM", "7:18 AM", "1:17 PM", "1:30 PM", "4:35 PM", "5:30 PM", "7:18 PM", "7:28 PM", "8:42 PM", "8:52 PM" },
        new[] { "3/18/27", "5:51 AM", "6:45 AM", "7:16 AM", "1:17 PM", "1:30 PM", "4:36 PM", "5:30 PM", "7:20 PM", "7:30 PM", "8:44 PM", "8:54 PM" },
        new[] { "3/19/27", "5:48 AM", "6:30 AM", "7:14 AM", "1:16 PM", "1:30 PM", "4:36 PM", "5:45 PM", "7:21 PM", "7:31 PM", "8:46 PM", "8:56 PM" },
        new[] { "3/20/27", "5:46 AM", "6:30 AM", "7:12 AM", "1:16 PM", "1:30 PM", "4:37 PM", "5:45 PM", "7:22 PM", "7:32 PM", "8:47 PM", "8:57 PM" },
        new[] { "3/21/27", "5:44 AM", "6:30 AM", "7:10 AM", "1:16 PM", "1:30 PM", "4:38 PM", "5:45 PM", "7:24 PM", "7:34 PM", "8:49 PM", "8:59 PM" },
        new[] { "3/22/27", "5:42 AM", "6:30 AM", "7:08 AM", "1:16 PM", "1:30 PM", "4:39 PM", "5:45 PM", "7:25 PM", "7:35 PM", "8:50 PM", "9:00 PM" },
        new[] { "3/23/27", "5:40 AM", "6:30 AM", "7:06 AM", "1:15 PM", "1:30 PM", "4:40 PM", "5:45 PM", "7:27 PM", "7:37 PM", "8:52 PM", "9:02 PM" },
        new[] { "3/24/27", "5:37 AM", "6:30 AM", "7:04 AM", "1:15 PM", "1:30 PM", "4:41 PM", "5:45 PM", "7:28 PM", "7:38 PM", "8:54 PM", "9:04 PM" },
        new[] { "3/25/27", "5:35 AM", "6:30 AM", "7:02 AM", "1:15 PM", "1:30 PM", "4:41 PM", "5:45 PM", "7:30 PM", "7:40 PM", "8:55 PM", "9:05 PM" },
    };

    /// <summary>
    /// Build the ordered day list (same shape Builder produces) from CSV rows.
    ///
    /// Dates are anchored at midnight like Builder does; Athan times are
    /// parsed in the location timezone so all CSV times (which are after 2:00 AM,
    /// even on the transition days) resolve to the post-transition DST regime.
    /// </summary>
    private static List<DayData> DaysDataFromRows(string[][] rows)
    {
        var daysData = new List<DayData>(rows.Length);

        foreach (var row in rows)
        {
            var date = ParseCsvDay(row[0]);
            var dayKey = DayKey(date);

            var athan = new Dictionary<string, DateTime>
            {
                ["fajr"]    = ParseCsvTime(dayKey, row[1]),
                ["sunrise"] = ParseCsvTime(dayKey, row[3]),
                ["dhuhr"]   = ParseCsvTime(dayKey, row[4]),
                ["maghrib"] = ParseCsvTime(dayKey, row[8]),
                ["isha"]    = ParseCsvTime(dayKey, row[10]),
            };

            daysData.Add(new DayData(date, athan));
        }

        return daysData;
    }

    private static DateTime ParseCsvDay(string csvDay)
        => DateTime.ParseExact(csvDay, "M/d/yy", CultureInfo.InvariantCulture);

    private static DateTime ParseCsvTime(string dayKey, string csvTime)
        => DateTime.ParseExact(dayKey + " " + csvTime, "yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture);

    private static string DayKey(DateTime date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Partition days into weekly batches starting on Friday, mirroring
    /// Builder weekly processing with changeOn "Friday" from the API configuration.
    /// </summary>
    private static List<List<DayData>> WeekBatches(List<DayData> daysData)
    {
        var batches = new List<List<DayData>>();
        var currentBatch = new List<DayData>();

        foreach (var dayData in daysData)
        {
            var dayNumber = (int)dayData.Date.DayOfWeek;

            if (dayNumber == (int)DayOfWeek.Friday || currentBatch.Count == 0)
            {
                // Friday starts a new week
                if (currentBatch.Count > 0)
                {
                    batches.Add(currentBatch);
                }
                currentBatch = new List<DayData>();
            }

            currentBatch.Add(dayData);

            if (dayNumber == (int)DayOfWeek.Thursday)
            {
                // Thursday is the last day of the week
                batches.Add(currentBatch);
                currentBatch = new List<DayData>();
            }
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// Calculate Iqama times for a prayer across the given CSV rows, batching
    /// weekly rules per week like Builder does.
    /// </summary>
    /// <returns>Iqama times keyed by yyyy-MM-dd, formatted like the CSV (g:i A).</returns>
    private static Dictionary<string, string> CalculateIqamaTimes(
        string prayerName, string[][] rows, string? endPrayerName = null)
    {
        var results = new Dictionary<string, string>();

        foreach (var batch in WeekBatches(DaysDataFromRows(rows)))
        {
            var batchData = new Dictionary<int, DayData>();
            for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

            var batchResults = IqamaCalculator.CalculateIqama(
                batchData, prayerName, RuleFor(prayerName), endPrayerName);

            foreach (var (index, iqama) in batchResults)
                results[DayKey(batch[index].Date)] = iqama.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        return results;
    }

    /// <summary>
    /// Calculation rules mirroring iqamaCalculationRules from
    /// https://bothellmosque.org/wp-json/muslim-prayer-times/v1/salah-api
    /// </summary>
    private static PrayerCalculationRule RuleFor(string prayerName)
    {
        switch (prayerName)
        {
            case "fajr":
                // {"change":"weekly","roundMinutes":"15","earliest":"04:30","latest":"06:45",
                //  "beforeEndMinutes":30,"overrides":[{"condition":"ramadan","time":{
                //  "change":"daily","afterAthanMinutes":15,"roundMinutes":"1"}}]}
                return new PrayerCalculationRule(null, "weekly", 15, "04:30", "06:45", null, 30, new List<PrayerCalculationOverrideRule>
                {
                    new("ramadan", new PrayerCalculationRule(null, "daily", 1, null, null, 15, null, null)),
                });
            case "dhuhr":
                // {"static":"12:30","overrides":[{"condition":"daylightSavingsTime",
                //  "time":{"static":"13:30"}}]}
                return new PrayerCalculationRule("12:30", null, null, null, null, null, null, new List<PrayerCalculationOverrideRule>
                {
                    new("daylightSavingsTime", new PrayerCalculationRule("13:30")),
                });
            case "maghrib":
                // {"change":"daily","roundMinutes":"1","afterAthanMinutes":10,"overrides":[...]}
                return new PrayerCalculationRule(null, "daily", 1, null, null, 10, null, new List<PrayerCalculationOverrideRule>
                {
                    new("ramadan", new PrayerCalculationRule(null, "daily", 1, null, null, 10, null, null)),
                });
            case "isha":
                // {"change":"daily","roundMinutes":"1","earliest":"19:45","latest":"23:30",
                //  "afterAthanMinutes":10,"overrides":[...]}
                return new PrayerCalculationRule(null, "daily", 1, "19:45", "23:30", 10, null, new List<PrayerCalculationOverrideRule>
                {
                    new("ramadan", new PrayerCalculationRule(null, "daily", 1, null, null, 15, null, null)),
                });
            case "asr":
                // {"change":"weekly","roundMinutes":"15","afterAthanMinutes":0}
                return new PrayerCalculationRule(null, "weekly", 15, null, null, 0, null, null);
        }

        throw new ArgumentException($"No rule configured for prayer: {prayerName}");
    }

    /// <summary>
    /// Calculate the Asr Athan for a day using the API configuration:
    /// ISNA calculation method, middleOfTheNight high-latitude adjustment,
    /// Bothell coordinates, elevation 0.
    /// </summary>
    /// <param name="school">PrayerTimes.SchoolStandard or PrayerTimes.SchoolHanafi.</param>
    /// <param name="dayKey">Day in yyyy-MM-dd format.</param>
    private static DateTime CalculateAsrAthan(string school, string dayKey)
    {
        var cacheKey = school + dayKey;
        if (AsrAthanCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var calculator = new PrayerTimes(Method.MethodIsna, school);
        var times = calculator.GetTimes(
            ParseDayKeyMidnight(dayKey),
            Latitude,
            Longitude,
            0,
            PrayerTimes.LatitudeAdjustmentMethodMotn,
            null,
            PrayerTimes.TimeFormat24H,
            Timezone);

        var athan = ParseCsvTime(dayKey, ToCsvTime(times[PrayerTimes.Asr]));
        AsrAthanCache[cacheKey] = athan;

        return athan;
    }

    private static readonly Dictionary<string, DateTime> AsrAthanCache = new();

    private static DateTime ParseDayKeyMidnight(string dayKey)
        => DateTime.ParseExact(dayKey, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Convert a 24h "HH:mm" value to the CSV's "g:i A" style so it can be reused by ParseCsvTime.</summary>
    private static string ToCsvTime(string time24h)
    {
        var time = DateTime.ParseExact(time24h, "H:mm", CultureInfo.InvariantCulture);
        return time.ToString("h:mm tt", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Assert computed Iqama times match the production CSV column for every day.
    /// </summary>
    /// <param name="prayerName">Prayer under test.</param>
    /// <param name="iqamaColumn">CSV column index of the expected Iqama time.</param>
    /// <param name="rows">Rows of one of the CSV fixtures.</param>
    /// <param name="changeLabel">Change description used in failure messages (e.g. "fall").</param>
    /// <param name="endPrayerName">End prayer for beforeEndMinutes rules.</param>
    private static void AssertIqamaMatchesProductionCsv(
        string prayerName, int iqamaColumn, string[][] rows, string changeLabel,
        string? endPrayerName = null)
    {
        var computed = CalculateIqamaTimes(prayerName, rows, endPrayerName);

        foreach (var row in rows)
        {
            var dayKey = DayKey(ParseCsvDay(row[0]));
            var expected = row[iqamaColumn];

            Assert.True(computed.ContainsKey(dayKey), $"Missing computed Iqama for {dayKey}");
            Assert.True(
                string.Equals(expected, computed[dayKey], StringComparison.Ordinal),
                $"{prayerName} Iqama on {dayKey} (around the {changeLabel} DST change) does not match production: " +
                $"expected {expected}, got {computed[dayKey]}");
        }
    }

    /// <summary>
    /// Calculate Asr Iqama times across the given CSV range from the calculated
    /// Hanafi Asr Athan (asrIqamaCalculationMethod: "hanafi").
    ///
    /// The CSV ranges start mid-week, while production generated them with complete
    /// weekly batches. The first batch is therefore extended back to the previous
    /// Friday with calculated Athan times so the weekly winner matches production
    /// (the CSV has no expected values for those days).
    /// </summary>
    /// <returns>Iqama times keyed by yyyy-MM-dd, formatted like the CSV (g:i A).</returns>
    private static Dictionary<string, string> CalculateAsrIqamaTimes(string[][] rows)
    {
        var firstDay = ParseCsvDay(rows[0][0]);
        var lastDay = ParseCsvDay(rows[^1][0]);

        var start = firstDay;
        if (start.DayOfWeek != DayOfWeek.Friday)
        {
            var daysSinceFriday = ((int)start.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
            start = start.AddDays(-daysSinceFriday);
        }

        var daysData = new List<DayData>();
        for (var current = start; current <= lastDay; current = current.AddDays(1))
        {
            var dayKey = DayKey(current);
            daysData.Add(new DayData(current, new Dictionary<string, DateTime>
            {
                ["asr"] = CalculateAsrAthan(PrayerTimes.SchoolHanafi, dayKey),
            }));
        }

        var results = new Dictionary<string, string>();
        foreach (var batch in WeekBatches(daysData))
        {
            var batchData = new Dictionary<int, DayData>();
            for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

            var batchResults = IqamaCalculator.CalculateIqama(batchData, "asr", RuleFor("asr"));

            foreach (var (index, iqama) in batchResults)
                results[DayKey(batch[index].Date)] = iqama.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        return results;
    }

    /// <summary>
    /// Assert the calculated standard Asr Athan matches the CSV's asr_athan column
    /// for every day. This validates the PrayerTimes engine (ISNA,
    /// middleOfTheNight, Bothell coordinates) against production, including the
    /// one-hour wall-clock shift across the DST change, and is what makes the
    /// Hanafi Athan times used for the Asr Iqama trustworthy.
    /// </summary>
    private static void AssertStandardAsrAthanMatchesProductionCsv(string[][] rows, string changeLabel)
    {
        foreach (var row in rows)
        {
            var dayKey = DayKey(ParseCsvDay(row[0]));

            var computed = CalculateAsrAthan(PrayerTimes.SchoolStandard, dayKey)
                .ToString("h:mm tt", CultureInfo.InvariantCulture);

            Assert.True(
                string.Equals(row[6], computed, StringComparison.Ordinal),
                $"Standard Asr Athan on {dayKey} (around the {changeLabel} DST change) does not match production: " +
                $"expected {row[6]}, got {computed}");
        }
    }

    // -----------------------------------------------------------------------
    // Sanity checks
    // -----------------------------------------------------------------------

    /// <summary>
    /// The test process must run in America/Los_Angeles: the .NET port resolves DST
    /// through TimeZoneInfo.Local (PHP used each DateTime's own timezone). Run with
    /// TZ=America/Los_Angeles or rely on the TestEnvironment module initializer.
    /// </summary>
    [Fact]
    public void TestProcessRunsInLosAngelesTimezone()
    {
        Assert.Equal(Timezone, TimeZoneInfo.Local.Id);
    }

    /// <summary>
    /// Sanity check: the fall fixture actually spans the fall DST transition.
    ///
    /// DST ends on Nov 1, 2026 at 2:00 AM. Midnight dates are still DST on the
    /// transition day, while all Athan times on Nov 1 (after 2:00 AM) are standard.
    /// </summary>
    [Fact]
    public void TestFixtureSpansFallDstTransition()
    {
        var daysData = DaysDataFromRows(FallCsvRows);
        var byKey = daysData.ToDictionary(d => DayKey(d.Date));

        Assert.True(TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2026-10-31"].Date), "Oct 31 should be DST");
        Assert.True(TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2026-11-01"].Date), "Nov 1 midnight is still DST");
        Assert.False(TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2026-11-02"].Date), "Nov 2 should be standard time");
        Assert.False(
            TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2026-11-01"].Athan["dhuhr"]),
            "Nov 1 Dhuhr Athan (11:52 AM) is after the 2 AM fall-back, so it is standard time");
        Assert.True(
            TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2026-10-31"].Athan["dhuhr"]),
            "Oct 31 Dhuhr Athan is DST");
    }

    /// <summary>
    /// Sanity check: the spring fixture actually spans the spring DST transition.
    ///
    /// DST starts on Mar 14, 2027 at 2:00 AM. Midnight dates are still standard on
    /// the transition day, while all Athan times on Mar 14 (after 2:00 AM) are DST.
    /// </summary>
    [Fact]
    public void TestFixtureSpansSpringDstTransition()
    {
        var daysData = DaysDataFromRows(SpringCsvRows);
        var byKey = daysData.ToDictionary(d => DayKey(d.Date));

        Assert.False(TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2027-03-13"].Date), "Mar 13 should be standard time");
        Assert.False(TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2027-03-14"].Date), "Mar 14 midnight is still standard time");
        Assert.True(TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2027-03-15"].Date), "Mar 15 should be DST");
        Assert.True(
            TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2027-03-14"].Athan["dhuhr"]),
            "Mar 14 Dhuhr Athan (1:18 PM) is after the 2 AM spring-forward, so it is DST");
        Assert.False(
            TimeZoneInfo.Local.IsDaylightSavingTime(byKey["2027-03-13"].Athan["dhuhr"]),
            "Mar 13 Dhuhr Athan is standard time");
    }

    // -----------------------------------------------------------------------
    // Full-range production comparison
    // -----------------------------------------------------------------------

    /// <summary>
    /// Dhuhr Iqama uses the DST override (1:30 PM) before the fall change and the
    /// base static time (12:30 PM) from the transition day onward. The override is
    /// resolved at the Athan time, so Nov 1 (Athan 11:52 AM, after the 2 AM
    /// fall-back) already uses standard time - matching production.
    /// </summary>
    [Fact]
    public void TestDhuhrIqamaMatchesProductionCsvAcrossFallChange()
    {
        AssertIqamaMatchesProductionCsv("dhuhr", 5, FallCsvRows, "fall");
    }

    /// <summary>
    /// Fajr Iqama (weekly, 30 min before sunrise) across the fall change: the week
    /// spanning the transition renders 6:45 AM on DST days (clamped by the 6:45
    /// latest constraint) and 6:15 AM on standard days - matching production.
    /// </summary>
    [Fact]
    public void TestFajrIqamaMatchesProductionCsvAcrossFallChange()
    {
        AssertIqamaMatchesProductionCsv("fajr", 2, FallCsvRows, "fall", "sunrise");
    }

    /// <summary>
    /// Maghrib Iqama (daily, Athan + 10 min) across the fall change: wall-clock
    /// times shift one hour back with the clocks - matching production.
    /// </summary>
    [Fact]
    public void TestMaghribIqamaMatchesProductionCsvAcrossFallChange()
    {
        AssertIqamaMatchesProductionCsv("maghrib", 9, FallCsvRows, "fall");
    }

    /// <summary>
    /// Isha Iqama (daily, Athan + 10 min, clamped to earliest 7:45 PM) across the
    /// fall change - matching production.
    /// </summary>
    [Fact]
    public void TestIshaIqamaMatchesProductionCsvAcrossFallChange()
    {
        AssertIqamaMatchesProductionCsv("isha", 11, FallCsvRows, "fall");
    }

    /// <summary>
    /// Dhuhr Iqama uses the base static time (12:30 PM) before the spring change
    /// and the DST override (1:30 PM) from the transition day onward. The override
    /// is resolved at the Athan time, so Mar 14 (Athan 1:18 PM, after the 2 AM
    /// spring-forward) already uses DST time - matching production.
    /// </summary>
    [Fact]
    public void TestDhuhrIqamaMatchesProductionCsvAcrossSpringChange()
    {
        AssertIqamaMatchesProductionCsv("dhuhr", 5, SpringCsvRows, "spring");
    }

    /// <summary>
    /// Fajr Iqama (weekly, 30 min before sunrise) across the spring change: the
    /// week spanning the transition renders 5:45 AM on standard days and 6:45 AM
    /// on DST days (the same absolute instant) - matching production.
    /// </summary>
    [Fact]
    public void TestFajrIqamaMatchesProductionCsvAcrossSpringChange()
    {
        AssertIqamaMatchesProductionCsv("fajr", 2, SpringCsvRows, "spring", "sunrise");
    }

    /// <summary>
    /// Maghrib Iqama (daily, Athan + 10 min) across the spring change: wall-clock
    /// times shift one hour forward with the clocks - matching production.
    /// </summary>
    [Fact]
    public void TestMaghribIqamaMatchesProductionCsvAcrossSpringChange()
    {
        AssertIqamaMatchesProductionCsv("maghrib", 9, SpringCsvRows, "spring");
    }

    /// <summary>
    /// Isha Iqama (daily, Athan + 10 min, clamped to earliest 7:45 PM) across the
    /// spring change - matching production.
    /// </summary>
    [Fact]
    public void TestIshaIqamaMatchesProductionCsvAcrossSpringChange()
    {
        AssertIqamaMatchesProductionCsv("isha", 11, SpringCsvRows, "spring");
    }

    // -----------------------------------------------------------------------
    // Focused boundary checks
    // -----------------------------------------------------------------------

    /// <summary>
    /// Focused check of the weekly Fajr behavior within the single week that spans
    /// the fall change (Fri Oct 30 - Thu Nov 5): the Iqama is the same absolute
    /// instant on both sides of the boundary, so DST days get 6:45 AM (clamped by
    /// the latest constraint) while standard days get 6:15 AM.
    /// </summary>
    [Fact]
    public void TestFajrWeeklyIqamaChangesAtDstBoundaryWithinSameWeekFallChange()
    {
        var batch = DaysDataFromRows(FallCsvRows.Skip(1).Take(7).ToArray()); // 2026-10-30 .. 2026-11-05

        Assert.Equal(7, batch.Count);
        Assert.Equal(DayOfWeek.Friday, batch[0].Date.DayOfWeek);
        Assert.Equal(DayOfWeek.Thursday, batch[6].Date.DayOfWeek);

        var batchData = new Dictionary<int, DayData>();
        for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

        var results = IqamaCalculator.CalculateIqama(batchData, "fajr", RuleFor("fajr"), "sunrise");

        Assert.Equal("06:45", results[0].ToString("HH:mm")); // Oct 30 (DST)
        Assert.Equal("06:45", results[1].ToString("HH:mm")); // Oct 31 (DST)
        Assert.Equal("06:15", results[2].ToString("HH:mm")); // Nov 1 (fall-back day, standard)
        Assert.Equal("06:15", results[3].ToString("HH:mm")); // Nov 2 (standard)
        Assert.Equal("06:15", results[6].ToString("HH:mm")); // Nov 5 (standard)
    }

    /// <summary>
    /// Focused check of the weekly Fajr behavior within the single week that spans
    /// the spring change (Fri Mar 12 - Thu Mar 18): the Iqama is the same absolute
    /// instant on both sides of the boundary, so standard days get 5:45 AM while
    /// DST days get 6:45 AM.
    /// </summary>
    [Fact]
    public void TestFajrWeeklyIqamaChangesAtDstBoundaryWithinSameWeekSpringChange()
    {
        var batch = DaysDataFromRows(SpringCsvRows.Skip(3).Take(7).ToArray()); // 2027-03-12 .. 2027-03-18

        Assert.Equal(7, batch.Count);
        Assert.Equal(DayOfWeek.Friday, batch[0].Date.DayOfWeek);
        Assert.Equal(DayOfWeek.Thursday, batch[6].Date.DayOfWeek);

        var batchData = new Dictionary<int, DayData>();
        for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

        var results = IqamaCalculator.CalculateIqama(batchData, "fajr", RuleFor("fajr"), "sunrise");

        Assert.Equal("05:45", results[0].ToString("HH:mm")); // Mar 12 (standard)
        Assert.Equal("05:45", results[1].ToString("HH:mm")); // Mar 13 (standard)
        Assert.Equal("06:45", results[2].ToString("HH:mm")); // Mar 14 (spring-forward day, DST)
        Assert.Equal("06:45", results[3].ToString("HH:mm")); // Mar 15 (DST)
        Assert.Equal("06:45", results[6].ToString("HH:mm")); // Mar 18 (DST)
    }

    /// <summary>
    /// Focused check of the Dhuhr DST override on the fall change day itself:
    /// Oct 31 (DST) -> 1:30 PM, Nov 1 (transition day, Athan after fall-back) ->
    /// 12:30 PM, Nov 2 (standard) -> 12:30 PM.
    /// </summary>
    [Fact]
    public void TestDhuhrIqamaUsesStandardTimeOnFallChangeDay()
    {
        var batch = DaysDataFromRows(FallCsvRows.Skip(2).Take(3).ToArray()); // 2026-10-31, 2026-11-01, 2026-11-02

        var batchData = new Dictionary<int, DayData>();
        for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

        var results = IqamaCalculator.CalculateIqama(batchData, "dhuhr", RuleFor("dhuhr"));

        Assert.Equal("13:30", results[0].ToString("HH:mm")); // Oct 31 (DST) should use the DST override
        Assert.Equal("12:30", results[1].ToString("HH:mm")); // Nov 1 Athan is after the fall-back, should use standard time
        Assert.Equal("12:30", results[2].ToString("HH:mm")); // Nov 2 (standard) should use standard time
    }

    /// <summary>
    /// Focused check of the Dhuhr DST override on the spring change day itself:
    /// Mar 13 (standard) -> 12:30 PM, Mar 14 (transition day, Athan after
    /// spring-forward) -> 1:30 PM, Mar 15 (DST) -> 1:30 PM.
    /// </summary>
    [Fact]
    public void TestDhuhrIqamaUsesDstTimeOnSpringChangeDay()
    {
        var batch = DaysDataFromRows(SpringCsvRows.Skip(4).Take(3).ToArray()); // 2027-03-13, 2027-03-14, 2027-03-15

        var batchData = new Dictionary<int, DayData>();
        for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

        var results = IqamaCalculator.CalculateIqama(batchData, "dhuhr", RuleFor("dhuhr"));

        Assert.Equal("12:30", results[0].ToString("HH:mm")); // Mar 13 (standard) should use standard time
        Assert.Equal("13:30", results[1].ToString("HH:mm")); // Mar 14 Athan is after the spring-forward, should use the DST override
        Assert.Equal("13:30", results[2].ToString("HH:mm")); // Mar 15 (DST) should use the DST override
    }

    // -----------------------------------------------------------------------
    // Asr (driven by the calculated Hanafi Asr Athan)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The calculated standard Asr Athan must match the CSV's asr_athan column
    /// exactly for the fall change range.
    /// </summary>
    [Fact]
    public void TestStandardAsrAthanMatchesProductionCsvAcrossFallChange()
    {
        AssertStandardAsrAthanMatchesProductionCsv(FallCsvRows, "fall");
    }

    /// <summary>
    /// The calculated standard Asr Athan must match the CSV's asr_athan column
    /// exactly for the spring change range.
    /// </summary>
    [Fact]
    public void TestStandardAsrAthanMatchesProductionCsvAcrossSpringChange()
    {
        AssertStandardAsrAthanMatchesProductionCsv(SpringCsvRows, "spring");
    }

    /// <summary>
    /// Asr Iqama (weekly, round 15 min, Athan + 0) driven by the calculated Hanafi
    /// Asr Athan across the fall change: 4:30 PM on Oct 29 (winner of the full week
    /// starting Oct 23), 4:15 PM on DST days vs 3:15 PM on standard days within the
    /// week spanning the transition (the same absolute instant), then 3:00 PM after
    /// the next weekly change - all matching production.
    /// </summary>
    [Fact]
    public void TestAsrIqamaMatchesProductionCsvAcrossFallChange()
    {
        var computed = CalculateAsrIqamaTimes(FallCsvRows);

        foreach (var row in FallCsvRows)
        {
            var dayKey = DayKey(ParseCsvDay(row[0]));

            Assert.True(computed.ContainsKey(dayKey), $"Missing computed Asr Iqama for {dayKey}");
            Assert.True(
                string.Equals(row[7], computed[dayKey], StringComparison.Ordinal),
                $"Asr Iqama on {dayKey} (around the fall DST change) does not match production: " +
                $"expected {row[7]}, got {computed[dayKey]}");
        }
    }

    /// <summary>
    /// Asr Iqama (weekly, round 15 min, Athan + 0) driven by the calculated Hanafi
    /// Asr Athan across the spring change: 4:30 PM while the weekly winner is on
    /// standard days, then 4:30 PM vs 5:30 PM within the week spanning the
    /// transition (the same absolute instant), then 5:45 PM after the next weekly
    /// change - all matching production.
    /// </summary>
    [Fact]
    public void TestAsrIqamaMatchesProductionCsvAcrossSpringChange()
    {
        var computed = CalculateAsrIqamaTimes(SpringCsvRows);

        foreach (var row in SpringCsvRows)
        {
            var dayKey = DayKey(ParseCsvDay(row[0]));

            Assert.True(computed.ContainsKey(dayKey), $"Missing computed Asr Iqama for {dayKey}");
            Assert.True(
                string.Equals(row[7], computed[dayKey], StringComparison.Ordinal),
                $"Asr Iqama on {dayKey} (around the spring DST change) does not match production: " +
                $"expected {row[7]}, got {computed[dayKey]}");
        }
    }

    /// <summary>
    /// Focused check of the weekly Asr behavior within the single week that spans
    /// the fall change (Fri Oct 30 - Thu Nov 5): the Iqama is the same absolute
    /// instant on both sides of the boundary, so DST days get 4:15 PM while
    /// standard days get 3:15 PM.
    /// </summary>
    [Fact]
    public void TestAsrWeeklyIqamaChangesAtDstBoundaryWithinSameWeekFallChange()
    {
        var results = IqamaCalculator.CalculateIqama(
            HanafiAsrBatch("2026-10-30", "2026-11-05"),
            "asr",
            RuleFor("asr"));

        Assert.Equal("16:15", results[0].ToString("HH:mm")); // Oct 30 (DST)
        Assert.Equal("16:15", results[1].ToString("HH:mm")); // Oct 31 (DST)
        Assert.Equal("15:15", results[2].ToString("HH:mm")); // Nov 1 (fall-back day, standard)
        Assert.Equal("15:15", results[3].ToString("HH:mm")); // Nov 2 (standard)
        Assert.Equal("15:15", results[6].ToString("HH:mm")); // Nov 5 (standard)
    }

    /// <summary>
    /// Focused check of the weekly Asr behavior within the single week that spans
    /// the spring change (Fri Mar 12 - Thu Mar 18): the Iqama is the same absolute
    /// instant on both sides of the boundary, so standard days get 4:30 PM while
    /// DST days get 5:30 PM.
    /// </summary>
    [Fact]
    public void TestAsrWeeklyIqamaChangesAtDstBoundaryWithinSameWeekSpringChange()
    {
        var batch = HanafiAsrBatch("2027-03-12", "2027-03-18");

        Assert.Equal(DayOfWeek.Friday, batch[0].Date.DayOfWeek);
        Assert.Equal(DayOfWeek.Thursday, batch[6].Date.DayOfWeek);

        var batchData = new Dictionary<int, DayData>();
        for (var i = 0; i < batch.Count; i++) batchData[i] = batch[i];

        var results = IqamaCalculator.CalculateIqama(batchData, "asr", RuleFor("asr"));

        Assert.Equal("16:30", results[0].ToString("HH:mm")); // Mar 12 (standard)
        Assert.Equal("16:30", results[1].ToString("HH:mm")); // Mar 13 (standard)
        Assert.Equal("17:30", results[2].ToString("HH:mm")); // Mar 14 (spring-forward day, DST)
        Assert.Equal("17:30", results[3].ToString("HH:mm")); // Mar 15 (DST)
        Assert.Equal("17:30", results[6].ToString("HH:mm")); // Mar 18 (DST)
    }

    /// <summary>
    /// Build a weekly batch of days with calculated Hanafi Asr Athan for the
    /// given inclusive date range (used by the focused Asr boundary tests).
    /// </summary>
    private static Dictionary<int, DayData> HanafiAsrBatch(string startDay, string endDay)
    {
        var start = ParseDayKeyMidnight(startDay);
        var end = ParseDayKeyMidnight(endDay);

        var batch = new Dictionary<int, DayData>();
        var i = 0;
        for (var current = start; current <= end; current = current.AddDays(1))
        {
            batch[i++] = new DayData(current, new Dictionary<string, DateTime>
            {
                ["asr"] = CalculateAsrAthan(PrayerTimes.SchoolHanafi, DayKey(current)),
            });
        }

        return batch;
    }
}
