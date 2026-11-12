# Views & Cameras

[← Overview](README.md)

A **view** draws the simulation. It reads the `GameSession` every frame and never changes it.
The simulation measures everything in **world units**; a **camera** decides which part of the
world appears where on screen, and at what size. Keeping these apart is what lets the game fit
any window size, scroll, zoom, shake, or split the screen without the simulation knowing.

The library code is in `CIS580.MonoGame/Views/`, and the game's views are in `MechaHorde/Views/`.

## World units vs. pixels

`World.Width` and `World.Height` (800 × 480) are **world units**, not pixels. `WorldView` fits
that area to the window with `Camera2D.FitToArea`, so it scales with the window and is
letterboxed when the shapes differ. Positions in the simulation, such as `PlayerAvatar.Position`
and `PlayerAvatar.Speed`, are all in world units. Choose a unit that suits your game (e.g. one
unit per tile, or per meter) and let the camera handle the scaling.

## `Camera2D`

[`Camera2D`](../CIS580/CIS580.MonoGame/Views/Camera2D.cs):

| Member | Meaning |
|---|---|
| `Position` | The world point shown at the **center** of the viewport |
| `Zoom` | Pixels per world unit |
| `Rotation` | Radians |
| `Viewport` | The rectangle of the screen it draws into, in pixels (set by the layout) |
| `Shake` | A [`ScreenShake`](feedback.md#screenshake) applied to this camera's view |
| `Transform` | A matrix for `SpriteBatch.Begin(transformMatrix: ...)`, including the shake |
| `WorldToScreen(world)` | Where a world point appears on screen (e.g. for a label) |
| `ScreenToWorld(screen)` | Which world point is under a screen point (e.g. the mouse) |
| `FitToArea(RectangleF area)` | Centers on and zooms to show the whole area |
| `Follow(target, stiffness, dt)` | Moves smoothly toward a target. The easing is frame-rate independent. |

[`RectangleF`](../CIS580/CIS580.MonoGame/Views/Camera2D.cs) is a float rectangle for areas of the world.

## `ViewLayout`

A [`ViewLayout`](../CIS580/CIS580.MonoGame/Views/ViewLayout.cs) divides the screen among the cameras:

- **`SharedLayout`** (default): every camera uses the whole screen. Use it with one camera for
  single-player games, and for multiplayer games where everyone shares a view.
- **`SplitLayout`**: each camera gets its own area. Two views go side by side (or stacked, with
  `StackTwoViews = true`); three or four go in a grid.

## `WorldLayer`

[`WorldLayer`](../CIS580/CIS580.MonoGame/Views/WorldLayer.cs) is an abstract
[layer](ui.md) that draws the world through its cameras. It is a backdrop (it stays visible
behind menus opened over the game) and takes no input.

| Member | Purpose |
|---|---|
| `Cameras` | One camera to begin with; add one per view |
| `Layout` | Places the cameras on screen |
| `BackgroundColor` | Fills each view before the world is drawn |
| `UpdateCameras(gameTime)` | **Override** to aim the cameras. Their viewports are already arranged. |
| `DrawWorld(camera, gameTime)` | **Override** to draw the world in world units. Called once per camera. |
| `Shake(trauma)` | Shakes every camera |

For each camera, `Draw` sets the graphics viewport to the camera's area, fills the background,
and calls `DrawWorld` inside `SpriteBatch.Begin(transformMatrix: camera.Transform)`. So the same
`DrawWorld` code works for one shared view or for split screen. Afterward it restores the full
viewport, and fades from black as the layer transitions.

Inside `DrawWorld`, the `SpriteBatch` is **already begun**. Just call `SpriteBatch.Draw` and
`DrawString` with world positions.

## This game's views

- [`WorldView`](../MechaHorde/Views/WorldView.cs): *placeholder* drawing. A cornflower-blue
  world rectangle, each avatar as the text `// TODO` in its player color, and the wanderer. It
  subscribes to `PlayerJoined` events to draw a spawn flash.
- [`HudLayer`](../MechaHorde/Views/HudLayer.cs): *placeholder* screen-space HUD (see
  [UI](ui.md#changing-the-hud)).
- [`PlayerColors`](../MechaHorde/Views/PlayerColors.cs): the color identifying each
  player, used by the world, the HUD, and the lobby, so people can tell who is who everywhere.

## Extending views

### Drawing sprites

Load gameplay textures in `InGameState.Load()` with its own `_content` manager, so they're
unloaded when the game ends. Then pass them to the view:

```csharp
// InGameState.Load()
_playerTexture = _content.Load<Texture2D>("Textures/player");

// InGameState.Enter()
_worldView = Show(new WorldView(Session, _gameFont, _playerTexture));

// WorldView.DrawWorld
foreach (var player in _session.World.Players)
{
    var origin = new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f);
    SpriteBatch.Draw(_playerTexture, player.Position, null, PlayerColors.For(player.Owner),
        0f, origin, 1f, SpriteEffects.None, 0f);
}
```

Sizes are in world units: a 32-pixel sprite drawn at scale 1 is 32 world units across, and the
camera's zoom scales it from there.

Content that lives as long as the game (menu art) can use `Game.Content` instead, as
`BackgroundLayer` does. See [Content](content.md) for adding the texture itself.

### A camera that follows the player

```csharp
protected override void UpdateCameras(GameTime gameTime)
{
    var camera = Cameras[0];
    camera.Zoom = 2f;   // two pixels per world unit
    var avatar = _session.World.Players.Count > 0 ? _session.World.Players[0] : null;
    if (avatar != null)
        camera.Follow(avatar.Position, stiffness: 5f, (float)gameTime.ElapsedGameTime.TotalSeconds);
}
```

Higher stiffness catches up faster. To keep the camera inside the world, clamp `Position` so half
the viewport (`camera.Viewport.Width / 2f / camera.Zoom`) never goes past an edge.

### Split screen

Give each local player a camera and use a `SplitLayout`. `WorldView` needs to know about the
players, so pass them in:

```csharp
public WorldView(GameSession session, SpriteFont font, IReadOnlyList<LocalPlayer> players)
{
    _session = session;
    _font = font;
    _players = players;

    if (players.Count > 1)
    {
        Layout = new SplitLayout();
        Cameras.Clear();
        foreach (var _ in players)
            Cameras.Add(new Camera2D { Zoom = 1.5f });
    }
}

protected override void UpdateCameras(GameTime gameTime)
{
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (Cameras.Count == 1)
    {
        Cameras[0].FitToArea(WorldArea);
        return;
    }
    for (int i = 0; i < Cameras.Count; i++)
    {
        var avatar = _session.World.FindPlayer(_players[i].Id);
        if (avatar != null)
            Cameras[i].Follow(avatar.Position, 5f, dt);
    }
}
```

and in `InGameState.Enter`: `Show(new WorldView(Session, _gameFont, _players))`. `DrawWorld`
doesn't change; it is simply called once per camera. Each camera has its own `Shake`, so one
player's view can shake on its own (`Cameras[i].Shake.Add(0.4f)`).

The HUD is drawn once over the whole screen. For per-player HUDs in split screen, draw each
player's HUD inside that player's camera `Viewport`.

### Smoother motion with interpolation

The simulation runs at 60 ticks per second, but the screen may refresh faster (or at a rate that
doesn't divide evenly), so motion can look slightly uneven. The fix is to draw each entity
*between* its last two tick positions:

1. In the simulation, keep `PreviousPosition` alongside `Position`, setting it at the start of
   each tick.
2. Give the view access to `SessionRunner.Alpha` (0–1, how far the runner is toward the next tick).
3. Draw at `Vector2.Lerp(previous, current, alpha)`.

This is not implemented yet, and it's a worthwhile improvement once your game has fast-moving
objects.

### Mouse picking

`camera.ScreenToWorld(Input.Current.Mouse.Position.ToVector2())` gives the world point under the
mouse. Remember the simulation only sees commands: turn a click into a command (or a direction in
`PlayerCommand.Look`; see [Input](input.md#aiming-with-the-mouse)) rather than changing the world
from the view.
