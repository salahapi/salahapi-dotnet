namespace SalahAPI.Calculations;

/// <summary>
/// Helper functions for time manipulation, rounding, and DST handling.
/// </summary>
public static class TimeHelpers
{
    /// <summary>Convert a DateTime to minutes since midnight, subtracting 60 minutes when DST is active.</summary>
    public static int TimeToMinutes(DateTime time)
    {
        int total = time.Hour * 60 + time.Minute;
        if (IsDst(time)) total -= 60;
        return total;
    }

    /// <summary>Return a clone of <paramref name="time"/> with one hour subtracted when DST is active.</summary>
    public static DateTime NormalizeTimeForDst(DateTime time)
    {
        return IsDst(time) ? time.AddHours(-1) : time;
    }

    /// <summary>Return a clone of <paramref name="time"/> with one hour added when DST is active.</summary>
    public static DateTime DenormalizeTimeForDst(DateTime time)
    {
        return IsDst(time) ? time.AddHours(1) : time;
    }

    /// <summary>
    /// Normalize all athan times in <paramref name="daysData"/> for DST.
    /// Returns a new dictionary; the originals are not modified.
    /// </summary>
    public static Dictionary<int, DayData> NormalizeTimesForDst(Dictionary<int, DayData> daysData)
    {
        var result = new Dictionary<int, DayData>(daysData.Count);
        foreach (var (idx, day) in daysData)
        {
            var normalizedAthan = day.Athan.ToDictionary(
                kv => kv.Key,
                kv => NormalizeTimeForDst(kv.Value));

            result[idx] = new DayData(day.Date, normalizedAthan);
        }
        return result;
    }

    /// <summary>Round <paramref name="time"/> down to the nearest <paramref name="roundingMinutes"/> boundary.</summary>
    public static DateTime RoundDown(DateTime time, int roundingMinutes = 1)
    {
        if (roundingMinutes <= 1) return time;

        int totalSecs = time.Minute * 60 + time.Second;
        int roundingSecs = roundingMinutes * 60;
        int roundedSecs = (totalSecs / roundingSecs) * roundingSecs;

        int newMinutes = roundedSecs / 60;
        int hour = time.Hour + newMinutes / 60;
        newMinutes %= 60;

        return new DateTime(time.Year, time.Month, time.Day, hour, newMinutes, 0, time.Kind);
    }

    /// <summary>Round <paramref name="time"/> up to the nearest <paramref name="roundingMinutes"/> boundary.</summary>
    public static DateTime RoundUp(DateTime time, int roundingMinutes = 1)
    {
        if (roundingMinutes <= 1) return time;

        int totalSecs = time.Minute * 60 + time.Second;
        int roundingSecs = roundingMinutes * 60;

        int roundedSecs = (totalSecs % roundingSecs == 0 && time.Second == 0)
            ? totalSecs
            : (int)(Math.Ceiling((double)totalSecs / roundingSecs) * roundingSecs);

        int newMinutes = roundedSecs / 60;
        int hour = time.Hour + newMinutes / 60;
        newMinutes %= 60;

        return new DateTime(time.Year, time.Month, time.Day, hour, newMinutes, 0, time.Kind);
    }

    /// <summary>
    /// Return a new DateTime that has the date of <paramref name="date"/> but the time from
    /// <paramref name="timeString"/> (expected "HH:mm" or "H:mm").
    /// </summary>
    public static DateTime ParseTimeString(DateTime date, string timeString)
    {
        var parts = timeString.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid time format: {timeString}", nameof(timeString));

        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        return new DateTime(date.Year, date.Month, date.Day, hours, minutes, 0, date.Kind);
    }

    /// <summary>Convert Western/ASCII digits to Eastern Arabic numerals (٠١٢…).</summary>
    public static string ConvertToArabicNumerals(string number)
    {
        const string western = "0123456789";
        const string arabic  = "٠١٢٣٤٥٦٧٨٩";
        var sb = new System.Text.StringBuilder(number.Length);
        foreach (char c in number)
        {
            int idx = western.IndexOf(c);
            sb.Append(idx >= 0 ? arabic[idx] : c);
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Returns true if the supplied DateTime appears to be during daylight saving time.
    /// This mirrors the PHP <c>$date->format('I') === '1'</c> check.
    /// </summary>
    private static bool IsDst(DateTime time)
    {
        // DateTimeKind.Unspecified / Local: ask the local timezone.
        // DateTimeKind.Utc: DST is never active in UTC.
        if (time.Kind == DateTimeKind.Utc) return false;
        return TimeZoneInfo.Local.IsDaylightSavingTime(time);
    }
}

// ---------------------------------------------------------------------------
// Shared data carrier used by Builder / IqamaCalculator
// ---------------------------------------------------------------------------

/// <summary>Holds the athan times for a single day.</summary>
public sealed class DayData
{
    public DateTime Date { get; }
    public Dictionary<string, DateTime> Athan { get; }

    public DayData(DateTime date, Dictionary<string, DateTime> athan)
    {
        Date = date;
        Athan = athan;
    }
}
