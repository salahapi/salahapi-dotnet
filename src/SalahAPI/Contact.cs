using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// Contact Object
///
/// Specifies contact information for the service maintainer.
/// </summary>
public class Contact
{
    /// <summary>The identifying name of the contact person or organization.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>The email address of the contact person or organization.</summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    public Contact() { }

    public Contact(string? name = null, string? email = null)
    {
        Name = name;
        Email = email;
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(this, options);
    }

    public static Contact FromJson(string json)
        => JsonSerializer.Deserialize<Contact>(json) ?? throw new ArgumentException("Invalid JSON");
}
