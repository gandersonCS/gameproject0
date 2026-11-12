using System;
using System.Collections.Generic;

namespace CIS580.Audio;

/// <summary>
/// The game's audio: the buses (volume groups) sounds play on, and the music player.
/// </summary>
/// <remarks>
/// <para>
/// There are four buses to begin with. <see cref="Music"/>, <see cref="Effects"/> and
/// <see cref="Ui"/> all feed into <see cref="Master"/>, so players can set the volume of each
/// kind of sound, and muting the master silences everything. Keeping user interface sounds on
/// their own bus means the game's sound effects can be paused (with the game) while menu
/// sounds still play.
/// </para>
/// <para>
/// Sounds are played through <see cref="SoundCue"/>s on a bus, and music through
/// <see cref="MusicPlayer"/>. Call <see cref="Update"/> once per frame.
/// </para>
/// </remarks>
public sealed class AudioManager
{
    private readonly List<AudioBus> _buses = new();

    public AudioManager()
    {
        Master = CreateBus("Master", parent: null);
        Music = CreateBus("Music");
        Effects = CreateBus("Effects");
        Ui = CreateBus("UI");
        MusicPlayer = new MusicPlayer(Music);
    }

    /// <summary>
    /// The bus every other bus feeds into
    /// </summary>
    public AudioBus Master { get; }

    /// <summary>
    /// The bus for background music
    /// </summary>
    public AudioBus Music { get; }

    /// <summary>
    /// The bus for the game's sound effects
    /// </summary>
    public AudioBus Effects { get; }

    /// <summary>
    /// The bus for user interface sounds
    /// </summary>
    public AudioBus Ui { get; }

    /// <summary>
    /// Plays background music on the <see cref="Music"/> bus
    /// </summary>
    public MusicPlayer MusicPlayer { get; }

    /// <summary>
    /// All the buses
    /// </summary>
    public IReadOnlyList<AudioBus> Buses => _buses;

    /// <summary>
    /// The time since the audio manager started (advanced by <see cref="Update"/>)
    /// </summary>
    public TimeSpan Now { get; private set; }

    /// <summary>
    /// Random numbers for varying sounds. (Not the simulation's random numbers: see <see cref="SoundCue"/>.)
    /// </summary>
    public Random Random { get; } = new();

    /// <summary>
    /// Adds a bus, e.g. for voices or ambience, so its volume can be set separately
    /// </summary>
    /// <param name="name">The bus's name</param>
    /// <param name="parent">The bus it feeds into; the master bus if null</param>
    public AudioBus CreateBus(string name, AudioBus parent)
    {
        var bus = new AudioBus(this, name, parent);
        _buses.Add(bus);
        return bus;
    }

    /// <inheritdoc cref="CreateBus(string, AudioBus)"/>
    public AudioBus CreateBus(string name) => CreateBus(name, Master);

    /// <summary>
    /// Applies volume and pause changes, cleans up finished sounds, and advances music fades
    /// </summary>
    /// <param name="elapsed">The time since the last update</param>
    public void Update(TimeSpan elapsed)
    {
        Now += elapsed;
        foreach (var bus in _buses)
            bus.Update();
        MusicPlayer.Update(elapsed);
    }

    /// <summary>
    /// Stops every sound and all music at once
    /// </summary>
    public void StopAll()
    {
        foreach (var bus in _buses)
            bus.StopAll();
        MusicPlayer.StopAll();
    }
}
