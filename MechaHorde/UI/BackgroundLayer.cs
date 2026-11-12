using Microsoft.Xna.Framework;
using CIS580.UI;

namespace MechaHorde.UI;

/// <summary>
/// The background behind the menus: MechaHorde's dark red sky with the animated planet
/// drifting across it. It is a backdrop, so it stays visible as menus come and go.
/// </summary>
public class BackgroundLayer : Layer
{
    private readonly PlanetSprite _planet = new();

    public BackgroundLayer()
    {
        IsBackdrop = true;
        InputMode = InputMode.None;
    }

    protected override void Load()
    {
        _planet.LoadContent(Game.Content);
    }

    public override void Update(GameTime gameTime)
    {
        _planet.Update(gameTime, Game.GraphicsDevice.Viewport.Width);
    }

    public override void Draw(GameTime gameTime)
    {
        float brightness = Transition.Alpha;

        SpriteBatch.Begin();
        SpriteBatch.Draw(Stack.BlankTexture, Game.GraphicsDevice.Viewport.Bounds, Color.DarkRed * brightness);
        _planet.Color = new Color(brightness, brightness, brightness);
        _planet.Draw(gameTime, SpriteBatch);
        SpriteBatch.End();
    }
}
