using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// PrayerCalculationRule Object
///
/// Specifies a single prayer time calculation rule.
/// </summary>
public class PrayerCalculationRule
{
    /// <summary>A static time value in 24-hour format (e.g., "12:30"). When specified, all other fields are ignored.</summary>
    [JsonPropertyName("static")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Static { get; set; }

    /// <summary>The frequency of change. Either "daily" or "weekly".</summary>
    [JsonPropertyName("change")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Change { get; set; }

    /// <summary>The rounding interval in minutes (e.g., 15 for quarter-hour rounding).</summary>
    [JsonPropertyName("roundMinutes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(StringToIntConverter))]
    public int? RoundMinutes { get; set; }

    /// <summary>The earliest allowed time in 24-hour format (e.g., "04:00").</summary>
    [JsonPropertyName("earliest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Earliest { get; set; }

    /// <summary>The latest allowed time in 24-hour format (e.g., "23:45").</summary>
    [JsonPropertyName("latest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Latest { get; set; }

    /// <summary>The delay in minutes after the Athan (call to prayer).</summary>
    [JsonPropertyName("afterAthanMinutes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(StringToIntConverter))]
    public int? AfterAthanMinutes { get; set; }

    /// <summary>The number of minutes before the end of the prayer timeframe.</summary>
    [JsonPropertyName("beforeEndMinutes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(StringToIntConverter))]
    public int? BeforeEndMinutes { get; set; }

    /// <summary>An array of date-specific overrides.</summary>
    [JsonPropertyName("overrides")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PrayerCalculationOverrideRule>? Overrides { get; set; }

    public PrayerCalculationRule() { }

    public PrayerCalculationRule(
        string? @static = null,
        string? change = null,
        int? roundMinutes = null,
        string? earliest = null,
        string? latest = null,
        int? afterAthanMinutes = null,
        int? beforeEndMinutes = null,
        List<PrayerCalculationOverrideRule>? overrides = null)
    {
        Static = @static;
        Change = change;
        RoundMinutes = roundMinutes;
        Earliest = earliest;
        Latest = latest;
        AfterAthanMinutes = afterAthanMinutes;
        BeforeEndMinutes = beforeEndMinutes;
        Overrides = overrides;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static PrayerCalculationRule FromJson(string json)
        => JsonSerializer.Deserialize<PrayerCalculationRule>(json) ?? throw new ArgumentException("Invalid JSON");
}

/// <summary>
/// JSON converter that reads integer values represented as strings in JSON (e.g., "15") or native integers.
/// Writes them as strings to match the PHP library behaviour.
/// </summary>
internal sealed class StringToIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return s is null ? null : int.Parse(s);
        }

        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        throw new JsonException($"Cannot convert token {reader.TokenType} to int?");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString());
    }
}
