using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// IqamaCalculationRules Object
///
/// Specifies the rules for calculating Iqama (congregation prayer start time) times.
/// </summary>
public class IqamaCalculationRules
{
    /// <summary>The day of the week when Iqama times change (e.g., "Friday").</summary>
    [JsonPropertyName("changeOn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChangeOn { get; set; }

    /// <summary>Calculation rule for Fajr Iqama.</summary>
    [JsonPropertyName("fajr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrayerCalculationRule? Fajr { get; set; }

    /// <summary>Calculation rule for Dhuhr Iqama.</summary>
    [JsonPropertyName("dhuhr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrayerCalculationRule? Dhuhr { get; set; }

    /// <summary>Calculation rule for Asr Iqama.</summary>
    [JsonPropertyName("asr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrayerCalculationRule? Asr { get; set; }

    /// <summary>Calculation rule for Maghrib Iqama.</summary>
    [JsonPropertyName("maghrib")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrayerCalculationRule? Maghrib { get; set; }

    /// <summary>Calculation rule for Isha Iqama.</summary>
    [JsonPropertyName("isha")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrayerCalculationRule? Isha { get; set; }

    public IqamaCalculationRules() { }

    public IqamaCalculationRules(
        string? changeOn = null,
        PrayerCalculationRule? fajr = null,
        PrayerCalculationRule? dhuhr = null,
        PrayerCalculationRule? asr = null,
        PrayerCalculationRule? maghrib = null,
        PrayerCalculationRule? isha = null)
    {
        ChangeOn = changeOn;
        Fajr = fajr;
        Dhuhr = dhuhr;
        Asr = asr;
        Maghrib = maghrib;
        Isha = isha;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static IqamaCalculationRules FromJson(string json)
        => JsonSerializer.Deserialize<IqamaCalculationRules>(json) ?? throw new ArgumentException("Invalid JSON");
}
