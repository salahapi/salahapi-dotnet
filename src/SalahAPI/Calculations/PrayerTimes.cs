using System.Text.RegularExpressions;
using TimeZoneConverter;

namespace SalahAPI.Calculations;

/// <summary>
/// Prayer Times Calculator.
/// Based on times.js v3.2 by Hamid Zarrabi-Zadeh.
/// Ported to C# from the SalahAPI PHP library.
/// </summary>
public class PrayerTimes
{
    // ---------------------------------------------------------------------------
    // Prayer name constants (used as dictionary keys)
    // ---------------------------------------------------------------------------
    public const string Imsak = "Imsak";
    public const string Fajr = "Fajr";
    public const string Sunrise = "Sunrise";
    public const string Zhuhr = "Dhuhr";
    public const string Asr = "Asr";
    public const string Sunset = "Sunset";
    public const string Maghrib = "Maghrib";
    public const string Isha = "Isha";
    public const string Midnight = "Midnight";
    public const string FirstThird = "Firstthird";
    public const string LastThird = "Lastthird";

    // ---------------------------------------------------------------------------
    // School (Asr shadow factor)
    // ---------------------------------------------------------------------------
    public const string SchoolStandard = "STANDARD";
    public const string SchoolHanafi = "HANAFI";

    // ---------------------------------------------------------------------------
    // Midnight modes
    // ---------------------------------------------------------------------------
    public const string MidnightModeStandard = "STANDARD";
    public const string MidnightModeJafari = "JAFARI";

    // ---------------------------------------------------------------------------
    // Higher-latitude adjustment methods
    // ---------------------------------------------------------------------------
    public const string LatitudeAdjustmentMethodMotn = "MIDDLE_OF_THE_NIGHT";
    public const string LatitudeAdjustmentMethodAngle = "ANGLE_BASED";
    public const string LatitudeAdjustmentMethodOneSeventh = "ONE_SEVENTH";
    public const string LatitudeAdjustmentMethodNone = "NONE";

    // ---------------------------------------------------------------------------
    // Time formats
    // ---------------------------------------------------------------------------
    public const string TimeFormat24H = "24h";
    public const string TimeFormat12H = "12h";
    public const string TimeFormat12HNs = "12hNS";
    public const string TimeFormatFloat = "Float";
    public const string TimeFormatIso8601 = "iso8601";

    public const string InvalidTime = "-----";

    // ---------------------------------------------------------------------------
    // Built-in method parameter tables (mirrors the PHP $methods array)
    // ---------------------------------------------------------------------------
    private static readonly Dictionary<string, Dictionary<string, object>> _methodParams = new()
    {
        ["MWL"]      = new() { ["fajr"] = 18.0, ["isha"] = 17.0 },
        ["ISNA"]     = new() { ["fajr"] = 15.0, ["isha"] = 15.0 },
        ["Egypt"]    = new() { ["fajr"] = 19.5, ["isha"] = 17.5 },
        ["Makkah"]   = new() { ["fajr"] = 18.5, ["isha"] = "90 min" },
        ["Karachi"]  = new() { ["fajr"] = 18.0, ["isha"] = 18.0 },
        ["Tehran"]   = new() { ["fajr"] = 17.7, ["maghrib"] = 4.5, ["midnight"] = "Jafari" },
        ["Jafari"]   = new() { ["fajr"] = 16.0, ["maghrib"] = 4.0, ["midnight"] = "Jafari" },
        ["France"]   = new() { ["fajr"] = 12.0, ["isha"] = 12.0 },
        ["Russia"]   = new() { ["fajr"] = 16.0, ["isha"] = 15.0 },
        ["Singapore"] = new() { ["fajr"] = 20.0, ["isha"] = 18.0 },
        ["defaults"] = new() { ["isha"] = 14.0, ["maghrib"] = "1 min", ["midnight"] = "Standard" }
    };

    private static readonly Dictionary<string, string> _methodCodeToKey = new()
    {
        [Method.MethodMwl]       = "MWL",
        [Method.MethodIsna]      = "ISNA",
        [Method.MethodEgypt]     = "Egypt",
        [Method.MethodMakkah]    = "Makkah",
        [Method.MethodKarachi]   = "Karachi",
        [Method.MethodTehran]    = "Tehran",
        [Method.MethodJafari]    = "Jafari",
        [Method.MethodFrance]    = "France",
        [Method.MethodRussia]    = "Russia",
        [Method.MethodSingapore] = "Singapore",
    };

    // ---------------------------------------------------------------------------
    // Instance state
    // ---------------------------------------------------------------------------
    private Dictionary<string, object> _settings;
    private long _utcTimeMs;            // UTC midnight of the current calculation date in ms
    private bool _adjusted;
    private DateTime _date;
    private string _method = Method.MethodMwl;
    private string _school = SchoolStandard;
    private string? _midnightMode;
    private string? _latitudeAdjustmentMethod;
    private string _timeFormat = TimeFormat24H;
    private double _latitude;
    private double _longitude;
    private double _elevation;
    private Dictionary<string, int> _offset = new();

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------
    public PrayerTimes(string method = Method.MethodMwl, string school = SchoolStandard)
    {
        _settings = new Dictionary<string, object>
        {
            ["dhuhr"]      = "0 min",
            ["asr"]        = "Standard",
            ["highLats"]   = "NightMiddle",
            ["tune"]       = new Dictionary<string, int>(),
            ["format"]     = "24h",
            ["rounding"]   = "nearest",
            ["utcOffset"]  = "auto",
            ["timezone"]   = TimeZoneInfo.Local.Id,
            ["location"]   = new double[] { 0, 0 },
            ["iterations"] = 1
        };

        _date = DateTime.UtcNow;
        SetMethod(method);
        SetSchool(school);
    }

    // ---------------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------------

    /// <summary>Get prayer times for a specific date at a given location.</summary>
    /// <param name="ianaTimezone">
    /// Optional IANA timezone identifier (e.g. "America/New_York"). When supplied it
    /// overrides the timezone derived from <paramref name="date"/>'s Kind, ensuring
    /// correct local-time formatting across machines with different local timezones.
    /// </param>
    public Dictionary<string, string> GetTimes(
        DateTime date,
        double latitude,
        double longitude,
        double elevation = 0,
        string latitudeAdjustmentMethod = LatitudeAdjustmentMethodAngle,
        string? midnightMode = null,
        string format = TimeFormat24H,
        string? ianaTimezone = null)
    {
        _date = date;
        _latitude = latitude;
        _longitude = longitude;
        _elevation = elevation;
        ((double[])_settings["location"])[0] = _latitude;
        ((double[])_settings["location"])[1] = _longitude;

        // Use the explicitly supplied timezone; fall back to the date's kind or local.
        _settings["timezone"] = ianaTimezone ?? GetIanaTimezone(date);

        SetTimeFormat(format);
        SetLatitudeAdjustmentMethod(latitudeAdjustmentMethod);
        if (midnightMode is not null) SetMidnightMode(midnightMode);

        // UTC midnight in ms (matches PHP's utcTime)
        var utcMidnight = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        _utcTimeMs = new DateTimeOffset(utcMidnight).ToUnixTimeMilliseconds();

        var times = ComputeTimes();
        return ConvertTimesToPrayerTimesFormat(times);
    }

    /// <summary>Get prayer times for today at the given IANA timezone.</summary>
    public Dictionary<string, string> GetTimesForToday(
        double latitude,
        double longitude,
        string timezone,
        double elevation = 0,
        string latitudeAdjustmentMethod = LatitudeAdjustmentMethodAngle,
        string? midnightMode = null,
        string format = TimeFormat24H)
    {
        var tz = TZConvert.GetTimeZoneInfo(timezone);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return GetTimes(now, latitude, longitude, elevation, latitudeAdjustmentMethod,
            midnightMode, format, ianaTimezone: timezone);
    }

    // ---------------------------------------------------------------------------
    // Configuration setters
    // ---------------------------------------------------------------------------

    public void SetMethod(string method)
    {
        _method = method;
        _settings = MergeSettings(_settings, _methodParams["defaults"]);
        if (_methodCodeToKey.TryGetValue(method, out var key) && _methodParams.TryGetValue(key, out var mp))
            _settings = MergeSettings(_settings, mp);
    }

    public void SetSchool(string school)
    {
        _school = school;
        _settings["asr"] = school == SchoolHanafi ? "Hanafi" : "Standard";
    }

    public void SetMidnightMode(string mode)
    {
        _midnightMode = mode;
        _settings["midnight"] = mode == MidnightModeJafari ? "Jafari" : "Standard";
    }

    public void SetLatitudeAdjustmentMethod(string method)
    {
        _latitudeAdjustmentMethod = method;
        _settings["highLats"] = method switch
        {
            LatitudeAdjustmentMethodNone       => "None",
            LatitudeAdjustmentMethodMotn       => "NightMiddle",
            LatitudeAdjustmentMethodOneSeventh => "OneSeventh",
            _                                  => "AngleBased"
        };
    }

    public void SetTimeFormat(string format)
    {
        _timeFormat = format;
        _settings["format"] = format switch
        {
            TimeFormat24H   => "24h",
            TimeFormat12H   => "12h",
            TimeFormat12HNs => "12H",
            TimeFormatFloat => "Float",
            _               => "24h"
        };
    }

    public void Tune(int imsak = 0, int fajr = 0, int sunrise = 0, int dhuhr = 0,
                     int asr = 0, int maghrib = 0, int sunset = 0, int isha = 0, int midnight = 0)
    {
        _offset = new Dictionary<string, int>
        {
            [Imsak]   = imsak,   [Fajr]    = fajr,    [Sunrise] = sunrise,
            [Zhuhr]   = dhuhr,   [Asr]     = asr,      [Maghrib] = maghrib,
            [Sunset]  = sunset,  [Isha]    = isha,      [Midnight] = midnight
        };
        _settings["tune"] = new Dictionary<string, int>
        {
            ["fajr"] = fajr, ["sunrise"] = sunrise, ["dhuhr"] = dhuhr,
            ["asr"] = asr, ["sunset"] = sunset, ["maghrib"] = maghrib,
            ["isha"] = isha, ["midnight"] = midnight
        };
    }

    public string GetMethod() => _method;

    // ---------------------------------------------------------------------------
    // Core computation
    // ---------------------------------------------------------------------------

    private Dictionary<string, double> ComputeTimes()
    {
        var times = new Dictionary<string, double>
        {
            ["fajr"] = 5, ["sunrise"] = 6, ["dhuhr"] = 12,
            ["asr"] = 13, ["sunset"] = 18, ["maghrib"] = 18,
            ["isha"] = 18, ["midnight"] = 24
        };

        int iterations = _settings.TryGetValue("iterations", out var it) ? (int)it : 1;
        for (int i = 0; i < iterations; i++)
            times = ProcessTimes(times);

        AdjustHighLats(times);
        UpdateTimes(times);
        TuneTimes(times);
        ConvertTimes(times);

        return times;
    }

    private Dictionary<string, double> ProcessTimes(Dictionary<string, double> times)
    {
        double horizon = 0.833;
        return new Dictionary<string, double>
        {
            ["fajr"]     = AngleTime(GetParamDouble("fajr", 18), times["fajr"], -1),
            ["sunrise"]  = AngleTime(horizon, times["sunrise"], -1),
            ["dhuhr"]    = MidDay(times["dhuhr"]),
            ["asr"]      = AngleTime(AsrAngle(GetParamString("asr", "Standard"), times["asr"]), times["asr"]),
            ["sunset"]   = AngleTime(horizon, times["sunset"]),
            ["maghrib"]  = AngleTime(GetParamDouble("maghrib", 0), times["maghrib"]),
            ["isha"]     = AngleTime(GetParamDouble("isha", 18), times["isha"]),
            ["midnight"] = MidDay(times["midnight"]) + 12
        };
    }

    private void UpdateTimes(Dictionary<string, double> times)
    {
        var maghribParam = GetParamString("maghrib", "0 min");
        if (IsMin(maghribParam))
            times["maghrib"] = times["sunset"] + ValueOf(maghribParam) / 60;

        var ishaParam = GetParamString("isha", "0 min");
        if (IsMin(ishaParam))
            times["isha"] = times["maghrib"] + ValueOf(ishaParam) / 60;

        if (GetParamString("midnight", "Standard") == "Jafari")
        {
            double nextFajr = AngleTime(GetParamDouble("fajr", 18), 29, -1) + 24;
            times["midnight"] = (times["sunset"] + (_adjusted ? times["fajr"] + 24 : nextFajr)) / 2;
        }

        times["dhuhr"] += ValueOf(GetParamString("dhuhr", "0 min")) / 60;
    }

    private void TuneTimes(Dictionary<string, double> times)
    {
        if (!_settings.TryGetValue("tune", out var tuneObj)) return;
        var mins = (Dictionary<string, int>)tuneObj;
        foreach (var key in times.Keys.ToList())
        {
            if (mins.TryGetValue(key, out var m))
                times[key] += m / 60.0;
        }
    }

    private void ConvertTimes(Dictionary<string, double> times)
    {
        double lng = ((double[])_settings["location"])[1];
        foreach (var key in times.Keys.ToList())
        {
            double adjustedTime = times[key] - lng / 15;
            long timestamp = _utcTimeMs + (long)Math.Floor(adjustedTime * 3600000);
            times[key] = RoundTime(timestamp);
        }
    }

    private void AdjustHighLats(Dictionary<string, double> times)
    {
        var highLats = GetParamString("highLats", "NightMiddle");
        if (highLats == "None") return;

        _adjusted = false;
        double night = 24 + times["sunrise"] - times["sunset"];

        times["fajr"]   = AdjustTime(times["fajr"],   times["sunrise"], GetParamString("fajr", "18"),   night, -1);
        times["isha"]   = AdjustTime(times["isha"],    times["sunset"],  GetParamString("isha", "18"),   night);
        times["maghrib"]= AdjustTime(times["maghrib"], times["sunset"],  GetParamString("maghrib","0"),  night);
    }

    private double AdjustTime(double time, double @base, string angle, double night, int direction = 1)
    {
        var highLats = GetParamString("highLats", "NightMiddle");
        double portion = highLats switch
        {
            "NightMiddle" => 0.5 * night,
            "OneSeventh"  => night / 7.0,
            "AngleBased"  => night / 60.0 * ValueOf(angle),
            _             => 0.5 * night
        };

        double timeDiff = (time - @base) * direction;
        if (double.IsNaN(time) || timeDiff > portion)
        {
            time = @base + portion * direction;
            _adjusted = true;
        }

        return time;
    }

    // ---------------------------------------------------------------------------
    // Astronomical helpers
    // ---------------------------------------------------------------------------

    private (double Declination, double Equation) SunPosition(double time)
    {
        double lng = ((double[])_settings["location"])[1];
        double D = _utcTimeMs / 86400000.0 - 10957.5 + time / 24.0 - lng / 360.0;

        double g = Mod(357.529 + 0.98560028 * D, 360);
        double q = Mod(280.459 + 0.98564736 * D, 360);
        double L = Mod(q + 1.915 * DSin(g) + 0.020 * DSin(2 * g), 360);
        double e = 23.439 - 0.00000036 * D;
        double RA = Mod(DAtan2(DCos(e) * DSin(L), DCos(L)) / 15, 24);

        return (DAsin(DSin(e) * DSin(L)), q / 15 - RA);
    }

    private double MidDay(double time)
    {
        var (_, equation) = SunPosition(time);
        return Mod(12 - equation, 24);
    }

    private double AngleTime(double angle, double time, int direction = 1)
    {
        double lat = ((double[])_settings["location"])[0];
        var (declination, _) = SunPosition(time);
        double numerator = -DSin(angle) - DSin(lat) * DSin(declination);
        double denominator = DCos(lat) * DCos(declination);

        if (Math.Abs(numerator / denominator) > 1) return double.NaN;

        double diff = DAcos(numerator / denominator) / 15;
        return MidDay(time) + diff * direction;
    }

    private double AsrAngle(string asrParam, double time)
    {
        double shadowFactor = asrParam switch
        {
            "Standard" => 1,
            "Hanafi"   => 2,
            _          => ValueOf(asrParam)
        };

        double lat = ((double[])_settings["location"])[0];
        var (declination, _) = SunPosition(time);
        return -DArccot(shadowFactor + Math.Tan(Math.Abs(ToRad(lat - declination))));
    }

    // ---------------------------------------------------------------------------
    // Formatting helpers
    // ---------------------------------------------------------------------------

    private string FormatTimeMs(double timestampMs)
    {
        if (double.IsNaN(timestampMs)) return InvalidTime;

        var format = GetParamString("format", "24h");
        return TimeToString(timestampMs, format);
    }

    private string TimeToString(double timestampMs, string format)
    {
        var utcOffset = GetParamString("utcOffset", "auto");
        double seconds = timestampMs / 1000.0;

        DateTimeOffset dto;
        if (utcOffset != "auto")
        {
            // manual offset in minutes
            _ = double.TryParse(utcOffset, out double offsetMins);
            seconds += offsetMins * 60;
            dto = DateTimeOffset.FromUnixTimeSeconds((long)seconds);
        }
        else
        {
            dto = DateTimeOffset.FromUnixTimeSeconds((long)seconds);
            var tz = GetCurrentTimeZoneInfo();
            dto = TimeZoneInfo.ConvertTime(dto, tz);
        }

        return format switch
        {
            "12h" => dto.ToString("h:mm tt"),
            "12H" => dto.ToString("h:mm"),
            _     => dto.ToString("HH:mm")
        };
    }

    // ---------------------------------------------------------------------------
    // Final mapping to prayer-name keyed dictionary with formatted strings
    // ---------------------------------------------------------------------------

    private Dictionary<string, string> ConvertTimesToPrayerTimesFormat(Dictionary<string, double> times)
    {
        var mapping = new Dictionary<string, string>
        {
            ["fajr"]     = Fajr,
            ["sunrise"]  = Sunrise,
            ["dhuhr"]    = Zhuhr,
            ["asr"]      = Asr,
            ["sunset"]   = Sunset,
            ["maghrib"]  = Maghrib,
            ["isha"]     = Isha,
            ["midnight"] = Midnight,
        };

        var result = new Dictionary<string, string>();
        foreach (var (jsKey, csharpKey) in mapping)
        {
            if (times.TryGetValue(jsKey, out var ts))
                result[csharpKey] = FormatTimeMs(ts);
        }
        return result;
    }

    // ---------------------------------------------------------------------------
    // Rounding
    // ---------------------------------------------------------------------------

    private double RoundTime(long timestampMs)
    {
        var rounding = GetParamString("rounding", "nearest");
        long oneMinute = 60000;
        return rounding switch
        {
            "up"   => (double)(long)(Math.Ceiling((double)timestampMs / oneMinute) * oneMinute),
            "down" => (double)(long)(Math.Floor((double)timestampMs / oneMinute) * oneMinute),
            _      => (double)(long)(Math.Round((double)timestampMs / oneMinute) * oneMinute)
        };
    }

    // ---------------------------------------------------------------------------
    // Utilities
    // ---------------------------------------------------------------------------

    private static double ValueOf(string s)
    {
        var m = Regex.Match(s, @"[0-9.+\-]*");
        return m.Success && double.TryParse(m.Value, out var v) ? v : 0;
    }

    private static bool IsMin(string s) => s.Contains("min");

    private static double Mod(double a, double b)
    {
        double result = a % b;
        return result < 0 ? result + b : result;
    }

    private static Dictionary<string, object> MergeSettings(Dictionary<string, object> target, Dictionary<string, object> source)
    {
        var result = new Dictionary<string, object>(target);
        foreach (var (k, v) in source)
            result[k] = v;
        return result;
    }

    private string GetParamString(string key, string defaultValue)
    {
        if (_settings.TryGetValue(key, out var v))
        {
            if (v is string s) return s;
            // Convert numeric settings to string so IsMin / ValueOf callers get the real value
            if (v is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (v is int i) return i.ToString();
        }
        return defaultValue;
    }

    private double GetParamDouble(string key, double defaultValue)
    {
        if (_settings.TryGetValue(key, out var v))
        {
            if (v is double d) return d;
            if (v is int i) return i;
            if (v is string s && double.TryParse(s, out var pd)) return pd;
        }
        return defaultValue;
    }

    // ---------------------------------------------------------------------------
    // Degree-based trigonometry helpers
    // ---------------------------------------------------------------------------

    private static double ToRad(double d) => d * Math.PI / 180.0;
    private static double ToDeg(double r) => r * 180.0 / Math.PI;
    private static double DSin(double d) => Math.Sin(ToRad(d));
    private static double DCos(double d) => Math.Cos(ToRad(d));
    private static double DAsin(double x) => ToDeg(Math.Asin(x));
    private static double DAcos(double x) => ToDeg(Math.Acos(x));
    private static double DAtan2(double y, double x) => ToDeg(Math.Atan2(y, x));
    private static double DArccot(double x) => ToDeg(Math.Atan(1.0 / x));

    // ---------------------------------------------------------------------------
    // Timezone helpers
    // ---------------------------------------------------------------------------

    private string GetIanaTimezone(DateTime date)
    {
        // If the DateTime carries an offset (DateTimeOffset), use that; otherwise fall back to Local.
        if (date.Kind == DateTimeKind.Utc) return "UTC";
        // Use the stored timezone if present and valid
        if (_settings.TryGetValue("timezone", out var tz) && tz is string tzStr && !string.IsNullOrEmpty(tzStr))
            return tzStr;
        return TimeZoneInfo.Local.Id;
    }

    private TimeZoneInfo GetCurrentTimeZoneInfo()
    {
        var tzId = GetParamString("timezone", TimeZoneInfo.Local.Id);
        try { return TZConvert.GetTimeZoneInfo(tzId); }
        catch { return TimeZoneInfo.Local; }
    }
}
