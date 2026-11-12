using System;
using System.Collections.Generic;
using CIS580.Input;
using CIS580.Settings;
using MechaHorde.Input;

namespace MechaHorde.Settings;

/// <summary>
/// Everything the player can set up, loaded when the game starts and saved when they change it.
/// </summary>
/// <remarks>
/// <para>
/// Each kind of setting has its own file in the store's folder, so a problem with one doesn't
/// lose the others:
/// </para>
/// <list type="bullet">
/// <item>settings.json: the <see cref="GameSettings"/> (difficulty, volumes, ...)</item>
/// <item>input.json: the <see cref="InputProfile"/> (invert Y, dead zone)</item>
/// <item>controls.json: only the controls the player has rebound (see <see cref="ControlSet.GetChanges"/>)</item>
/// </list>
/// <para>
/// To add a setting, add a property to GameSettings (it's saved automatically) and an item for
/// it to the options menu.
/// </para>
/// </remarks>
public sealed class UserSettings
{
    private const string SettingsFile = "settings.json";
    private const string InputFile = "input.json";
    private const string ControlsFile = "controls.json";

    private UserSettings(SettingsStore store, GameSettings game, InputProfile input, GameControls controls)
    {
        Store = store;
        Game = game;
        Input = input;
        Controls = controls;
    }

    /// <summary>
    /// Where the settings are saved
    /// </summary>
    public SettingsStore Store { get; }

    /// <summary>
    /// The game's own settings
    /// </summary>
    public GameSettings Game { get; }

    /// <summary>
    /// The local players' input preferences
    /// </summary>
    public InputProfile Input { get; }

    /// <summary>
    /// The controls, including any the player has rebound
    /// </summary>
    public GameControls Controls { get; }

    /// <summary>
    /// Loads the saved settings, using the defaults for any that haven't been saved (or can't be read)
    /// </summary>
    public static UserSettings Load(SettingsStore store)
    {
        var game = store.Load(SettingsFile, () => new GameSettings());
        var input = store.Load(InputFile, () => new InputProfile());

        var controls = new GameControls();
        controls.Set.ApplyChanges(store.Load(ControlsFile, () => new List<ControlScheme>()));

        return new UserSettings(store, game, input, controls);
    }

    /// <summary>
    /// Saves the settings
    /// </summary>
    /// <returns>Whether everything was saved (if not, see the store's LastError)</returns>
    public bool Save()
    {
        bool saved = Store.Save(SettingsFile, Game);
        saved &= Store.Save(InputFile, Input);

        // Only the rebound controls are saved; with none, there's no need for the file at all
        var changes = Controls.Set.GetChanges();
        if (changes.Count > 0)
            saved &= Store.Save(ControlsFile, changes);
        else
            Store.Delete(ControlsFile);

        return saved;
    }
}
