using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// DailyPrayerTimes Object
///
/// Provides a reference to the CSV data containing daily prayer times.
/// </summary>
public class DailyPrayerTimes
{
    /// <summary>The URL of the endpoint that serves prayer times in CSV format.</summary>
    [JsonPropertyName("csvUrl")]
    public string CsvUrl { get; set; } = string.Empty;

    /// <summary>URL parameters that may be passed to the CSV endpoint.</summary>
    [JsonPropertyName("csvUrlParameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CsvUrlParameters? CsvUrlParameters { get; set; }

    /// <summary>The date format pattern used in the CSV (e.g., "YYYY-MM-DD").</summary>
    [JsonPropertyName("dateFormat")]
    public string DateFormat { get; set; } = "YYYY-MM-DD";

    /// <summary>The time format pattern used in the CSV (e.g., "HH:mm" or "hh:mm A").</summary>
    [JsonPropertyName("timeFormat")]
    public string TimeFormat { get; set; } = "HH:mm";

    public DailyPrayerTimes() { }

    public DailyPrayerTimes(
        string csvUrl,
        string dateFormat = "YYYY-MM-DD",
        string timeFormat = "HH:mm",
        CsvUrlParameters? csvUrlParameters = null)
    {
        CsvUrl = csvUrl;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        CsvUrlParameters = csvUrlParameters;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static DailyPrayerTimes FromJson(string json)
        => JsonSerializer.Deserialize<DailyPrayerTimes>(json) ?? throw new ArgumentException("Invalid JSON");
}
