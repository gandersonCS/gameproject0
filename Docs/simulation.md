# Simulation

[← Overview](README.md)

The simulation is the game itself: the state of the world and the rules that change it. It
lives in two MonoGame-free projects:

- **`CIS580.Core`** (`CIS580.Simulation` namespace): the machinery that runs any simulation,
  including the fixed-tick runner, commands, actions, events, and random numbers.
- **`MechaHorde.Simulation`**: *this* game's world and rules. It contains placeholders for
  you to replace.

## Why a separate simulation?

Most beginner MonoGame games update their game objects directly in `Game.Update`, reading the
keyboard as they go. That works, but it ties the rules of the game to the window, the frame rate,
and the input devices. This project keeps them apart:

| The simulation... | ...so that |
|---|---|
| advances in fixed-length **ticks** (60 per second), not variable frames | physics and timing behave the same at any frame rate |
| gets player input only as **commands** (`PlayerCommand`) | AI, replays, tests, and remote players can drive it just as easily as a keyboard |
| reports what happened as **events**, and never plays sounds or draws | it can run on a headless server, and the client decides how things look and sound |
| is **deterministic**: the same seed and commands always give the same result | networked games and replays become possible |
| references **no MonoGame types** | it can run in a server or a unit test with no window |

You don't need networking to benefit from this design. Every one of these properties also makes
the game easier to test and debug.

## The pieces

### `PlayerId`

[`PlayerId`](../CIS580/CIS580.Core/Simulation/PlayerId.cs) identifies a player in the
simulation. It is a small `record struct` wrapping an `int`, and it is deliberately unrelated to
any input device. Local players are numbered from 1 in the order they joined.

### `PlayerCommand` and `ActionSet`

[`PlayerCommand`](../CIS580/CIS580.Core/Simulation/PlayerCommand.cs) is everything one player
wants to do during one tick:

```csharp
public struct PlayerCommand
{
    public Vector2 Move;        // desired movement, length 0..1; X right, Y down
    public Vector2 Look;        // desired look/aim direction, length 0..1
    public ActionSet Actions;   // which game actions are held down
}
```

[`ActionSet`](../CIS580/CIS580.Core/Simulation/ActionSet.cs) stores held actions as bits in a
`uint`. Each game defines its actions as an enum whose values (0–31) are bit indices.
This game's actions are in [`GameAction`](../MechaHorde.Simulation/GameAction.cs):

```csharp
public enum GameAction
{
    Primary = 0,
    Secondary = 1,
}
```

Rename these and add your own (`Jump`, `Fire`, `Interact`, ...). [Input](input.md#adding-a-game-action)
explains how to bind them. Menu actions such as pausing are *not* game actions, because the
simulation doesn't know about menus.

> `Vector2` here is `System.Numerics.Vector2`, not MonoGame's
> `Microsoft.Xna.Framework.Vector2`, because the simulation can't reference MonoGame. MonoGame
> converts `System.Numerics.Vector2` to its own `Vector2` implicitly, so views can pass simulation
> positions straight to `SpriteBatch`. In the other direction, call `.ToNumerics()`.

### `ICommandSource`

The runner asks each player's [`ICommandSource`](../CIS580/CIS580.Core/Simulation/ICommandSource.cs)
for a command once per tick. Different kinds of players are just different command sources:

| Source | Used for |
|---|---|
| [`LocalCommandSource`](../CIS580/CIS580.Core/Simulation/LocalCommandSource.cs) | A player on this machine. The game calls `Submit(command)` every frame. |
| [`DelegateCommandSource`](../CIS580/CIS580.Core/Simulation/DelegateCommandSource.cs) | A function of the tick number. Useful for simple AI and scripted tests. |
| *(your own)* | Replays (return recorded commands) or network players (return received commands) |

`LocalCommandSource` makes sure quick taps are never lost. Input is read once per frame, but a
frame can contain zero, one, or several ticks. An action pressed in *any* frame since the last
tick is included in the next tick's command, even if it was released before that tick ran.

### `SessionRunner`

[`SessionRunner`](../CIS580/CIS580.Core/Simulation/SessionRunner.cs) owns the fixed timestep:

```csharp
var session = new GameSession(seed);
var runner  = new SessionRunner(session, ticksPerSecond: 60);
runner.AddPlayer(new PlayerId(1), player.Commands);

// every frame:
runner.Update(gameTime.ElapsedGameTime);   // runs as many whole ticks as fit
```

| Member | Purpose |
|---|---|
| `Update(elapsed)` | Adds the frame's time to an accumulator and runs whole ticks; leftover time carries over |
| `Step()` | Runs exactly one tick. Use it in tests, or to step a paused game one tick at a time. |
| `Tick` | The number of the next tick to run |
| `MaxTicksPerUpdate` | Default 5. If the game falls far behind (e.g. paused in a debugger), the extra time is discarded instead of running hundreds of ticks at once. |
| `Alpha` | How far (0–1) the runner is between the last tick and the next. Use it to interpolate drawing (see [Views](views.md#smoother-motion-with-interpolation)). |
| `AddPlayer` / `RemovePlayer` | Take effect at the start of the **next** tick, so every machine in a networked game applies them on the same tick |

On each tick the runner applies any players joining or leaving, then collects one command per
player in join order, and passes them to the simulation as a
[`TickContext`](../CIS580/CIS580.Core/Simulation/TickContext.cs).

### `ISimulation`, `TickContext`, and `PlayerInput`

Your simulation implements [`ISimulation`](../CIS580/CIS580.Core/Simulation/ISimulation.cs):

```csharp
public interface ISimulation
{
    void AddPlayer(PlayerId player);
    void RemovePlayer(PlayerId player);
    void Tick(in TickContext context);
}
```

`TickContext` provides the tick number, `DeltaSeconds` (always the same value, 1/60 by default),
and one [`PlayerInput`](../CIS580/CIS580.Core/Simulation/PlayerInput.cs) per player.
`PlayerInput` pairs this tick's command with the previous tick's, so the simulation can detect
edges:

```csharp
if (input.WasPressed(GameAction.Primary))   { /* fire once per press */ }
if (input.IsDown(GameAction.Secondary))     { /* charge while held */ }
if (input.WasReleased(GameAction.Secondary)) { /* release the charge */ }
```

### `EventQueue<TEvent>`

The simulation reports what happened by raising events on an
[`EventQueue`](../CIS580/CIS580.Core/Simulation/EventQueue.cs). The client reads
`Events.Pending` after the runner updates, reacts to each event, and then calls `Clear()`.
`InGameState` does this, re-raising each event through its `GameEventRaised` C# event, which the
views, audio, and feedback subscribe to.

This game's events are records deriving from
[`GameEvent`](../MechaHorde.Simulation/GameEvents.cs):

```csharp
public abstract record GameEvent(long Tick);
public sealed record PlayerJoined(long Tick, PlayerId Player) : GameEvent(Tick);
public sealed record PlayerLeft(long Tick, PlayerId Player) : GameEvent(Tick);
```

Records give you value equality and a readable `ToString()` for free. Subscribers handle events
with a `switch` on the type (see `GameAudio.OnGameEvent`).

### `DeterministicRandom`

[`DeterministicRandom`](../CIS580/CIS580.Core/Simulation/DeterministicRandom.cs) is a PCG32
generator. Unlike `System.Random`, it gives the same sequence on every machine and .NET version,
and its entire state is one `ulong` (`State`), so you can save and restore it.

```csharp
int roll    = random.NextInt(1, 7);          // 1..6
float x     = random.NextFloat(0, World.Width);
bool crit   = random.Chance(0.1f);           // 10% of the time
```

The world owns one generator (`World.Random`), seeded from the session's seed.

## This game's simulation

| Type | Role |
|---|---|
| [`GameSession`](../MechaHorde.Simulation/GameSession.cs) | Implements `ISimulation`. Owns the `World` and the `Events` queue. Applies each player's input, then updates everything else. |
| [`World`](../MechaHorde.Simulation/World.cs) | Everything that exists: player avatars (in join order), the wanderer, and the random generator. Measured in **world units** (800 × 480), not pixels. |
| [`PlayerAvatar`](../MechaHorde.Simulation/PlayerAvatar.cs) | *Placeholder.* A position that moves at `Speed` in the direction of `Move`. |
| [`Wanderer`](../MechaHorde.Simulation/Wanderer.cs) | *Placeholder.* Jitters randomly around a home point, using `World.Random`. |
| [`GameAction`](../MechaHorde.Simulation/GameAction.cs) | *Placeholder.* The game's actions. |
| [`GameEvents`](../MechaHorde.Simulation/GameEvents.cs) | The events the simulation raises. |

`World.AddPlayer` and `World.RemovePlayer` are `internal`: only the session can change who is in
the world. Views get read-only access (`IReadOnlyList<PlayerAvatar> Players`, `FindPlayer`).
Keep to this pattern as you add entities: public getters, `internal` or `private` setters.

`PlayerAvatar.ApplyInput` shows a habit worth keeping: **never trust a command**. A command
could come from a modified client over a network, so the simulation clamps it (here, normalizing
`Move` to length 1 or less) instead of assuming the input layer did.

> The placeholder avatar is not kept inside the world; it can walk off the edge. Clamping its
> position (or adding walls) is a good first change.

## Rules for staying deterministic

These rules come from the comments on `ISimulation`. Break one and two runs of the same game can
drift apart, which won't matter until you try networking or replays, and then it will be very
hard to find.

1. Change simulation state **only inside** `Tick`, `AddPlayer`, and `RemovePlayer`.
2. Use `context.DeltaSeconds` for time, **never** a clock (`DateTime`, `Stopwatch`, `GameTime`).
3. Use the world's seeded `DeterministicRandom`, **never** `System.Random` or `Guid.NewGuid()`.
4. Read player input **only** from `context.Inputs`.
5. Iterate over **lists**, not dictionaries or hash sets, whenever the order affects the outcome.
6. Don't read the game's mutable settings during a tick. Copy what you need, such as the
   difficulty, into the session when it is created (see [Settings](settings.md#settings-and-the-simulation)).

Floating-point results can still differ slightly between CPU architectures. Networked games that
depend on determinism should run the same build on every machine.

`InGameState` seeds each new session from `Environment.TickCount64`, so each game is different.
To record a replay, save that seed along with every tick's commands.

## Extending the simulation

### Add an entity

1. Create a class in `MechaHorde.Simulation` with public getters and private or internal
   setters, plus an update method that takes `float deltaSeconds` (and the `World`, if it needs
   the random generator or other entities).
2. Store instances in a `List<T>` in `World`, exposed as `IReadOnlyList<T>`.
3. Update them from `GameSession.Tick`.
4. Draw them in `WorldView.DrawWorld` (see [Views](views.md)).

The [walkthrough](walkthrough.md) does exactly this with collectible coins.

### Raise an event

1. Add a record to `GameEvents.cs`, e.g.
   `public sealed record PlayerHit(long Tick, PlayerId Player, Vector2 Position) : GameEvent(Tick);`
2. Raise it from the simulation: `Events.Raise(new PlayerHit(context.Tick, id, position));`
3. React to it in any subscriber's `switch`: `GameAudio.OnGameEvent` for sound,
   `GameFeedback.OnGameEvent` for shake and rumble, and `WorldView`/`HudLayer` for visuals.

Include in each event everything a subscriber might need (who, where, how much), so subscribers
don't need to query the world for it.

### Give the session parameters

`GameSession` currently takes only a seed. When your game has options that affect play (a level,
the difficulty, the number of rounds), pass them into the constructor and store them, and create
the session with them in `InGameState.Load`. That keeps them fixed for the whole game, which
determinism requires.

### Add AI players

An AI player is just another command source:

```csharp
var bot = new DelegateCommandSource(tick =>
    new PlayerCommand { Move = new Vector2(MathF.Cos(tick * 0.02f), MathF.Sin(tick * 0.02f)) });
Runner.AddPlayer(new PlayerId(99), bot);
```

For AI that needs to see the world, write a class implementing `ICommandSource` that holds a
reference to the `World` and decides in `GetCommand`. It may only *read* the world.

### Test it

Because the simulation has no MonoGame dependencies, you can drive it directly in a unit test
with `DelegateCommandSource` and `runner.Step()`. See [Testing](testing.md#testing-the-simulation).
