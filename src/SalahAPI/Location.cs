using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// Location Object
///
/// Specifies the geographic location for which prayer times are calculated.
/// </summary>
public class Location
{
    /// <summary>Geographic latitude in decimal degrees. Valid range: -90 to 90.</summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>Geographic longitude in decimal degrees. Valid range: -180 to 180.</summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>IANA timezone identifier (e.g., "America/New_York").</summary>
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    /// <summary>The name of the city.</summary>
    [JsonPropertyName("city")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? City { get; set; }

    /// <summary>The name of the country.</summary>
    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; set; }

    /// <summary>The date format pattern used throughout the document (e.g., "YYYY-MM-DD").</summary>
    [JsonPropertyName("dateFormat")]
    public string DateFormat { get; set; } = "YYYY-MM-DD";

    /// <summary>The time format pattern used throughout the document (e.g., "HH:mm" or "hh:mm A").</summary>
    [JsonPropertyName("timeFormat")]
    public string TimeFormat { get; set; } = "HH:mm";

    public Location() { }

    public Location(
        double latitude,
        double longitude,
        string timezone,
        string dateFormat = "YYYY-MM-DD",
        string timeFormat = "HH:mm",
        string? city = null,
        string? country = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Timezone = timezone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        City = city;
        Country = country;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static Location FromJson(string json)
        => JsonSerializer.Deserialize<Location>(json) ?? throw new ArgumentException("Invalid JSON");
}
