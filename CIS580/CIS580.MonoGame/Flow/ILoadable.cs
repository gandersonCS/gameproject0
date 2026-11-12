using System.Collections.Generic;

namespace CIS580.Flow;

/// <summary>
/// A state that needs to load before it starts, such as the in-game state loading its level.
/// Use a <see cref="LoadingState"/> to load it with a loading screen.
/// </summary>
public interface ILoadable
{
    /// <summary>
    /// Loads the state a step at a time, yielding the progress (0 to 1) after each step.
    /// </summary>
    /// <remarks>
    /// Loading runs on the game's main thread, a few steps per frame, so the loading screen
    /// keeps animating. Keep each step short: load one asset, or build one part of the level.
    /// Loading happens before the state is entered, but the state can already use the game
    /// (e.g. its services and content).
    /// </remarks>
    /// <example>
    /// <code>
    /// public IEnumerable&lt;float&gt; Load()
    /// {
    ///     _font = _content.Load&lt;SpriteFont&gt;("Fonts/gamefont");
    ///     yield return 0.5f;
    ///     _session = new GameSession(seed);
    ///     yield return 1f;
    /// }
    /// </code>
    /// </example>
    IEnumerable<float> Load();
}
