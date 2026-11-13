namespace MechaHorde.Settings;

/// <summary>
/// How hard the game is
/// </summary>
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
}

/// <summary>
/// The player's settings for this game. These are placeholders showing the kinds of settings
/// (an enum, a list of choices, a toggle, and a number) the options menu can edit; replace
/// them with your own game's settings.
/// </summary>
/// <remarks>
/// A single instance is shared through the game's services, so every part of the game sees
/// the same settings.
/// </remarks>
public sealed class GameSettings
{
    /// <summary>
    /// The player colors to choose from
    /// </summary>
    public static readonly string[] PlayerColors = { "Red", "Green", "Blue" };

    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    public string PlayerColor { get; set; } = PlayerColors[0];

    /// <summary>
    /// Whether the game makes any sound at all (mutes the master audio bus)
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// The music volume, from 0 to <see cref="MaxVolume"/>
    /// </summary>
    public int MusicVolume { get; set; } = 7;

    /// <summary>
    /// The sound effects volume (game and menu sounds), from 0 to <see cref="MaxVolume"/>
    /// </summary>
    public int EffectsVolume { get; set; } = 10;

    /// <summary>
    /// The highest volume setting
    /// </summary>
    public const int MaxVolume = 10;

    /// <summary>
    /// Whether gamepads vibrate
    /// </summary>
    public bool Vibration { get; set; } = true;

    /// <summary>
    /// Whether the game fills the screen (otherwise it is in a resizable window). F11 switches.
    /// </summary>
    public bool Fullscreen { get; set; } = true;
}
