using System;
using CIS580.Input;
using CIS580.UI;
using CIS580.UI.Widgets;
using MechaHorde.Settings;

namespace MechaHorde.UI;

/// <summary>
/// The options menu, which edits the game's settings. Each kind of menu item shows a different
/// kind of setting: a choice from an enum or list, a toggle, and a number in a range.
/// </summary>
public class OptionsLayer : MechaMenuLayer
{
    /// <param name="overGame">
    /// Whether the menu is shown over a game in progress (from the pause menu), in which case
    /// the game shows dimly behind it
    /// </param>
    public OptionsLayer(bool overGame = false) : base("Options")
    {
        // Over the game, the world shows dimly behind the menu
        DimBackground = overGame;
    }

    protected override void Load()
    {
        base.Load();

        var settings = Game.Services.GetService<UserSettings>().Game;
        var display = Game.Services.GetService<Display>();

        Menu.Add(new ChoiceItem<Difficulty>("Difficulty", Enum.GetValues<Difficulty>(),
                () => settings.Difficulty, value => settings.Difficulty = value))
            .Add(new ChoiceItem<string>("Player color", GameSettings.PlayerColors,
                () => settings.PlayerColor, value => settings.PlayerColor = value))
            .Add(new ToggleItem("Sound", () => settings.SoundEnabled, value => settings.SoundEnabled = value))
            .Add(new RangeItem("Music volume", 0, GameSettings.MaxVolume, 1,
                () => settings.MusicVolume, value => settings.MusicVolume = value))
            .Add(new RangeItem("Effects volume", 0, GameSettings.MaxVolume, 1,
                () => settings.EffectsVolume, value => settings.EffectsVolume = value))
            .Add(new ToggleItem("Vibration", () => settings.Vibration, value => settings.Vibration = value))
            .Add(new ToggleItem("Fullscreen (F11)", () => display.IsFullscreen, display.SetFullscreen))
            .Add(new ButtonItem("Controls", _ => Open(new ControlsLayer(DimBackground))))
            .Add(new ButtonItem("Back", OnCancel));
    }

    /// <summary>
    /// Saves the settings when the player leaves the menu
    /// </summary>
    protected override void OnCancel(InputSource source)
    {
        var settings = Game.Services.GetService<UserSettings>();
        if (!settings.Save())
            Console.WriteLine($"Couldn't save settings: {settings.Store.LastError?.Message}");
        base.OnCancel(source);
    }
}
