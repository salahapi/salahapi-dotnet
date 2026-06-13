using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// SalahAPI - Prayer Times Document
///
/// Represents a SalahAPI document structure as defined in version 1.1 of the specification.
/// </summary>
public class SalahAPIDocument
{
    /// <summary>The version of the SalahAPI Specification that the document conforms to.</summary>
    [JsonPropertyName("salahapi")]
    public string Salahapi { get; set; } = "1.1";

    /// <summary>Metadata describing the prayer times data.</summary>
    [JsonPropertyName("info")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Info? Info { get; set; }

    /// <summary>Geographic coordinates and timezone information.</summary>
    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Location? Location { get; set; }

    /// <summary>Parameters used for prayer time calculations.</summary>
    [JsonPropertyName("calculationMethod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CalculationMethod? CalculationMethod { get; set; }

    /// <summary>Reference to the CSV prayer times data.</summary>
    [JsonPropertyName("dailyPrayerTimes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DailyPrayerTimes? DailyPrayerTimes { get; set; }

    public SalahAPIDocument() { }

    public SalahAPIDocument(
        string salahapi = "1.1",
        Info? info = null,
        Location? location = null,
        CalculationMethod? calculationMethod = null,
        DailyPrayerTimes? dailyPrayerTimes = null)
    {
        Salahapi = salahapi;
        Info = info;
        Location = location;
        CalculationMethod = calculationMethod;
        DailyPrayerTimes = dailyPrayerTimes;
    }

    /// <summary>Serialize the document to a JSON string.</summary>
    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>Deserialize a SalahAPIDocument from a JSON string.</summary>
    public static SalahAPIDocument FromJson(string json)
    {
        var result = JsonSerializer.Deserialize<SalahAPIDocument>(json)
            ?? throw new ArgumentException("Invalid JSON string");
        return result;
    }
}
