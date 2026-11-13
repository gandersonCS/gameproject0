using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MechaHorde.Gameplay;

/// <summary>
/// The level being played: the player, their gun, the enemies, the scrolling parallax level and the rain.
/// </summary>
/// <remarks>
/// <para>
/// This is the gameplay from the original GameProject0Game (everything that ran once
/// "started" was true), moved into its own class so the architecture's InGameState can run it.
/// The InGameState updates it (only while the game isn't paused), and the LevelView draws it.
/// </para>
/// <para>
/// Everything in the level is measured in level pixels. How big a level pixel is on screen
/// (<see cref="PixelScale"/>) depends on the screen: the camera always shows about
/// <see cref="ViewHeight"/> level pixels from top to bottom, so the player, the robots and the
/// level take up the same share of the screen at any resolution. The scale is rounded to a
/// whole number of screen pixels so the pixel art stays crisp.
/// </para>
/// <para>
/// The ground the player and the walking robots stand on is <see cref="Enemy.GroundY"/>. It
/// sits <see cref="FloorFraction"/> of the way up from the bottom of the screen; the floor is
/// drawn from there down, and every background layer stands on that same line.
/// </para>
/// </remarks>
public class Battlefield
{
    /// <summary>
    /// About how many level pixels tall the view is. Smaller zooms in (everything bigger).
    /// The player is about 16 level pixels tall, so this makes her about a sixth of the screen.
    /// </summary>
    public const float ViewHeight = 84f;

    /// <summary>
    /// The share of the screen's height, at the bottom, taken up by the floor
    /// </summary>
    public const float FloorFraction = 0.14f;

    /// <summary>
    /// How quickly the camera catches up with the player (higher is tighter)
    /// </summary>
    private const float CameraFollowSharpness = 7f;

    /// <summary>
    /// How far ahead of the player the camera looks when they run at full speed (level pixels)
    /// </summary>
    private const float CameraLookAhead = 18f;

    /// <summary>
    /// The player can't go past this X coordinate (in level pixels)
    /// </summary>
    private const float LevelWidth = 1000f;

    /// <summary>
    /// The floor's paving slabs are this wide (level pixels)
    /// </summary>
    private const float FloorSlabWidth = 24f;

    // The parallax layers, from the back. Each has its own pixel size (a fraction of the level's,
    // so further layers look further away), how fast it scrolls compared with the level
    // (its parallax), and a tint. Every layer stands on the ground line.
    //
    // The look: a red dusk. The tints are soft so the art keeps its own neon blues and pinks
    // (a pure red tint turns everything one flat red), warming the sky and darkening the
    // layers nearer the player, so the player and the robots stand out against them.
    private static readonly ParallaxLayer SkyLayer = new(PixelSize: 0f, Parallax: 0.1f, Tint: new Color(255, 180, 160));
    private static readonly ParallaxLayer CityLayer = new(PixelSize: 0.3f, Parallax: 0.3f, Tint: new Color(125, 60, 80));
    private static readonly ParallaxLayer BuildingsLayer = new(PixelSize: 0.4f, Parallax: 0.6f, Tint: new Color(110, 78, 100));

    /// <summary>
    /// The red haze rising from the horizon, between the far city and the buildings: it gives
    /// the scene depth and ties the layers together
    /// </summary>
    private static readonly Color HazeColor = new(160, 30, 40);
    private const float HazeOpacity = 0.67f;
    /// <summary>How high the haze rises, as a share of the height above the ground</summary>
    private const float HazeHeight = 0.6f;

    /// <summary>
    /// The row of the sky texture where its sea meets the city on the horizon; it is lined up
    /// with the ground (the floor hides the sea)
    /// </summary>
    private const int SkyHorizonRow = 190;

    private static readonly Color FloorColor = new(30, 18, 26);
    private static readonly Color FloorEdgeColor = new(170, 70, 80);
    private static readonly Color FloorLipColor = new(70, 32, 42);
    private static readonly Color FloorSeamColor = new(20, 10, 16);
    private static readonly Color SkyColor = new(18, 8, 22);
    private static readonly Color ShadowColor = Color.Black * 0.45f;

    private readonly Game _game;
    private readonly LevelCamera _camera = new();
    private readonly Player _player = new();
    private Gun _playerGun;
    private readonly EnemySpawner _spawner = new(0f, LevelWidth);
    private RainParticleSystem _rain;

    private Texture2D _pixel;
    private Texture2D _haze;
    private Texture2D _foreground;
    private Texture2D _midground;
    private Texture2D _background;

    private float gameTimer = 0f;
    private bool _cameraPlaced;

    public Battlefield(Game game)
    {
        _game = game;
    }

    /// <summary>
    /// The player
    /// </summary>
    public Player Player => _player;

    /// <summary>
    /// Sends in the enemies, in waves
    /// </summary>
    public EnemySpawner Spawner => _spawner;

    /// <summary>
    /// The time shown on the HUD
    /// </summary>
    public float GameTimer => gameTimer;

    /// <summary>
    /// How many screen pixels one level pixel covers on a screen this tall: a whole number,
    /// so the pixel art stays crisp, chosen so the view is about <see cref="ViewHeight"/>
    /// level pixels tall
    /// </summary>
    public static float PixelScale(int screenHeight) => Math.Max(1f, MathF.Round(screenHeight / ViewHeight));

    /// <summary>
    /// Loads the level's content
    /// </summary>
    /// <param name="content">Loads the level's content (and unloads it when the game ends)</param>
    public void LoadContent(ContentManager content)
    {
        _player.LoadContent(content);
        _spawner.LoadContent(content);
        _foreground = content.Load<Texture2D>("Textures/Level/foreground");
        _midground = content.Load<Texture2D>("Textures/Level/midground");
        _background = content.Load<Texture2D>("Textures/Level/background");

        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _haze = CreateHaze(_game.GraphicsDevice);
        _playerGun = new Gun(_pixel);

        // The rain falls across the whole screen, in UI units (a screen 1080 tall), so it looks
        // the same at any resolution. It starts left of the screen to allow for the wind, and
        // is wide enough for any screen shape. (It is updated and drawn with the level, so it
        // pauses with the game and goes away when it ends.)
        _rain = new RainParticleSystem(_game, new Rectangle(-500, -20, 6000, 10));
        _rain.Initialize();
    }

    /// <summary>
    /// Releases what the level created itself (its content is unloaded by its ContentManager)
    /// </summary>
    public void Unload()
    {
        _pixel?.Dispose();
        _haze?.Dispose();
        _rain?.Dispose();
    }

    /// <summary>
    /// Updates the level
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="viewport">The screen the level is drawn on</param>
    public void Update(GameTime gameTime, Viewport viewport)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var view = View(viewport);

        // Convert screen mouse coordinates into level coordinates (using last frame's camera,
        // which is what's on screen right now)
        MouseState mouse = Mouse.GetState();
        Vector2 worldMousePosition = view.ScreenToLevel(new Vector2(mouse.X, mouse.Y));

        // 1. Move the player, aiming at the mouse, and keep them inside the level
        _player.Update(gameTime, worldMousePosition);
        _player.ClampX(16f, LevelWidth);

        // 2. Fire from Sara's gun, towards the mouse
        if (_playerGun.Update(gameTime, _player.MuzzlePosition, _player.AimDirection))
            _player.NotifyShot();

        // 3. The camera eases towards the player, looking ahead in the direction they run,
        // and never scrolls past the level's edges
        float halfViewWidth = viewport.Width / 2f / view.Scale;
        float minCameraX = halfViewWidth;
        float maxCameraX = Math.Max(minCameraX, LevelWidth - halfViewWidth);

        float targetX = _player.position.X + _player.Velocity.X / 120f * CameraLookAhead;
        float cameraX = _cameraPlaced
            ? MathHelper.Lerp(_camera.Position.X, targetX, 1f - MathF.Exp(-CameraFollowSharpness * dt))
            : targetX;
        _cameraPlaced = true;

        _camera.Position = new Vector2(MathHelper.Clamp(cameraX, minCameraX, maxCameraX), Enemy.GroundY);

        // The enemies: spawned just off the screen's edges, then they come for the player
        _spawner.Update(gameTime, _player,
            _camera.Position.X - halfViewWidth, _camera.Position.X + halfViewWidth,
            _playerGun.ActiveBullets);

        // When the player has health, damage it here, e.g.:
        // foreach (var enemy in _spawner.EnemiesTouching(_player.Bounds)) { ... }

        _rain.Update(gameTime);
    }

    /// <summary>
    /// Draws the level
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">A SpriteBatch that has not been begun</param>
    /// <param name="viewport">The screen to draw on</param>
    /// <param name="shake">How far the screen is shaken (from the architecture's screen shake)</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Viewport viewport, Vector2 shake)
    {
        var view = View(viewport, shake);
        int screenWidth = viewport.Width;

        // BATCH 1: The parallax layers, back to front, all standing on the ground line.
        // (PointWrap repeats each layer's art sideways, however far the level scrolls.)
        spriteBatch.Begin(samplerState: SamplerState.PointWrap);

        // The sky: big enough to fill the screen above the ground, with its horizon on the ground
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, (int)view.Ground + 1), SkyColor);
        float skyScale = MathF.Max(1f, MathF.Ceiling(view.Ground / (SkyHorizonRow - 12)));
        DrawLayer(spriteBatch, _background, SkyLayer, skyScale, view, screenWidth, SkyHorizonRow);

        DrawLayer(spriteBatch, _midground, CityLayer, LayerScale(CityLayer, view), view, screenWidth, _midground.Height);

        // The haze, rising from the horizon behind the buildings
        int hazeHeight = (int)(view.Ground * HazeHeight);
        spriteBatch.Draw(_haze, new Rectangle(0, (int)(view.Ground + view.ShakeY) - hazeHeight, screenWidth, hazeHeight),
            new Rectangle(0, 0, 1, _haze.Height), HazeColor * HazeOpacity);

        DrawLayer(spriteBatch, _foreground, BuildingsLayer, LayerScale(BuildingsLayer, view), view, screenWidth, _foreground.Height);

        // The floor, from the ground line to the bottom of the screen
        DrawFloor(spriteBatch, view, viewport);
        spriteBatch.End();

        // BATCH 2: Enemies, the player, and bullets, in level pixels
        Matrix levelTransform = Matrix.CreateScale(view.Scale, view.Scale, 1) * Matrix.CreateTranslation(view.OffsetX, view.OffsetY, 0);
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            depthStencilState: null,
            rasterizerState: null,
            effect: null,
            transformMatrix: levelTransform
        );
        DrawShadows(spriteBatch);
        _spawner.Draw(gameTime, spriteBatch);
        _player.Draw(gameTime, spriteBatch, view.Scale);
        _playerGun.Draw(spriteBatch);
        spriteBatch.End();

        // The rain, over the level, sized to the screen
        _rain.Transform = Matrix.CreateScale(viewport.Height / 1080f, viewport.Height / 1080f, 1f);
        _rain.Draw(gameTime);
    }

    /// <summary>
    /// Soft shadows on the floor under the player and the walking robots, so they look like
    /// they stand on it. The player's shrinks as she jumps.
    /// </summary>
    private void DrawShadows(SpriteBatch spriteBatch)
    {
        float height = Enemy.GroundY - _player.position.Y;
        float shrink = MathHelper.Clamp(1f - height / 40f, 0.3f, 1f);
        DrawShadow(spriteBatch, _player.position.X, 12f * shrink, shrink);

        foreach (var enemy in _spawner.Enemies)
        {
            if (enemy.IsFlying)
                continue;
            float width = enemy.EnemyType == Enemy.Type.heavy ? 24f : 12f;
            DrawShadow(spriteBatch, enemy.Position.X, width, 1f);
        }
    }

    /// <summary>
    /// A shadow on the ground: a wide strip with a narrower one beneath it, so it reads as an oval
    /// </summary>
    private void DrawShadow(SpriteBatch spriteBatch, float x, float width, float strength)
    {
        var color = ShadowColor * strength;
        spriteBatch.Draw(_pixel, new Vector2(x - width / 2f, Enemy.GroundY), null, color, 0f, Vector2.Zero,
            new Vector2(width, 1f), SpriteEffects.None, 0f);
        spriteBatch.Draw(_pixel, new Vector2(x - width / 3f, Enemy.GroundY + 1f), null, color * 0.6f, 0f, Vector2.Zero,
            new Vector2(width * 2f / 3f, 1f), SpriteEffects.None, 0f);
    }

    /// <summary>
    /// A 1-pixel-wide strip, opaque at the bottom and fading out towards the top, for the haze
    /// </summary>
    private static Texture2D CreateHaze(GraphicsDevice graphics)
    {
        const int height = 256;
        var data = new Color[height];
        for (int y = 0; y < height; y++)
        {
            float t = y / (height - 1f);            // 0 at the top, 1 at the bottom
            data[y] = Color.White * MathF.Pow(t, 1.5f);
        }
        var texture = new Texture2D(graphics, 1, height);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// The size of a layer's pixels on screen: a whole number, a fraction of the level's
    /// </summary>
    private static float LayerScale(ParallaxLayer layer, ScreenLayout view) => MathF.Max(1f, MathF.Round(view.Scale * layer.PixelSize));

    /// <summary>
    /// Draws a parallax layer across the whole screen, repeating sideways, with a row of its
    /// texture (normally its bottom) on the ground line
    /// </summary>
    /// <param name="groundRow">The row of the texture that sits on the ground</param>
    private static void DrawLayer(SpriteBatch spriteBatch, Texture2D texture, ParallaxLayer layer, float scale,
        ScreenLayout view, int screenWidth, int groundRow)
    {
        // How far the layer has scrolled, in screen pixels: a share of how far the camera has
        float scroll = -view.OffsetX * layer.Parallax;
        int sourceX = (int)MathF.Floor(scroll / scale);
        float leftover = scroll - sourceX * scale;
        int sourceWidth = (int)MathF.Ceiling((screenWidth + leftover) / scale) + 1;

        var position = new Vector2(MathF.Round(-leftover), MathF.Round(view.Ground - groundRow * scale + view.ShakeY));
        spriteBatch.Draw(texture, position, new Rectangle(sourceX, 0, sourceWidth, texture.Height), layer.Tint,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Draws the floor: a lit edge along the ground line, then paving slabs that scroll with
    /// the level down to the bottom of the screen
    /// </summary>
    private void DrawFloor(SpriteBatch spriteBatch, ScreenLayout view, Viewport viewport)
    {
        int s = (int)view.Scale;
        int top = (int)view.Ground + (int)view.ShakeY;
        int width = viewport.Width;
        int bottom = viewport.Height + s * 2; // past the bottom, so shaking never shows a gap

        spriteBatch.Draw(_pixel, new Rectangle(0, top, width, bottom - top), FloorColor);
        spriteBatch.Draw(_pixel, new Rectangle(0, top, width, s), FloorEdgeColor);
        spriteBatch.Draw(_pixel, new Rectangle(0, top + s, width, s), FloorLipColor);

        // The seams between the slabs, every FloorSlabWidth level pixels
        float leftLevelX = view.ScreenToLevel(Vector2.Zero).X;
        float firstSeam = MathF.Floor(leftLevelX / FloorSlabWidth) * FloorSlabWidth;
        for (float x = firstSeam; ; x += FloorSlabWidth)
        {
            int screenX = (int)view.LevelToScreen(new Vector2(x, 0)).X;
            if (screenX > width)
                break;
            spriteBatch.Draw(_pixel, new Rectangle(screenX, top + s * 2, s, bottom - top - s * 2), FloorSeamColor);
        }
    }

    /// <summary>
    /// Where the level is on screen this frame. Update and Draw share this so the mouse aims
    /// where things are drawn.
    /// </summary>
    private ScreenLayout View(Viewport viewport, Vector2 shake = default)
    {
        float scale = PixelScale(viewport.Height);
        float ground = MathF.Round(viewport.Height * (1f - FloorFraction));

        // Rounded to whole screen pixels, so the pixel art doesn't shimmer as the camera moves
        // (the scale is a whole number, so the ground line lands exactly on a screen pixel)
        float shakeY = MathF.Round(shake.Y);
        float offsetX = MathF.Round(viewport.Width / 2f - _camera.Position.X * scale + shake.X);
        float offsetY = ground - Enemy.GroundY * scale + shakeY;
        return new ScreenLayout(scale, offsetX, offsetY, ground, shakeY);
    }

    /// <summary>
    /// Where the level is drawn on screen
    /// </summary>
    /// <param name="Scale">Screen pixels per level pixel</param>
    /// <param name="OffsetX">The screen X of level X 0</param>
    /// <param name="OffsetY">The screen Y of level Y 0</param>
    /// <param name="Ground">The screen Y of the ground line (without shake)</param>
    /// <param name="ShakeY">How far the screen is shaken vertically</param>
    private readonly record struct ScreenLayout(float Scale, float OffsetX, float OffsetY, float Ground, float ShakeY)
    {
        public Vector2 ScreenToLevel(Vector2 screen) => new((screen.X - OffsetX) / Scale, (screen.Y - OffsetY) / Scale);

        public Vector2 LevelToScreen(Vector2 level) => new(level.X * Scale + OffsetX, level.Y * Scale + OffsetY);
    }

    /// <param name="PixelSize">The size of the layer's pixels, as a fraction of the level's</param>
    /// <param name="Parallax">How fast the layer scrolls, as a fraction of the level's speed</param>
    /// <param name="Tint">The color the layer is tinted</param>
    private readonly record struct ParallaxLayer(float PixelSize, float Parallax, Color Tint);
}
