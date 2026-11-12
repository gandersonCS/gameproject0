using CIS580.UI;

namespace CIS580.Audio;

/// <summary>
/// Plays the user interface's sounds through sound cues. Assign one to the theme's
/// <see cref="UiTheme.Sounds"/> to give menus sound.
/// </summary>
public sealed class UiSoundSet : IUiSounds
{
    /// <summary>
    /// Played when the menu selection moves (or null for silence)
    /// </summary>
    public SoundCue Move { get; init; }

    /// <summary>
    /// Played when a menu item is chosen or a value changes (or null for silence)
    /// </summary>
    public SoundCue Confirm { get; init; }

    /// <summary>
    /// Played when a menu is cancelled (or null for silence)
    /// </summary>
    public SoundCue Cancel { get; init; }

    public void Play(UiSound sound)
    {
        var cue = sound switch
        {
            UiSound.Move => Move,
            UiSound.Confirm => Confirm,
            UiSound.Cancel => Cancel,
            _ => null,
        };
        cue?.Play();
    }
}
