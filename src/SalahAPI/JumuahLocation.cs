using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// JumuahLocation Object
///
/// Specifies the location for a Jumuah prayer.
/// </summary>
public class JumuahLocation
{
    /// <summary>The name of the location (e.g., "Main Prayer Hall", "Community Center").</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>The physical address of the location.</summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    public JumuahLocation() { }

    public JumuahLocation(string? name = null, string? address = null)
    {
        Name = name;
        Address = address;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static JumuahLocation FromJson(string json)
        => JsonSerializer.Deserialize<JumuahLocation>(json) ?? throw new ArgumentException("Invalid JSON");
}
