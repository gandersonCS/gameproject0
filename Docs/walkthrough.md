# Walkthrough: Your First Feature

[← Overview](README.md)

This walkthrough adds **collectible coins**: gold squares that players walk over to score. It's a
small feature, but it passes through every part of the architecture, in the order you'd normally
work:

1. [the simulation](#1-the-simulation): a `Coin` entity, a score, and a `CoinCollected` event
2. [a test](#2-test-it): keep the simulation deterministic
3. [the view](#3-draw-the-coins): draw the coins
4. [the HUD](#4-show-the-score): show the score
5. [audio](#5-make-it-sound-good): play a sound when one is collected
6. [game feel](#6-make-it-feel-good): a little shake and rumble

The code below has been built and tested against this project. Run the game with
`dotnet run --project MechaHorde -- --play` at any point to see your progress.

## 1. The simulation

Everything about *what happens* (where coins are, who collected one, the score) belongs in the
simulation, in `MechaHorde.Simulation`. Remember the [rules](simulation.md#rules-for-staying-deterministic):
no MonoGame types, and randomness only from `World.Random`.

### The `Coin` entity

Create `MechaHorde.Simulation/Coin.cs`:

```csharp
using System.Numerics;

namespace MechaHorde.Simulation;

/// <summary>
/// A coin waiting to be collected
/// </summary>
public sealed class Coin
{
    /// <summary>
    /// How close (in world units) a player must get to collect the coin
    /// </summary>
    public const float PickupRadius = 24f;

    /// <summary>
    /// The coin's position in the world
    /// </summary>
    public Vector2 Position { get; }

    public Coin(Vector2 position) => Position = position;
}
```

`Vector2` is `System.Numerics.Vector2`. The simulation can't use MonoGame's.

### Coins in the world

In [`World.cs`](../MechaHorde.Simulation/World.cs), add a list of coins after the players
list. Expose it read-only, so only the simulation can change it:

```csharp
    /// <summary>
    /// How many coins are in the world at once
    /// </summary>
    public const int MaxCoins = 3;

    private readonly List<Coin> _coins = new();

    /// <summary>
    /// The coins waiting to be collected
    /// </summary>
    public IReadOnlyList<Coin> Coins => _coins;
```

and, after `RemovePlayer`, methods for adding and removing them:

```csharp
    internal void SpawnCoin()
    {
        // A random position, away from the edges, using the world's deterministic random numbers
        var position = new Vector2(Random.NextFloat(32, Width - 32), Random.NextFloat(32, Height - 32));
        _coins.Add(new Coin(position));
    }

    internal void RemoveCoin(Coin coin) => _coins.Remove(coin);
```

`internal` means only code in the simulation project can call them. A view could never
accidentally spawn a coin.

### A score

In [`PlayerAvatar.cs`](../MechaHorde.Simulation/PlayerAvatar.cs), below `Position`:

```csharp
    /// <summary>
    /// How many coins the player has collected
    /// </summary>
    public int Score { get; internal set; }
```

### An event

The simulation can't play a sound or shake the screen, so it *reports* the collection and lets
the client react. In [`GameEvents.cs`](../MechaHorde.Simulation/GameEvents.cs), add
`using System.Numerics;` at the top and this record at the bottom:

```csharp
/// <summary>
/// A player collected a coin
/// </summary>
public sealed record CoinCollected(long Tick, PlayerId Player, Vector2 Position) : GameEvent(Tick);
```

The event includes the position so subscribers can use it (to pan the sound, for example)
without looking anything up.

### The rules

In [`GameSession.cs`](../MechaHorde.Simulation/GameSession.cs), add
`using System.Numerics;` at the top. In `Tick`, after the players have moved and before the
wanderer updates, collect and replace coins:

```csharp
        foreach (var input in context.Inputs)
            World.FindPlayer(input.Player)?.ApplyInput(input, dt);

        CollectCoins(context.Tick);
        while (World.Coins.Count < World.MaxCoins)
            World.SpawnCoin();

        World.Wanderer.Update(World, dt);
```

and add the method:

```csharp
    /// <summary>
    /// Gives each player the coins they are touching
    /// </summary>
    private void CollectCoins(long tick)
    {
        foreach (var avatar in World.Players)
        {
            // Loop backward, so removing a coin doesn't skip the one after it
            for (int i = World.Coins.Count - 1; i >= 0; i--)
            {
                var coin = World.Coins[i];
                if (Vector2.Distance(avatar.Position, coin.Position) <= Coin.PickupRadius)
                {
                    World.RemoveCoin(coin);
                    avatar.Score++;
                    Events.Raise(new CoinCollected(tick, avatar.Owner, coin.Position));
                }
            }
        }
    }
```

Players are checked in join order (a list, not a dictionary). If two players reach a coin on the
same tick, the earlier one always wins, on every machine.

## 2. Test it

In [`GameSessionTests.cs`](../Tests/MechaHorde.Tests/GameSessionTests.cs), add a test that
coins are always available. It uses the file's existing `Start`, `One`, and `Circling` helpers:

```csharp
    [Fact]
    public void ThereAreAlwaysCoinsToCollect()
    {
        var (session, runner) = Start(seed: 7);
        runner.AddPlayer(One, Circling(0.05f));

        for (int i = 0; i < 600; i++)
        {
            runner.Step();
            Assert.Equal(World.MaxCoins, session.World.Coins.Count);
        }
    }
```

Then extend the existing determinism test, `SameSeedAndCommands_PlayOutIdentically`, to compare
the new state too (add `using System.Linq;` at the top):

```csharp
        Assert.Equal(a.World.FindPlayer(One).Score, b.World.FindPlayer(One).Score);
        Assert.Equal(a.World.Coins.Select(c => c.Position), b.World.Coins.Select(c => c.Position));
```

Run `dotnet test MechaHorde.slnx`. If you had used `System.Random` in `SpawnCoin`, the
determinism test would now fail, which is exactly what it's there for.

## 3. Draw the coins

The rest happens in the game client. In [`WorldView.DrawWorld`](../MechaHorde/Views/WorldView.cs),
after `var world = _session.World;`:

```csharp
        // Coins are gold squares, centered on their positions
        foreach (var coin in world.Coins)
        {
            var bounds = new Rectangle((int)coin.Position.X - 8, (int)coin.Position.Y - 8, 16, 16);
            SpriteBatch.Draw(Stack.BlankTexture, bounds, Color.Gold);
        }
```

The coordinates are **world units**; the camera scales them to the window. `Stack.BlankTexture`
is a 1×1 white pixel, which is handy until you have art. To use a sprite instead, see
[Views](views.md#drawing-sprites).

Run the game. Three gold squares appear, and walking `// TODO` (its top-left corner is its
position) onto one makes it vanish and a new one appear elsewhere.

## 4. Show the score

The HUD needs to read the scores, so give it the session. In
[`HudLayer.cs`](../MechaHorde/Views/HudLayer.cs), add a field and a constructor parameter:

```csharp
    private readonly IReadOnlyList<LocalPlayer> _players;
    private readonly GameSession _session;

    /// <param name="players">The players on this machine</param>
    public HudLayer(IReadOnlyList<LocalPlayer> players, GameSession session)
    {
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        IsPopup = true;
        InputMode = InputMode.None;
    }
```

In `Draw`, before the join and leave messages:

```csharp
        // Each player's score, in the top left
        position = new Vector2(margin, margin);
        foreach (var player in _players)
        {
            int score = _session.World.FindPlayer(player.Id)?.Score ?? 0;
            string label = IsMultiplayer ? $"Player {player.Id.Value}: {score}" : $"Score: {score}";
            var color = IsMultiplayer ? PlayerColors.For(player.Id) : Theme.TextColor;
            SpriteBatch.DrawString(font, label, position, color * alpha);
            position.Y += font.LineSpacing;
        }
```

A single player sees just "Score: 3", with no "Player 1"; single-player games shouldn't show
multiplayer details.

Finally, pass the session where the HUD is created, in
[`InGameState.Enter`](../MechaHorde/States/InGameState.cs):

```csharp
        var hud = Show(new HudLayer(_players, Session));
```

## 5. Make it sound good

In [`GameAudio.cs`](../MechaHorde/Audio/GameAudio.cs), add a cue field and create it in the
constructor. Gameplay sounds go on the **Effects** bus, so they pause with the game:

```csharp
    private readonly SoundCue _coinCollected;

    // in the constructor:
        // Until you have a coin sound, the join sound stands in
        _coinCollected = new SoundCue(audio.Effects, content.Load<SoundEffect>("Audio/Sfx/player_join"))
        {
            PitchVariation = 0.1f,
            Cooldown = TimeSpan.FromMilliseconds(30),
        };
```

`PitchVariation` keeps repeated pickups from sounding identical, and `Cooldown` merges several
pickups on the same frame into one sound.

Then play it in `OnGameEvent`:

```csharp
            case CoinCollected coin:
                _coinCollected.Play(pan: PanFor(coin.Position));
                break;
```

When you have a real sound, put `coin.wav` in `MechaHorde.Content/Assets/Audio/Sfx/`, load
`"Audio/Sfx/coin"` instead, and add it to [`ASSETS.md`](../ASSETS.md). Better still, pass
two or three recordings to the cue as variations (see [Audio](audio.md#soundcue)).

## 6. Make it feel good

In [`GameFeedback.OnGameEvent`](../MechaHorde/Feedback/GameFeedback.cs), add a light shake
and a short buzz for the player who collected it:

```csharp
            case CoinCollected coin:
                _world.Shake(0.15f);
                if (PlayerFor(coin.Player) is { } collector)
                    _rumble.Play(collector, 0f, 0.3f, TimeSpan.FromMilliseconds(60));
                break;
```

Keep it subtle: coins are frequent, and a big shake every time would get tiresome. Save heavier
effects (and `_hitStop`) for rarer, bigger moments. See [Game Feel](feedback.md#tuning-tips).

## What you've touched

| Concern | Change | Project |
|---|---|---|
| Simulation | `Coin`, `World.Coins`, `PlayerAvatar.Score`, `CoinCollected`, `GameSession.CollectCoins` | `MechaHorde.Simulation` |
| Tests | coins always available; determinism covers the new state | `MechaHorde.Tests` |
| View | coins drawn in `WorldView` | `MechaHorde` |
| UI | score in `HudLayer` | `MechaHorde` |
| Audio | `CoinCollected` cue in `GameAudio` | `MechaHorde` |
| Game feel | shake and rumble in `GameFeedback` | `MechaHorde` |

Notice what you **didn't** touch: the library (`CIS580/`), the input system (walking was already
a command), and the app flow. The simulation knows nothing about sound, shake, or the HUD, and
each of those reacts to one event independently. Adding a particle burst later is one more
subscriber, with no change to the simulation.

## Where to go next

- **Keep the avatar in the world.** It can currently walk off the edge. Clamp its position in
  `PlayerAvatar.ApplyInput` ([Simulation](simulation.md)).
- **End the game** at, say, 10 coins: raise a `GameOver` event and switch to a game-over screen
  ([App Flow](flow.md#adding-a-state), [UI](ui.md#adding-a-menu)).
- **Use a game action.** Make `Primary` do something (a dash?) with `input.WasPressed(GameAction.Primary)`,
  and rename it ([Input](input.md#adding-a-game-action)).
- **Real art.** Replace the text and squares with sprites ([Views](views.md#drawing-sprites),
  [Content](content.md)).
- **Split screen.** Give each local player a camera that follows them
  ([Views](views.md#split-screen)).
- **Difficulty that matters.** Pass `GameSettings.Difficulty` into the session when it starts
  ([Settings](settings.md#settings-and-the-simulation)).
