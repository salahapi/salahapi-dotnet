using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// PrayerCalculationOverrideRule Object
///
/// Specifies a date-specific override for a prayer time calculation rule.
/// </summary>
public class PrayerCalculationOverrideRule
{
    /// <summary>The type of override rule. One of: "daylightSavingsTime", "ramadan".</summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    /// <summary>The prayer calculation rule to apply for the override.</summary>
    [JsonPropertyName("time")]
    public PrayerCalculationRule Time { get; set; } = new();

    public PrayerCalculationOverrideRule() { }

    public PrayerCalculationOverrideRule(string condition, PrayerCalculationRule time)
    {
        Condition = condition;
        Time = time;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static PrayerCalculationOverrideRule FromJson(string json)
        => JsonSerializer.Deserialize<PrayerCalculationOverrideRule>(json) ?? throw new ArgumentException("Invalid JSON");
}
