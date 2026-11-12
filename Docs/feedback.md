# Game Feel

[← Overview](README.md)

"Game feel" (or "juice") is the extra feedback that makes actions feel powerful: the screen
shakes when something explodes, the controller rumbles when you're hit, and the game freezes for
a few frames when a big blow lands. None of it changes the rules, so like audio it lives entirely
in the client and responds to simulation events.

The library code is in `CIS580.MonoGame/Feedback/`. The game decides when to use it in
[`MechaHorde/Feedback/GameFeedback.cs`](../MechaHorde/Feedback/GameFeedback.cs).

## `ScreenShake`

[`ScreenShake`](../CIS580/CIS580.MonoGame/Feedback/ScreenShake.cs) uses the **trauma** model from
Squirrel Eiserloh's GDC talk *Math for Game Programmers: Juicing Your Cameras With Math*:

- Events add **trauma**, from 0 (still) to 1 (maximum): a light hit 0.2, an explosion 0.6.
- Trauma wears off at `Recovery` per second (1.5).
- The shake grows with trauma **squared**, so small amounts barely move the view, big ones are
  violent, and repeated hits build on each other.
- The motion comes from smooth noise (summed sine waves), so the view wobbles instead of jittering.

| Property | Default | Meaning |
|---|---|---|
| `MaxOffset` | 24 | Pixels moved at full trauma |
| `MaxAngle` | 0.05 | Radians turned at full trauma |
| `Recovery` | 1.5 | Trauma lost per second |
| `Frequency` | 20 | How frantic the shake is |

Every [`Camera2D`](views.md#camera2d) has its own `Shake`, which is included in its `Transform`,
and `WorldLayer` updates them. So:

```csharp
_worldView.Shake(0.5f);            // shake every camera (WorldLayer.Shake)
camera.Shake.Add(0.3f);            // shake one player's view in split screen
```

Because the shake is applied by the camera, the HUD (drawn in screen space) stays still, which
is what you want.

## `Rumble`

[`Rumble`](../CIS580/CIS580.MonoGame/Feedback/Rumble.cs) vibrates gamepads. Each rumble has a
duration and stops by itself, so you never have to remember to turn the motors off. When rumbles
overlap on the same gamepad, the strongest wins.

```csharp
rumble.Play(player, low: 0.8f, high: 0.4f, TimeSpan.FromMilliseconds(200));  // a LocalPlayer's gamepads
rumble.Play(gamePad: 0, low: 0.2f, high: 0.5f, TimeSpan.FromMilliseconds(150));
```

- `low` drives the heavy, rumbling motor; `high` drives the light, buzzing motor.
- Keyboard players simply feel nothing.
- `Enabled` turns all rumble off. `InGameState` sets it from the player's *Vibration* setting
  every frame.
- Call `Update` every frame, and `StopAll()` when the game pauses or ends. `InGameState` does
  both. Motors left running are very noticeable.

The constructor takes an optional `Action<int, float, float>` for setting the motors. The game
leaves it null to vibrate real gamepads, and tests pass a fake to record what would have happened.

## `HitStop`

[`HitStop`](../CIS580/CIS580.MonoGame/Feedback/HitStop.cs) freezes the game for a moment by
**swallowing elapsed time** before it reaches the simulation:

```csharp
// InGameState.Update
Runner.Update(_hitStop.Apply(gameTime.ElapsedGameTime));

// when a big hit lands
_hitStop.Start(TimeSpan.FromMilliseconds(80));
```

Only the simulation freezes. Menus, music, screen shake, and rumble keep going, which is what
makes a hit-stop feel like impact rather than lag. Overlapping hit-stops don't add up; the longer
one wins. Typical durations are 50–150 ms.

> Hit-stop freezes only **this machine's** copy of the simulation. That's fine for local play.
> In a networked game every machine must agree on the timing, so there the freeze belongs inside
> the simulation instead (for example, entities that skip a few ticks after a hit).

## `GameFeedback`: the game's feel, in one place

[`GameFeedback`](../MechaHorde/Feedback/GameFeedback.cs) turns simulation events into
shake, rumble, and hit-stop, the same way `GameAudio` turns them into sound. `InGameState`
creates it in `Enter()` with the local players, the world view, and its `Rumble` and `HitStop`,
then subscribes it to `GameEventRaised`.

Currently it does one thing: a short buzz when a player joins, so people can tell which gamepad
is theirs. Keeping all the feel decisions in one class makes them easy to find and tune together.

## Extending game feel

### Responding to a new event

Add a case to `GameFeedback.OnGameEvent`:

```csharp
case PlayerHit hit:
    _world.Shake(0.4f);                                                        // shake the view
    if (PlayerFor(hit.Player) is { } victim)
        _rumble.Play(victim, 0.8f, 0.4f, TimeSpan.FromMilliseconds(200));       // rumble their gamepad
    _hitStop.Start(TimeSpan.FromMilliseconds(80));                             // freeze for a moment
    break;
```

`PlayerFor` returns `null` for players who aren't on this machine (e.g. remote players), so
check for that.

### Tuning tips

- **Less is more.** Start small (trauma 0.1–0.3, hit-stop 50 ms) and increase until it feels right.
- **Scale with importance.** A coin pickup might get a tiny shake; a boss hit gets everything.
- **Combine channels.** Sound, shake, rumble, and a brief freeze together feel far stronger than
  any one of them turned up.
- **Feel on the receiving end.** Rumble the gamepad of the player who was *hit*, not everyone's.

### Letting players turn it off

Screen shake can cause motion sickness, so it's good practice to make it optional. Following
the pattern of the existing *Vibration* setting:

1. Add `public bool ScreenShake { get; set; } = true;` to [`GameSettings`](settings.md).
2. Add `.Add(new ToggleItem("Screen shake", () => settings.ScreenShake, value => settings.ScreenShake = value))`
   to `OptionsLayer`.
3. Pass `GameSettings` to `GameFeedback` and skip `_world.Shake(...)` when it's off (or scale
   the trauma by a "shake intensity" setting).
