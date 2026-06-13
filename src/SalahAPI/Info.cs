using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// Info Object
///
/// Provides metadata about the prayer times data.
/// </summary>
public class Info
{
    /// <summary>A human-readable title of the organization or service providing the prayer times.</summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>A textual description of the organization or service.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>The version identifier of the prayer times data document.</summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>Contact information for the service maintainer.</summary>
    [JsonPropertyName("contact")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Contact? Contact { get; set; }

    public Info() { }

    public Info(string? title = null, string? description = null, string? version = null, Contact? contact = null)
    {
        Title = title;
        Description = description;
        Version = version;
        Contact = contact;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static Info FromJson(string json)
        => JsonSerializer.Deserialize<Info>(json) ?? throw new ArgumentException("Invalid JSON");
}
