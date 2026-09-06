using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace gameproject0;

/// <summary>
/// A class representing a slime ghost
/// </summary>
public class SlimeGhostSprite
{
    private GamePadState gamePadState;

    private KeyboardState keyboardState;

    private Texture2D texture;

    private Vector2 position = new Vector2(200, 200);

    private bool flipped;

    private BoundingRectangle bounds = new BoundingRectangle(new Vector2(200-16, 200-16), 32, 32);

    public BoundingRectangle Bounds => bounds;

    /// <summary>
    /// the color overlay
    /// </summary>
    public Color Color {get; set;} = Color.White;

    /// <summary>
    /// Loads the sprite texture using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("slime");
    }

    /// <summary>
    /// Updates the sprite's position based on user input
    /// </summary>
    /// <param name="gameTime">The GameTime</param>
    public void Update(GameTime gameTime)
    {
        gamePadState = GamePad.GetState(0);
        keyboardState = Keyboard.GetState();

        // Apply the gamepad movement with inverted Y axis
        position += gamePadState.ThumbSticks.Left * new Vector2(1, -1);
        if (gamePadState.ThumbSticks.Left.X < 0) flipped = true;
        if (gamePadState.ThumbSticks.Left.X > 0) flipped = false;

        // Apply keyboard movement
        if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W)) position += new Vector2(0, -1);
        if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S)) position += new Vector2(0, 1);
        if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
        { 
            position += new Vector2(-1, 0);
            flipped = true;
        }
        if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
        {
            position += new Vector2(1, 0);
            flipped = false;
        }

        //Update the bounds
        bounds.X = position.X - 16;
        bounds.Y = position.Y - 16;
    }

    /// <summary>
    /// Draws the sprite using the supplied SpriteBatch
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The spritebatch to render with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        SpriteEffects spriteEffects = (flipped) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(texture, position, null, Color, 0, new Vector2(64, 64), 0.25f, spriteEffects, 0);
    }
}
