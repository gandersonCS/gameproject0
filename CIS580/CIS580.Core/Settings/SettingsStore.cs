using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CIS580.Settings;

/// <summary>
/// Saves and loads settings as JSON files in a folder, one file per kind of settings
/// (e.g. "settings.json", "controls.json").
/// </summary>
/// <remarks>
/// <para>
/// Settings are a convenience, so problems with them must never stop the game: a missing or
/// damaged file loads as the defaults (the damaged file is renamed so it can be inspected),
/// and a failed save is reported in <see cref="LastError"/> rather than thrown. Files are
/// written to a temporary file first and then moved into place, so a crash part-way through
/// saving can't leave a half-written file.
/// </para>
/// <para>
/// This class has no MonoGame dependencies, so a server could use it too.
/// </para>
/// </remarks>
public sealed class SettingsStore
{
    /// <param name="directory">The folder to keep the settings files in (created when first saving)</param>
    /// <param name="options">How to convert settings to JSON; null for <see cref="DefaultOptions"/></param>
    public SettingsStore(string directory, JsonSerializerOptions options = null)
    {
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));
        Options = options ?? DefaultOptions;
    }

    /// <summary>
    /// Readable JSON: indented, camelCase names, and enums written as names rather than numbers
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// The usual place for a game's settings: a folder named after the game in the user's
    /// application data folder (e.g. %LOCALAPPDATA% on Windows, ~/Library/Application Support on macOS)
    /// </summary>
    /// <param name="gameName">The game's name, used as the folder name</param>
    public static string DefaultDirectory(string gameName)
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create), gameName);

    /// <summary>
    /// The folder the settings files are kept in
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// How settings are converted to JSON
    /// </summary>
    public JsonSerializerOptions Options { get; }

    /// <summary>
    /// The last problem loading or saving, or null if there hasn't been one
    /// </summary>
    public Exception LastError { get; private set; }

    /// <summary>
    /// The full path of a settings file
    /// </summary>
    /// <param name="name">The file's name, e.g. "settings.json"</param>
    public string PathOf(string name) => Path.Combine(Directory, name);

    /// <summary>
    /// Whether a settings file has been saved
    /// </summary>
    public bool Exists(string name) => File.Exists(PathOf(name));

    /// <summary>
    /// Loads settings, or creates the defaults if there are none (or they can't be read)
    /// </summary>
    /// <typeparam name="T">The type of the settings</typeparam>
    /// <param name="name">The file's name, e.g. "settings.json"</param>
    /// <param name="defaults">Creates the default settings</param>
    public T Load<T>(string name, Func<T> defaults)
        => TryLoad(name, out T value) ? value : defaults();

    /// <summary>
    /// Loads settings, if they have been saved and can be read
    /// </summary>
    public bool TryLoad<T>(string name, out T value)
    {
        value = default;
        string path = PathOf(name);
        if (!File.Exists(path))
            return false;

        try
        {
            value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            return value != null;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            // Damaged (or from an incompatible version): keep a copy, and use the defaults
            LastError = e;
            TryMove(path, path + ".bad");
            return false;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LastError = e;
            return false;
        }
    }

    /// <summary>
    /// Saves settings
    /// </summary>
    /// <returns>Whether they were saved (if not, see <see cref="LastError"/>)</returns>
    public bool Save<T>(string name, T value)
    {
        string path = PathOf(name);
        string temporary = path + ".tmp";
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Options));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LastError = e;
            return false;
        }
    }

    /// <summary>
    /// Deletes saved settings, so the defaults are used next time
    /// </summary>
    public void Delete(string name)
    {
        try
        {
            File.Delete(PathOf(name));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LastError = e;
        }
    }

    private void TryMove(string from, string to)
    {
        try
        {
            File.Move(from, to, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LastError = e;
        }
    }
}
