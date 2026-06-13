using TimeZoneConverter;

namespace SalahAPI.Calculations;

/// <summary>
/// Prayer Times Builder.
///
/// Builds prayer times for a date range based on a <see cref="SalahAPI.Location"/> and a
/// <see cref="SalahAPI.CalculationMethod"/>, optionally including Iqama times derived from
/// <see cref="SalahAPI.IqamaCalculationRules"/>.
/// </summary>
public class Builder
{
    private static readonly Dictionary<string, int> DayOfWeekMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sunday"]    = 0, ["Monday"]  = 1, ["Tuesday"]  = 2,
        ["Wednesday"] = 3, ["Thursday"]= 4, ["Friday"]   = 5, ["Saturday"] = 6
    };

    private readonly PrayerTimes _prayerTimes;
    private readonly Location _location;
    private readonly CalculationMethod _calculationMethod;
    private readonly int _elevation;
    private readonly bool _includeAsrMethods;
    private readonly PrayerTimes? _asrStandardCalculator;
    private readonly PrayerTimes? _asrHanafiCalculator;

    /// <summary>
    /// Initialise the builder.
    /// </summary>
    /// <param name="location">Geographic location and timezone.</param>
    /// <param name="calculationMethod">Calculation method parameters.</param>
    /// <param name="elevation">Elevation above sea level in metres (default: 0).</param>
    /// <param name="includeAsrMethods">
    /// When <c>true</c>, the generated output includes the optional
    /// <c>asr_athan_standard</c> and <c>asr_athan_hanafi</c> columns.
    /// </param>
    public Builder(
        Location location,
        CalculationMethod calculationMethod,
        int elevation = 0,
        bool includeAsrMethods = false)
    {
        _location = location;
        _calculationMethod = calculationMethod;
        _elevation = elevation;
        _includeAsrMethods = includeAsrMethods;

        _prayerTimes = new PrayerTimes(
            calculationMethod.Name,
            NormalizeAsrSchool(calculationMethod.AsrCalculationMethod));

        if (_includeAsrMethods)
        {
            _asrStandardCalculator = new PrayerTimes(calculationMethod.Name, PrayerTimes.SchoolStandard);
            _asrHanafiCalculator   = new PrayerTimes(calculationMethod.Name, PrayerTimes.SchoolHanafi);
        }
    }

    // ---------------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Build prayer times for the given date range and return raw rows (header + data).
    /// The first row is a string[] header.
    /// </summary>
    public List<string[]> Build(DateTime startDate, DateTime endDate)
    {
        var tz = TZConvert.GetTimeZoneInfo(_location.Timezone);

        startDate = TimeZoneInfo.ConvertTime(startDate, tz);
        endDate   = TimeZoneInfo.ConvertTime(endDate, tz);

        int daysToGenerate = (int)(endDate.Date - startDate.Date).TotalDays + 1;

        var allDaysData = new Dictionary<int, DayData>(daysToGenerate);
        var current = startDate.Date;

        for (int i = 0; i < daysToGenerate; i++, current = current.AddDays(1))
        {
            var localDate = new DateTime(current.Year, current.Month, current.Day,
                                         0, 0, 0, DateTimeKind.Unspecified);

            var times = _prayerTimes.GetTimes(
                localDate,
                _location.Latitude,
                _location.Longitude,
                _elevation,
                NormalizeHighLatitudeAdjustment(_calculationMethod.HighLatitudeAdjustment),
                null,
                PrayerTimes.TimeFormat24H,
                ianaTimezone: _location.Timezone);

            // Store timezone in the prayer times instance before computing
            // (GetTimes already does this; we just set timezone on localDate via the builder)
            string datePrefix = $"{current:yyyy-MM-dd} ";

            var athan = new Dictionary<string, DateTime>
            {
                ["fajr"]    = ParseLocalDateTime(datePrefix + times[PrayerTimes.Fajr],    tz),
                ["sunrise"] = ParseLocalDateTime(datePrefix + times[PrayerTimes.Sunrise],  tz),
                ["dhuhr"]   = ParseLocalDateTime(datePrefix + times[PrayerTimes.Zhuhr],    tz),
                ["asr"]     = ParseLocalDateTime(datePrefix + times[PrayerTimes.Asr],      tz),
                ["maghrib"] = ParseLocalDateTime(datePrefix + times[PrayerTimes.Maghrib],  tz),
                ["isha"]    = ParseLocalDateTime(datePrefix + times[PrayerTimes.Isha],     tz),
            };

            if (_includeAsrMethods && _asrStandardCalculator is not null && _asrHanafiCalculator is not null)
            {
                var stdTimes = _asrStandardCalculator.GetTimes(localDate, _location.Latitude, _location.Longitude,
                    _elevation, NormalizeHighLatitudeAdjustment(_calculationMethod.HighLatitudeAdjustment),
                    null, PrayerTimes.TimeFormat24H, ianaTimezone: _location.Timezone);

                var hanTimes = _asrHanafiCalculator.GetTimes(localDate, _location.Latitude, _location.Longitude,
                    _elevation, NormalizeHighLatitudeAdjustment(_calculationMethod.HighLatitudeAdjustment),
                    null, PrayerTimes.TimeFormat24H, ianaTimezone: _location.Timezone);

                athan["asr_standard"] = ParseLocalDateTime(datePrefix + stdTimes[PrayerTimes.Asr], tz);
                athan["asr_hanafi"]   = ParseLocalDateTime(datePrefix + hanTimes[PrayerTimes.Asr], tz);
            }

            allDaysData[i] = new DayData(current, athan);
        }

        // Build header
        var header = new List<string>
        {
            "day", "fajr_athan", "fajr_iqama", "sunrise",
            "dhuhr_athan", "dhuhr_iqama", "asr_athan", "asr_iqama",
            "maghrib_athan", "maghrib_iqama", "isha_athan", "isha_iqama"
        };
        if (_includeAsrMethods) { header.Add("asr_athan_standard"); header.Add("asr_athan_hanafi"); }

        var csvData = new List<string[]> { header.ToArray() };

        bool isWeekly = _calculationMethod.IqamaCalculationRules?.ChangeOn is not null;
        var dataRows  = isWeekly ? ProcessWeekly(allDaysData) : CalculateWeekIqama(allDaysData);

        csvData.AddRange(dataRows);
        return csvData;
    }

    /// <summary>Build and return CSV content as a string.</summary>
    public string BuildCsv(DateTime startDate, DateTime endDate)
    {
        var rows = Build(startDate, endDate);
        return string.Join("\n", rows.Select(r => string.Join(",", r)));
    }

    /// <summary>Build and return an array of associative dictionaries (one per day).</summary>
    public List<Dictionary<string, string?>> BuildAssociative(DateTime startDate, DateTime endDate)
    {
        var rows = Build(startDate, endDate);
        var header = rows[0];

        return rows.Skip(1).Select(row =>
        {
            var dict = new Dictionary<string, string?>(header.Length);
            for (int i = 0; i < header.Length; i++)
                dict[header[i]] = i < row.Length ? row[i] : null;
            return dict;
        }).ToList();
    }

    // Convenience overloads that accept string dates (ISO 8601 format).
    public List<string[]> Build(string startDate, string endDate)
        => Build(DateTime.Parse(startDate), DateTime.Parse(endDate));

    public string BuildCsv(string startDate, string endDate)
        => BuildCsv(DateTime.Parse(startDate), DateTime.Parse(endDate));

    public List<Dictionary<string, string?>> BuildAssociative(string startDate, string endDate)
        => BuildAssociative(DateTime.Parse(startDate), DateTime.Parse(endDate));

    // ---------------------------------------------------------------------------
    // Weekly processing
    // ---------------------------------------------------------------------------

    private List<string[]> ProcessWeekly(Dictionary<int, DayData> allDaysData)
    {
        var csvRows = new List<string[]>();
        var weekDays = new Dictionary<int, DayData>();
        DateTime? currentWeekStart = null;
        int totalDays = allDaysData.Count;
        int processed = 0;

        string changeOnDay = _calculationMethod.IqamaCalculationRules?.ChangeOn ?? "Friday";
        if (!DayOfWeekMap.TryGetValue(changeOnDay, out int changeOnDayNumber))
            changeOnDayNumber = 5; // Friday

        foreach (var (dayIndex, dayData) in allDaysData.OrderBy(kv => kv.Key))
        {
            int currentDayNumber = (int)dayData.Date.DayOfWeek;

            if (currentDayNumber == changeOnDayNumber || currentWeekStart is null)
            {
                if (currentWeekStart is not null && weekDays.Count > 0)
                {
                    csvRows.AddRange(CalculateWeekIqama(weekDays));
                    weekDays = new Dictionary<int, DayData>();
                }
                currentWeekStart = dayData.Date;
            }

            weekDays[dayIndex] = dayData;

            bool isEndOfWeek = IsEndOfWeek(currentDayNumber, changeOnDayNumber);
            bool isLastDay   = ++processed >= totalDays;

            if (isEndOfWeek || isLastDay)
            {
                csvRows.AddRange(CalculateWeekIqama(weekDays));
                weekDays = new Dictionary<int, DayData>();
                currentWeekStart = null;
            }
        }

        if (weekDays.Count > 0)
            csvRows.AddRange(CalculateWeekIqama(weekDays));

        return csvRows;
    }

    // ---------------------------------------------------------------------------
    // Per-week (or per-batch) Iqama calculation
    // ---------------------------------------------------------------------------

    private List<string[]> CalculateWeekIqama(Dictionary<int, DayData> weekDays)
    {
        if (weekDays.Count == 0) return [];

        var iqamaRules = _calculationMethod.IqamaCalculationRules;

        var fajrIqama    = IqamaCalculator.CalculateIqama(weekDays, "fajr",    iqamaRules?.Fajr,    "sunrise");
        var dhuhrIqama   = IqamaCalculator.CalculateIqama(weekDays, "dhuhr",   iqamaRules?.Dhuhr);
        var asrIqama     = IqamaCalculator.CalculateIqama(weekDays, "asr",     iqamaRules?.Asr);
        var maghribIqama = IqamaCalculator.CalculateIqama(weekDays, "maghrib", iqamaRules?.Maghrib);
        var ishaIqama    = IqamaCalculator.CalculateIqama(weekDays, "isha",    iqamaRules?.Isha);

        var rows = new List<string[]>();

        foreach (var (dayIndex, dayData) in weekDays.OrderBy(kv => kv.Key))
        {
            var athan = dayData.Athan;

            var row = new List<string>
            {
                dayData.Date.ToString("yyyy-MM-dd"),
                athan["fajr"].ToString("HH:mm"),
                fajrIqama.TryGetValue(dayIndex, out var fi) ? fi.ToString("HH:mm") : "",
                athan["sunrise"].ToString("HH:mm"),
                athan["dhuhr"].ToString("HH:mm"),
                dhuhrIqama.TryGetValue(dayIndex, out var di) ? di.ToString("HH:mm") : "",
                athan["asr"].ToString("HH:mm"),
                asrIqama.TryGetValue(dayIndex, out var ai) ? ai.ToString("HH:mm") : "",
                athan["maghrib"].ToString("HH:mm"),
                maghribIqama.TryGetValue(dayIndex, out var mi) ? mi.ToString("HH:mm") : "",
                athan["isha"].ToString("HH:mm"),
                ishaIqama.TryGetValue(dayIndex, out var ii) ? ii.ToString("HH:mm") : "",
            };

            if (_includeAsrMethods)
            {
                row.Add(athan.TryGetValue("asr_standard", out var s) ? s.ToString("HH:mm") : "");
                row.Add(athan.TryGetValue("asr_hanafi",   out var h) ? h.ToString("HH:mm") : "");
            }

            rows.Add(row.ToArray());
        }

        return rows;
    }

    // ---------------------------------------------------------------------------
    // Normalisation helpers
    // ---------------------------------------------------------------------------

    private static string NormalizeHighLatitudeAdjustment(string? method) => method switch
    {
        "MiddleOfTheNight" or "MIDDLE_OF_THE_NIGHT" or "NightMiddle"
            => PrayerTimes.LatitudeAdjustmentMethodMotn,
        "AngleBased" or "ANGLE_BASED"
            => PrayerTimes.LatitudeAdjustmentMethodAngle,
        "OneSeventh" or "ONE_SEVENTH"
            => PrayerTimes.LatitudeAdjustmentMethodOneSeventh,
        "None" or "NONE"
            => PrayerTimes.LatitudeAdjustmentMethodNone,
        _ => PrayerTimes.LatitudeAdjustmentMethodMotn
    };

    private static string NormalizeAsrSchool(string? method)
        => string.Equals(method, "hanafi", StringComparison.OrdinalIgnoreCase)
            ? PrayerTimes.SchoolHanafi
            : PrayerTimes.SchoolStandard;

    private static bool IsEndOfWeek(int currentDayNumber, int changeOnDayNumber)
    {
        int dayBeforeChange = (changeOnDayNumber - 1 + 7) % 7;
        return currentDayNumber == dayBeforeChange;
    }

    // ---------------------------------------------------------------------------
    // DateTime parsing helper
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Parse a "yyyy-MM-dd HH:mm" string in the given timezone and return a DateTime
    /// with DateTimeKind.Unspecified (local wall-clock time — mirrors PHP behaviour).
    /// </summary>
    private static DateTime ParseLocalDateTime(string dateTimeStr, TimeZoneInfo tz)
    {
        if (!DateTime.TryParseExact(dateTimeStr, "yyyy-MM-dd HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
        {
            // Fallback: try flexible parse
            dt = DateTime.Parse(dateTimeStr, System.Globalization.CultureInfo.InvariantCulture);
        }
        return dt;
    }
}
