using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// JumuahRule Object
///
/// Specifies a single Jumuah (Friday prayer) rule.
/// </summary>
public class JumuahRule
{
    /// <summary>The name of the Jumuah prayer (e.g., "Jumuah 1", "Youth Jumuah").</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>Calculation rule for Jumuah time.</summary>
    [JsonPropertyName("time")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrayerCalculationRule? Time { get; set; }

    /// <summary>Location information for the Jumuah prayer.</summary>
    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JumuahLocation? Location { get; set; }

    public JumuahRule() { }

    public JumuahRule(string? name = null, PrayerCalculationRule? time = null, JumuahLocation? location = null)
    {
        Name = name;
        Time = time;
        Location = location;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static JumuahRule FromJson(string json)
        => JsonSerializer.Deserialize<JumuahRule>(json) ?? throw new ArgumentException("Invalid JSON");
}
