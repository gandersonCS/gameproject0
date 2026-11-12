using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MechaHorde.Gameplay;

/// <summary>
/// The level being played: the player, their gun, the scrolling parallax level and the rain.
/// </summary>
/// <remarks>
/// This is the gameplay from the original GameProject0Game (everything that ran once
/// "started" was true), moved into its own class so the architecture's InGameState can run it.
/// The InGameState updates it (only while the game isn't paused), and the LevelView draws it.
/// The gameplay itself is unchanged.
/// </remarks>
public class Battlefield
{
    /// <summary>
    /// The level and its sprites are drawn this many times their size
    /// </summary>
    private const float Scale = 6.0f;

    /// <summary>
    /// The player can't go past this X coordinate (in level pixels)
    /// </summary>
    private const float LevelWidth = 1000f;

    private readonly Game _game;
    private readonly LevelCamera _camera = new();
    private readonly Player _player = new();
    private Gun _playerGun;
    private RainParticleSystem _rain;

    private Texture2D _pixel;
    private Texture2D _foreground;
    private Texture2D _midground;
    private Texture2D _background;

    private float gameTimer = 0f;

    public Battlefield(Game game)
    {
        _game = game;
    }

    /// <summary>
    /// The player
    /// </summary>
    public Player Player => _player;

    /// <summary>
    /// The time shown on the HUD
    /// </summary>
    public float GameTimer => gameTimer;

    /// <summary>
    /// Loads the level's content
    /// </summary>
    /// <param name="content">Loads the level's content (and unloads it when the game ends)</param>
    public void LoadContent(ContentManager content)
    {
        _player.LoadContent(content);
        _foreground = content.Load<Texture2D>("Textures/Level/foreground");
        _midground = content.Load<Texture2D>("Textures/Level/midground");
        _background = content.Load<Texture2D>("Textures/Level/background");

        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _playerGun = new Gun(_pixel);

        // Start spawning rain at -500 to account for the diagonal wind, and make it wide enough
        // to cover the whole level. (It used to be added to Game.Components; it is now updated
        // and drawn with the level, so it pauses with the game and goes away when it ends.)
        _rain = new RainParticleSystem(_game, new Rectangle(-500, -20, 6000, 10));
        _rain.Initialize();
    }

    /// <summary>
    /// Releases what the level created itself (its content is unloaded by its ContentManager)
    /// </summary>
    public void Unload()
    {
        _pixel?.Dispose();
        _rain?.Dispose();
    }

    /// <summary>
    /// Updates the level
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="viewport">The screen the level is drawn on</param>
    public void Update(GameTime gameTime, Viewport viewport)
    {
        _player.Update(gameTime);

        // 1. Trap the player at exactly 1000
        _player.position.X = MathHelper.Clamp(_player.position.X, 16f, LevelWidth);

        float screenCenterW = viewport.Width / 2f;

        // 2. The dynamic deadzone
        float deadzone = (screenCenterW / Scale) - 20f;

        if (_player.position.X > _camera.Position.X + deadzone)
            _camera.Position = new Vector2(_player.position.X - deadzone, 100f);
        else if (_player.position.X < _camera.Position.X - deadzone)
            _camera.Position = new Vector2(_player.position.X + deadzone, 100f);

        // 3. Stop the camera from scrolling past the 1000 pixel mark
        float minCameraX = screenCenterW / Scale;
        float maxCameraX = LevelWidth - minCameraX;

        _camera.Position = new Vector2(
            MathHelper.Clamp(_camera.Position.X, minCameraX, maxCameraX),
            100f
        );

        // Convert screen mouse coordinates into accurate world coordinates
        var (offsetX, offsetY, _) = Offsets(viewport);
        MouseState mouse = Mouse.GetState();
        Vector2 worldMousePosition = new Vector2(
            (mouse.X - offsetX) / Scale,
            (mouse.Y - offsetY) / Scale
        );

        // Update the gun with the correct targeted mouse position
        _playerGun.Update(gameTime, _player.position, worldMousePosition);

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
        var (offsetX, offsetY, floorScreenPosition) = Offsets(viewport);
        offsetX += shake.X;
        offsetY += shake.Y;

        // BATCH 1: Background
        Matrix bgTransform = Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(offsetX * 0.33f, shake.Y, 0);
        spriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: bgTransform);
        spriteBatch.Draw(_background, Vector2.Zero, new Rectangle(0, 0, 1000, _background.Height), Color.DarkOrange);
        spriteBatch.End();

        // BATCH 2: Midground
        Matrix mgTransform = Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(offsetX * 0.66f, offsetY, 0);
        spriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: mgTransform);
        spriteBatch.Draw(_midground, Vector2.Zero, new Rectangle(0, 0, 1000, _midground.Height), Color.DarkRed);
        spriteBatch.End();

        // BATCH 3: Foreground, Player, and Bullets
        Matrix fgTransform = Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(offsetX, offsetY, 0);
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointWrap,
            depthStencilState: null,
            rasterizerState: null,
            effect: null,
            transformMatrix: fgTransform
        );
        spriteBatch.Draw(_foreground, Vector2.Zero, new Rectangle(0, 0, 1000, _foreground.Height), Color.Red);
        _player.Draw(gameTime, spriteBatch);
        _playerGun.Draw(spriteBatch);
        spriteBatch.End();

        // The floor: a slate grey strip filling the bottom of the screen
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, (int)floorScreenPosition, viewport.Width, 100), Color.SlateGray);
        spriteBatch.End();

        // The rain, over the level
        _rain.Draw(gameTime);
    }

    /// <summary>
    /// Where the level is drawn on screen: the X and Y offsets of the camera transform, and the
    /// screen Y of the floor. Update and Draw share this so the mouse aims where things are drawn.
    /// </summary>
    private (float OffsetX, float OffsetY, float Floor) Offsets(Viewport viewport)
    {
        float offsetX = viewport.Width / 2f - (_camera.Position.X * Scale);
        float floorScreenPosition = viewport.Height - 50f;
        float offsetY = floorScreenPosition - (100f * Scale);
        return (offsetX, offsetY, floorScreenPosition);
    }
}
