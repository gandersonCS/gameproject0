using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CIS580.UI;

namespace CIS580.Views;

/// <summary>
/// A layer that draws the game world through one or more cameras.
/// </summary>
/// <remarks>
/// <para>
/// Derive from this, and draw the world (in world units) in <see cref="DrawWorld"/>. It is
/// called once per camera, with the graphics viewport and SpriteBatch transform already set
/// for that camera, so the same drawing code works for one shared view or for split screen.
/// </para>
/// <para>
/// Each frame, the <see cref="Layout"/> places the cameras on screen, then
/// <see cref="UpdateCameras"/> lets you aim them (e.g. fit the world, or follow players).
/// For split screen, add a camera per player and use a <see cref="SplitLayout"/>.
/// </para>
/// </remarks>
public abstract class WorldLayer : Layer
{
    /// <summary>
    /// The cameras, one per view. There is one to begin with.
    /// </summary>
    protected List<Camera2D> Cameras { get; } = new() { new Camera2D() };

    /// <summary>
    /// Decides where on screen each camera's view goes
    /// </summary>
    public ViewLayout Layout { get; set; } = new SharedLayout();

    /// <summary>
    /// The color behind the world in each view
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Black;

    protected WorldLayer()
    {
        // The world is not a menu: input during play is read by the app's flow, not by layers
        InputMode = InputMode.None;

        // The world stays visible behind menus opened over the game (e.g. options from the
        // pause menu); it only goes away when it is closed
        IsBackdrop = true;
    }

    public override void Update(GameTime gameTime)
    {
        ArrangeViews();
        UpdateCameras(gameTime);
        foreach (var camera in Cameras)
            camera.Shake.Update(gameTime.ElapsedGameTime);
    }

    /// <summary>
    /// Aims the cameras. Called every frame, after the cameras' viewports have been arranged.
    /// </summary>
    protected virtual void UpdateCameras(GameTime gameTime) { }

    /// <summary>
    /// Shakes every camera's view (e.g. for an explosion everyone feels). To shake just one
    /// player's view in split screen, add trauma to that camera's <see cref="Camera2D.Shake"/>.
    /// </summary>
    /// <param name="trauma">How much shake to add (0 to 1; see <see cref="Feedback.ScreenShake"/>)</param>
    public void Shake(float trauma)
    {
        foreach (var camera in Cameras)
            camera.Shake.Add(trauma);
    }

    /// <summary>
    /// Draws the world, in world units, as seen by one camera
    /// </summary>
    /// <param name="camera">The camera being drawn</param>
    /// <param name="gameTime">The game time</param>
    protected abstract void DrawWorld(Camera2D camera, GameTime gameTime);

    public override void Draw(GameTime gameTime)
    {
        var graphics = Game.GraphicsDevice;
        var screenViewport = graphics.Viewport;
        ArrangeViews();

        foreach (var camera in Cameras)
        {
            graphics.Viewport = new Viewport(camera.Viewport);

            // Fill the view's background (in screen space)
            SpriteBatch.Begin();
            SpriteBatch.Draw(Stack.BlankTexture, new Rectangle(0, 0, camera.Viewport.Width, camera.Viewport.Height), BackgroundColor);
            SpriteBatch.End();

            // Draw the world (in world space)
            SpriteBatch.Begin(transformMatrix: camera.Transform);
            DrawWorld(camera, gameTime);
            SpriteBatch.End();
        }

        graphics.Viewport = screenViewport;

        // Fade in from black as the layer comes on screen, and out to black as it leaves
        if (Transition.Alpha < 1f)
            Stack.FillScreen(Color.Black * (1f - Transition.Alpha));
    }

    private void ArrangeViews()
    {
        var screen = Game.GraphicsDevice.PresentationParameters.Bounds;
        var areas = Layout.Arrange(screen, Cameras.Count);
        for (int i = 0; i < Cameras.Count; i++)
            Cameras[i].Viewport = areas[i];
    }
}
