using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Feedback;

/// <summary>
/// Vibrates gamepads, for effects like a hit or an engine's rumble.
/// </summary>
/// <remarks>
/// <para>
/// Each rumble lasts a set time and then stops by itself, so callers never need to remember to
/// turn the motors off. When several rumbles overlap on the same gamepad, the strongest wins.
/// Players on the keyboard simply don't feel anything.
/// </para>
/// <para>
/// Call <see cref="Update"/> once per frame, and <see cref="StopAll"/> when the game pauses or
/// ends: motors left running are very noticeable!
/// </para>
/// </remarks>
public sealed class Rumble
{
    private const int MaxGamePads = 4;

    // A rumble in progress on one gamepad
    private sealed record Effect(int GamePad, float Low, float High, TimeSpan Ends);

    private readonly Action<int, float, float> _setVibration;
    private readonly List<Effect> _effects = new();
    private readonly (float Low, float High)[] _applied = new (float, float)[MaxGamePads];
    private TimeSpan _now;

    /// <param name="setVibration">
    /// Sets a gamepad's motor speeds (gamepad index, low-frequency motor, high-frequency motor).
    /// Leave null to vibrate real gamepads; tests pass their own.
    /// </param>
    public Rumble(Action<int, float, float> setVibration = null)
    {
        _setVibration = setVibration ?? ((pad, low, high) => GamePad.SetVibration(pad, low, high));
    }

    /// <summary>
    /// Whether rumble is turned on (players may turn it off in the settings)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Vibrates a gamepad
    /// </summary>
    /// <param name="gamePad">The gamepad's index (0 to 3)</param>
    /// <param name="low">The low-frequency (heavy, rumbling) motor's speed, 0 to 1</param>
    /// <param name="high">The high-frequency (light, buzzing) motor's speed, 0 to 1</param>
    /// <param name="duration">How long to vibrate for</param>
    public void Play(int gamePad, float low, float high, TimeSpan duration)
    {
        if (!Enabled || gamePad < 0 || gamePad >= MaxGamePads)
            return;
        _effects.Add(new Effect(gamePad, Math.Clamp(low, 0f, 1f), Math.Clamp(high, 0f, 1f), _now + duration));
    }

    /// <summary>
    /// Vibrates the gamepads a player is using (if any)
    /// </summary>
    /// <inheritdoc cref="Play(int, float, float, TimeSpan)"/>
    public void Play(LocalPlayer player, float low, float high, TimeSpan duration)
    {
        foreach (var assignment in player.Input.Assignments)
            if (assignment.Device.Kind == DeviceKind.GamePad)
                Play(assignment.Device.GamePadIndex, low, high, duration);
    }

    /// <summary>
    /// Stops every gamepad vibrating
    /// </summary>
    public void StopAll()
    {
        _effects.Clear();
        Apply();
    }

    /// <summary>
    /// Ends rumbles that are over, and sets the motors' speeds
    /// </summary>
    public void Update(GameTime gameTime) => Update(gameTime.ElapsedGameTime);

    /// <inheritdoc cref="Update(GameTime)"/>
    public void Update(TimeSpan elapsed)
    {
        _now += elapsed;
        _effects.RemoveAll(e => e.Ends <= _now);
        if (!Enabled)
            _effects.Clear();
        Apply();
    }

    /// <summary>
    /// Sets each gamepad's motors to its strongest rumble, telling the gamepad only when that changes
    /// </summary>
    private void Apply()
    {
        for (int pad = 0; pad < MaxGamePads; pad++)
        {
            (float Low, float High) speed = (0f, 0f);
            foreach (var effect in _effects)
            {
                if (effect.GamePad == pad)
                    speed = (Math.Max(speed.Low, effect.Low), Math.Max(speed.High, effect.High));
            }

            if (speed != _applied[pad])
            {
                _setVibration(pad, speed.Low, speed.High);
                _applied[pad] = speed;
            }
        }
    }
}
