using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using CIS580.Audio;
using CIS580.UI;

namespace MechaHorde.UI;

/// <summary>
/// MechaHorde's user interface theme: its fonts, colors, panel art and click sounds. Every
/// menu, prompt and the lobby take their look from here.
/// </summary>
public static class GameTheme
{
    /// <summary>
    /// Creates the theme, loading its content
    /// </summary>
    /// <param name="content">Loads the theme's fonts, textures and sounds</param>
    /// <param name="audio">Plays the theme's sounds (on the UI bus)</param>
    public static UiTheme Create(ContentManager content, AudioManager audio) => new()
    {
        TitleFont = content.Load<SpriteFont>("Fonts/aquire"),
        TextFont = content.Load<SpriteFont>("Fonts/robotheroes"),
        PanelTexture = content.Load<Texture2D>("Textures/Menu/futureui1"),

        TitleColor = Color.Gold,
        TitleScale = 1f,
        TextColor = Color.White,
        SelectedColor = Color.CornflowerBlue,

        Sounds = new UiSoundSet
        {
            // Hovering a button, and choosing one, sound as they did in the original menu.
            // The selection can move quickly (holding a key repeats), so allow only a couple at once.
            Move = new SoundCue(audio.Ui, content.Load<SoundEffect>("Audio/Sfx/Click_Standard_05")) { MaxInstances = 2 },
            Confirm = new SoundCue(audio.Ui, content.Load<SoundEffect>("Audio/Sfx/Click_Heavy_00")),
            Cancel = new SoundCue(audio.Ui, content.Load<SoundEffect>("Audio/Sfx/Click_Standard_02")),
        },
    };
}
