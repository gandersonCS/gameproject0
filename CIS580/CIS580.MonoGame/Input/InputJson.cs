using System.Text.Json;
using System.Text.Json.Serialization;

namespace CIS580.Input;

/// <summary>
/// Converts control schemes and input profiles to and from JSON, so players' custom controls
/// can be saved.
/// </summary>
/// <example>
/// A saved scheme looks like:
/// <code>
/// {
///   "name": "Keyboard",
///   "controls": {
///     "MoveLeft": [ { "type": "key", "key": "A" }, { "type": "key", "key": "Left" } ],
///     "Primary": [ { "type": "mouse", "button": "Left" } ]
///   }
/// }
/// </code>
/// </example>
public static class InputJson
{
    /// <summary>
    /// The serializer options used for input data
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(ControlScheme scheme) => JsonSerializer.Serialize(scheme, Options);

    public static ControlScheme DeserializeScheme(string json) => JsonSerializer.Deserialize<ControlScheme>(json, Options);

    public static string Serialize(InputProfile profile) => JsonSerializer.Serialize(profile, Options);

    public static InputProfile DeserializeProfile(string json) => JsonSerializer.Deserialize<InputProfile>(json, Options);
}
