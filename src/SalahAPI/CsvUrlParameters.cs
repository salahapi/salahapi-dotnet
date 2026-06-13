using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalahAPI;

/// <summary>
/// CsvUrlParameters Object
///
/// Specifies optional URL parameters that may be passed to the CSV endpoint.
/// Each parameter is keyed by its name and contains configuration such as
/// where the value is sent ("query", "path", "header") and the value or date binding.
/// </summary>
public class CsvUrlParameters
{
    private readonly Dictionary<string, Dictionary<string, object>> _parameters;

    public CsvUrlParameters() => _parameters = new();

    public CsvUrlParameters(Dictionary<string, Dictionary<string, object>> parameters)
        => _parameters = parameters;

    /// <summary>Add a static-value parameter.</summary>
    public CsvUrlParameters AddStaticParameter(string name, string @in, object value)
    {
        _parameters[name] = new Dictionary<string, object>
        {
            ["in"] = @in,
            ["value"] = value
        };
        return this;
    }

    /// <summary>Add a date-binding parameter (fromDate / toDate).</summary>
    public CsvUrlParameters AddDateParameter(string name, string @in, string type, string format)
    {
        _parameters[name] = new Dictionary<string, object>
        {
            ["in"] = @in,
            ["type"] = type,
            ["format"] = format
        };
        return this;
    }

    /// <summary>Retrieve a parameter configuration by name.</summary>
    public Dictionary<string, object>? GetParameter(string name)
        => _parameters.TryGetValue(name, out var p) ? p : null;

    /// <summary>Return all parameter configurations.</summary>
    public IReadOnlyDictionary<string, Dictionary<string, object>> GetAllParameters()
        => _parameters;

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };
        return JsonSerializer.Serialize(_parameters, options);
    }

    public static CsvUrlParameters FromJson(string json)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json)
            ?? throw new ArgumentException("Invalid JSON");
        return new CsvUrlParameters(raw);
    }
}
