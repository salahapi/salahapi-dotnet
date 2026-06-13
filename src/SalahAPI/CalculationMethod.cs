using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// CalculationMethod Object
///
/// Specifies the parameters used for calculating prayer times.
/// </summary>
public class CalculationMethod
{
    /// <summary>The calculation method identifier.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The angle of the sun below the horizon for Fajr calculation, in degrees.</summary>
    [JsonPropertyName("fajrAngle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FajrAngle { get; set; }

    /// <summary>The angle of the sun below the horizon for Isha calculation, in degrees.</summary>
    [JsonPropertyName("ishaAngle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? IshaAngle { get; set; }

    /// <summary>The method for calculating Asr prayer time (e.g., "Standard" or "Hanafi").</summary>
    [JsonPropertyName("asrCalculationMethod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AsrCalculationMethod { get; set; }

    /// <summary>The adjustment method for high latitude locations (e.g., "MiddleOfTheNight").</summary>
    [JsonPropertyName("highLatitudeAdjustment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HighLatitudeAdjustment { get; set; }

    /// <summary>Rules for calculating Iqama times.</summary>
    [JsonPropertyName("iqamaCalculationRules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IqamaCalculationRules? IqamaCalculationRules { get; set; }

    /// <summary>An array of Jumuah (Friday prayer) rules.</summary>
    [JsonPropertyName("jumuahRules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JumuahRule>? JumuahRules { get; set; }

    public CalculationMethod() { }

    public CalculationMethod(
        string name,
        double? fajrAngle = null,
        double? ishaAngle = null,
        string? asrCalculationMethod = null,
        string? highLatitudeAdjustment = null,
        IqamaCalculationRules? iqamaCalculationRules = null,
        List<JumuahRule>? jumuahRules = null)
    {
        Name = name;
        FajrAngle = fajrAngle;
        IshaAngle = ishaAngle;
        AsrCalculationMethod = asrCalculationMethod;
        HighLatitudeAdjustment = highLatitudeAdjustment;
        IqamaCalculationRules = iqamaCalculationRules;
        JumuahRules = jumuahRules;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static CalculationMethod FromJson(string json)
        => JsonSerializer.Deserialize<CalculationMethod>(json) ?? throw new ArgumentException("Invalid JSON");
}
